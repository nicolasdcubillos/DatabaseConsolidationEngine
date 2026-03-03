# Database Consolidation Engine

**DatabaseConsolidationEngine** es un sistema de replicación de bases de datos de nivel productivo y casi en tiempo real, construido en C# .NET 9. Se ejecuta como un Servicio de Windows que sondea continuamente múltiples bases de datos SQL Server de origen usando la funcionalidad nativa de **Change Tracking**, y replica los cambios incrementales hacia una única base de datos centralizada de destino.

El sistema fue diseñado para entornos ERP de alto volumen donde decenas de sucursales deben consolidarse en una sola base de datos operacional o de reporting — procesando miles de transacciones por minuto — **sin imponer carga en los sistemas de origen** ni requerir cambios en las aplicaciones.

A continuación se muestra un diagrama de producción de referencia. En este despliegue, 25 bases de datos de origen se replican en tiempo real hacia una única base de datos consolidada para facturación nacional.

![Diagrama de Arquitectura](DatabaseConsolidationEngine.png)

---

## Tabla de Contenidos

- [Arquitectura de Alto Nivel](#arquitectura-de-alto-nivel)
- [Descripción de Componentes](#descripción-de-componentes)
- [Flujo de Datos](#flujo-de-datos)
- [Infraestructura SQL](#infraestructura-sql)
- [Escalabilidad](#escalabilidad)
- [Resiliencia](#resiliencia)
- [Observabilidad](#observabilidad)
- [Configuración](#configuración)
- [Despliegue](#despliegue)

---

## Arquitectura de Alto Nivel

```
┌─────────────────────────────────────────────────────────────────────┐
│                       Instancia SQL Server                          │
│                                                                     │
│  BDs Origen (N)             ConsolidationEngine         BD Destino  │
│  ┌──────────┐               ┌──────────────────┐        ┌────────┐ │
│  │ DB_1     │──CT Changes──►│  Orchestrator    │──MERGE─►│        │ │
│  │ DB_2     │──CT Changes──►│  (jobs paralelos)│──MERGE─►│ CONSOL │ │
│  │ ...      │──CT Changes──►│                  │──MERGE─►│ IDADA  │ │
│  │ DB_N     │──CT Changes──►│  FaultRetry      │        │        │ │
│  └──────────┘               └──────────────────┘        └────────┘ │
│                                      │                              │
│                              ConsolidationDashboard (WinForms)      │
│                         Estado en tiempo real · Errores · Logs      │
└─────────────────────────────────────────────────────────────────────┘
```

El motor opera como un pipeline **CDC (Change Data Capture) basado en pull**. En cada ciclo de heartbeat:

1. Detecta nuevas versiones de Change Tracking en cada base de datos de origen.
2. Obtiene las filas modificadas desde el último watermark registrado.
3. Aplica los cambios en la base de datos destino mediante un MERGE sobre tabla de staging + eliminación por lotes.
4. Avanza el watermark una vez aplicados los cambios con éxito.
5. Opcionalmente reintenta registros que fallaron en ciclos anteriores.

---

## Descripción de Componentes

### `ConsolidationEngine` — Servicio de Windows (.NET 9)

| Componente | Responsabilidad |
|---|---|
| `Worker` | Host `BackgroundService`. Conduce el ciclo de heartbeat, dispara la validación de esquemas al iniciar y protege el ciclo con un try/catch de nivel superior para evitar caídas del servicio. |
| `ChangeTrackingOrchestrator` | Lanza un `Task` por cada par _(BD origen, tabla)_. Usa un `ConcurrentDictionary` como registro de jobs activos para evitar la ejecución simultánea de jobs duplicados sobre la misma clave. |
| `ChangeTrackingETL` | Pipeline ETL por par. Lee versiones del CT, obtiene filas del delta, gestiona upserts y eliminaciones, y avanza el watermark. |
| `FaultRetryProcessor` | Ciclo de reintentos asíncrono. Consulta `ConsolidationEngineErrors` buscando registros con `Retry = 1`, reproduce su payload SQL almacenado y los marca como resueltos. |
| `SqlSchemaValidator` | Guardián de arranque. Valida la conectividad a todas las bases de datos de origen y destino. Agrega automáticamente columnas presentes en el origen pero ausentes en el destino (reparación automática de deriva de esquema). |
| `SqlConsolidationHelper` | Operaciones SQL centrales: gestión de watermarks, obtención de cambios, `SqlBulkCopy` hacia tabla temporal `#stage`, upsert via MERGE con fallback por fila, y eliminación por lotes. |
| `SqlConnectionBuilder` | Fábrica de conexiones singleton, thread-safe. Soporta Autenticación SQL Server y Autenticación Windows. |
| `DualLogger` / `ERPLogger` | Logger de doble destino. Escribe en el pipeline `ILogger` de .NET (consola + Event Log de Windows) **y** persiste registros estructurados en las tablas SQL `ConsolidationEngineLogs` y `ConsolidationEngineErrors`. |

### `ConsolidationDashboard` — Monitor WinForms

Aplicación de escritorio WinForms liviana que se conecta a la base de datos consolidada y ofrece:

- **Grilla de estado de sincronización**: BD origen, versión CT local, versión del watermark consolidado y estado de sincronía (`Sincronizada` / `Pendiente` / `Desactualizada`).
- **Grilla de detalle de errores**: errores de replicación pendientes con soporte para reintento.
- **Visor de logs**: actividad reciente del motor.
- **Gráfico de torta**: proporción visual de bases de datos sincronizadas vs. no sincronizadas.
- **Auto-refresco** cada 5 segundos; botón de refresco manual.
- **Botón Reintentar todos**: invoca `dbo.ConsolidationEngineRetryAll` para programar el reintento de todos los errores pendientes.

---

## Flujo de Datos

```
[BD Origen] CHANGE_TRACKING_CURRENT_VERSION()
        │
        ▼
[ETL] Comparar toVersion vs. watermark fromVersion
        │  sin cambios → avanzar watermark, salir
        │  cambios detectados ▼
[ETL] CHANGETABLE(CHANGES …) + LEFT JOIN tabla origen
        │
        ▼
[Repositorio] SqlBulkCopy → #stage (tabla temporal)
        │
        ├─ Filas INSERT/UPDATE ──► MERGE #stage INTO destino (upsert)
        │                               ↓ fallback por fila ante timeout
        │
        └─ Filas DELETE ─────────► DELETE destino WHERE keyCol IN (…)
                │
                ▼
[ETL] SetWatermark(toVersion)
        │
        ▼
[FaultRetry] Consultar ConsolidationEngineErrors WHERE Retry=1
             │ reproducir payload SQL
             └► marcar Retry=2 si exitoso
```

Cada fila escrita en el destino lleva una columna `SourceKey` (`{BDOrigen}_{ValorClave}`) que identifica de forma unívoca el origen del registro, habilitando trazabilidad y merge multi-origen sin conflictos.

---

## Infraestructura SQL

Las siguientes tablas de control son creadas en la base de datos destino por `ConsolidationEngineInitialConfig.sql`:

| Tabla | Propósito |
|---|---|
| `ConsolidationEngineWatermark` | Almacena la última versión del CT procesada por _(servidor, BD origen, tabla)_. Actúa como el checkpoint persistente para la reanudabilidad. |
| `ConsolidationEngineErrors` | Bitácora de errores. Almacena el payload SQL original, la clave de origen, el tipo de operación y el estado de reintento (`0` = pendiente, `1` = programado para reintento, `2` = resuelto). |
| `ConsolidationEngineLogs` | Log de actividad estructurado, persistido en SQL para consumo del dashboard y análisis post-mortem. |

Objetos clave de la base de datos:

| Objeto | Propósito |
|---|---|
| `dbo.ConsolidationEngineStatus` (SP) | Compara la versión CT local con la versión del watermark para calcular el lag de sincronización en tiempo real por BD origen. |
| `dbo.ConsolidationEngineRetryAll` (SP) | Marca todos los errores no resueltos (`Retry = 0`) como listos para reintentar (`Retry = 1`). |
| `dbo.ConsolidationEngineErrorsView` | Top 25 errores activos (no resueltos). |
| `dbo.ConsolidationEngineLogsView` | Top 25 entradas de log más recientes. |

---

## Escalabilidad

**Fan-out horizontal mediante tareas paralelas**  
El `ChangeTrackingOrchestrator` lanza un `Task.Run` por cada combinación _(BD origen × tabla)_ en cada heartbeat. Con N bases de datos de origen y M tablas, se ejecutan hasta N×M jobs independientes de forma concurrente por ciclo, aprovechando al máximo los hilos disponibles sin bloquearse entre sí.

**Transferencia de datos orientada a lotes**  
Todo el movimiento de datos utiliza `SqlBulkCopy` hacia una tabla temporal `#stage` antes de aplicar un único MERGE. El tamaño del lote está gobernado por `BatchSize` (por defecto: 5.000 filas), manteniendo las transacciones individuales de tamaño predecible y reduciendo los picos de memoria.

**Heartbeat configurable**  
`HeartbeatSeconds` controla el intervalo de sondeo. En entornos de baja latencia se puede configurar en 5–15 segundos; en escenarios de menor prioridad se puede aumentar para reducir la carga sobre SQL.

**Impacto cero en las bases de datos de origen**  
El Change Tracking de SQL Server es un mecanismo liviano basado en el log, mantenido por el propio motor SQL. El motor solo lee el delta del CT — nunca escanea tablas completas, nunca instala triggers y nunca modifica los esquemas de origen.

**Crecimiento lineal de bases de datos de origen**  
Agregar una nueva base de datos de origen requiere únicamente una entrada de configuración (arreglo `Databases` en `appsettings.json`) y ejecutar el script de habilitación del CT. No se requieren cambios de código. El orquestador toma los nuevos pares automáticamente al próximo arranque.

---

## Resiliencia

**Checkpointing basado en watermark**  
Cada job ETL persiste su progreso en `ConsolidationEngineWatermark` únicamente después de aplicar los cambios de forma exitosa. Si el servicio se cae en medio de un ciclo, la siguiente ejecución reprocesa desde la última versión confirmada — garantizando **entrega al-menos-una-vez** con semántica MERGE idempotente.

**Guardia de versión mínima de Change Tracking**  
Antes de procesar, el ETL compara el watermark almacenado contra `CHANGE_TRACKING_MIN_VALID_VERSION` de SQL Server. Si el watermark expiró (el historial del CT fue purgado), el motor avanza el watermark a la versión actual y registra una advertencia, en lugar de entrar en un ciclo de caídas.

**Deduplicación de jobs**  
El `ConcurrentDictionary<string, Task>` en el orquestador garantiza que si un job ETL anterior para una clave _(BD, tabla)_ dada aún está en ejecución cuando dispara el próximo heartbeat, la nueva invocación se omite con una advertencia, en lugar de apilar escrituras concurrentes conflictivas.

**Procesador de reintentos por fallos**  
Las operaciones fallidas a nivel de fila (en lote o individuales) se persisten en `ConsolidationEngineErrors` con su payload SQL original. El `FaultRetryProcessor` se ejecuta asincrónicamente en cada ciclo de heartbeat y reproduce esos payloads, con seguimiento del resultado y contador de reintentos. Esto desacopla los fallos transitorios del flujo principal de replicación.

**Upsert con fallback por fila**  
El camino de MERGE en lote está envuelto con un timeout configurable (`UpsertBatchWithFallbackTimeoutSeconds`). Si la operación de lote supera el timeout, el motor cae a operaciones fila por fila para rescatar lotes parciales, y cualquier fila que falle individualmente queda capturada en `ConsolidationEngineErrors` para reintento posterior.

**Validación de esquema y auto-reparación al arranque**  
Al iniciar el servicio, `SqlSchemaValidator` verifica la conectividad a cada base de datos configurada y compara las definiciones de columnas entre cada tabla de origen y de destino. Cualquier columna presente en el origen pero ausente en el destino se agrega automáticamente mediante `ALTER TABLE`. Esto previene que la deriva de esquemas provoque fallos de replicación tras una actualización del ERP.

**Manejo de errores aislado por job**  
Cada tarea ETL está envuelta individualmente en try/catch. Un fallo en un job _(BD, tabla)_ no afecta a los demás jobs en ejecución. El ciclo de heartbeat tiene un catch externo para asegurar que el servicio continúe corriendo incluso si un error inesperado escapa de un job.

**Gestión del ciclo de vida como Servicio de Windows**  
El servicio usa `UseWindowsService()` y se integra con el Administrador de Control de Servicios de Windows. El host respeta el apagado graceful mediante `CancellationToken` y puede administrarse con `sc.exe` estándar o la consola de Servicios de Windows.

---

## Observabilidad

**Logging estructurado de doble destino**  
`DualLogger` enruta cada evento de log tanto por la abstracción `ILogger` de .NET (→ Consola + Event Log de Windows) como por `ERPLogger` (→ tablas SQL). Esto significa que los logs operacionales están disponibles en tiempo real (Visor de Eventos, consola) y son consultables históricamente en SQL.

**Bitácora de errores persistida en SQL**  
`ConsolidationEngineErrors` almacena el contexto completo del error: clave de origen, base de datos, tabla, tipo de operación, mensaje de error, detalle del stack, payload SQL original y estado de reintento. Esto permite el análisis post-mortem sin necesidad de correlacionar archivos de log.

**Interfaz en tiempo real del ConsolidationDashboard**  
El dashboard WinForms proporciona visibilidad de un vistazo con un ciclo de auto-refresco de 5 segundos:
- Lag de sincronización por BD origen (versión CT local vs. watermark consolidado).
- Clasificación del estado de sincronía: `Sincronizada` (al día), `Pendiente` (existe lag), `Desactualizada` (watermark adelantado al origen).
- Conteo de errores y marca de tiempo del último error por BD origen.
- Gráfico de torta mostrando la proporción de bases de datos sincronizadas.

**Logging de heartbeat**  
Cada ciclo emite una entrada de log con timestamp, proveyendo una señal simple de liveness para herramientas de monitoreo.

---

## Configuración

Todo el comportamiento del motor se controla desde `appsettings.json`. No se requiere recompilar para agregar bases de datos o tablas.

```json
{
  "HeartbeatSeconds": 15,
  "ConsolidationEngine": {
    "Server": "TU_SQL_SERVER",
    "User": "",
    "Password": "",
    "FaultRetryProcessorEnabled": true,
    "BatchSize": 5000,
    "UpsertBatchWithFallbackTimeoutSeconds": 800,
    "Databases": [
      { "Origin": "SUCURSAL_A", "Target": "CONSOLIDADA" },
      { "Origin": "SUCURSAL_B", "Target": "CONSOLIDADA" }
    ],
    "Tables": [
      { "Name": "dbo.MVTONIIF", "KeyColumn": "IDMVTO", "SkipPrimaryKey": true },
      { "Name": "dbo.CENTCOS", "KeyColumn": "CODCC" }
    ]
  }
}
```

| Parámetro | Descripción |
|---|---|
| `HeartbeatSeconds` | Intervalo de sondeo en segundos. |
| `BatchSize` | Filas por lote en `SqlBulkCopy` y en el MERGE. |
| `UpsertBatchWithFallbackTimeoutSeconds` | Timeout antes de cambiar al fallback fila por fila. |
| `FaultRetryProcessorEnabled` | Activar o desactivar el procesador de reintentos sin reiniciar el servicio. |
| `SkipPrimaryKey` | Omitir la clave primaria en INSERT/UPDATE cuando el destino usa una PK sustituta. |

**Autenticación**: dejar `User`/`Password` vacíos para Autenticación Windows Integrada; completar ambos para Autenticación SQL Server.

---

## Despliegue

### 1. Inicializar la infraestructura SQL

Ejecutar `SQL/ConsolidationEngineInitialConfig.sql` contra la base de datos destino (consolidada). Esto crea las tablas de control, vistas, procedimientos almacenados, habilita Change Tracking en las bases de datos de origen y registra los watermarks iniciales.

### 2. Publicar el servicio

```bash
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

O usar el script provisto `dce-publisher.bat`.

### 3. Instalar como Servicio de Windows

```bash
sc create "ConsolidationEngine" binPath= "C:\ruta\a\ConsolidationEngine.exe"
sc start "ConsolidationEngine"
```

O usar el script provisto `dce-installer.bat`.

### 4. Desplegar el Dashboard

Compilar el proyecto `ConsolidationDashboard` y actualizar `App.config` con el string de conexión a la base de datos consolidada. Ejecutar `ConsolidationDashboard.exe` en cualquier equipo Windows con acceso de red al SQL Server.
