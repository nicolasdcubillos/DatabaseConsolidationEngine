# ?? Guía de Pruebas Funcionales - Consolidation Engine

## ?? Tabla de Contenidos

1. [Introducción](#introducción)
2. [Prerequisitos](#prerequisitos)
3. [Configuración Inicial](#configuración-inicial)
4. [Escenarios de Prueba](#escenarios-de-prueba)
5. [Validación de Resultados](#validación-de-resultados)
6. [Troubleshooting](#troubleshooting)

---

## ?? Introducción

Este documento acompaña al script SQL `FunctionalTests_ConsolidationEngine.sql` y proporciona una guía detallada para ejecutar pruebas funcionales completas del motor de consolidación usando las **bases de datos existentes**.

### Bases de Datos

- **BOGOTA** - Base de datos origen 1
- **PASTO** - Base de datos origen 2  
- **CONSOLIDADA** - Base de datos destino (consolidada)

### ¿Qué se prueba?

- ? Verificación del estado actual del sistema
- ? Operaciones INSERT en bases origen
- ? Operaciones UPDATE en bases origen
- ? Replicación a CONSOLIDADA
- ? Manejo de errores parciales con fallback
- ? Sistema de retry (FaultRetryProcessor)
- ? Performance y estadísticas
- ? Health check del sistema

---

## ?? Prerequisitos

### Software Requerido

- **SQL Server 2016+** con las bases de datos BOGOTA, PASTO y CONSOLIDADA ya creadas
- **SQL Server Management Studio** (SSMS) o Azure Data Studio
- **Consolidation Engine** compilado y configurado
- Permisos adecuados en las tres bases de datos

### Estado del Sistema

1. **Bases de Datos:**
   - BOGOTA, PASTO y CONSOLIDADA deben existir
   - Change Tracking debe estar habilitado en BOGOTA y PASTO
   - Tablas de control deben existir en CONSOLIDADA:
     - `ConsolidationEngineWatermark`
     - `ConsolidationEngineErrors`
     - `ConsolidationEngineLogs`

2. **Configurar ConsolidationEngine:**
   - El archivo `appsettings.json` ya debe estar configurado
   - Verificar credenciales y nombres de bases de datos

3. **Compilar el proyecto:**
   ```bash
   dotnet build --configuration Release
   ```

---

## ?? Configuración Inicial

### Paso 1: Verificar appsettings.json

Tu configuración debe tener algo similar a esto:

```json
{
  "ConsolidationEngine": {
    "Server": "tu-servidor",
    "User": "tu-usuario",
    "Password": "tu-password",
    "Databases": [
      {
        "Origin": "BOGOTA",
        "Target": "CONSOLIDADA"
      },
      {
        "Origin": "PASTO",
        "Target": "CONSOLIDADA"
      }
    ],
    "Tables": [
      {
        "Name": "dbo.TuTabla1",
        "KeyColumn": "Id",
        "SkipPrimaryKey": false
      },
      {
        "Name": "dbo.TuTabla2",
        "KeyColumn": "IdTabla",
        "SkipPrimaryKey": false
      }
    ],
    "BatchSize": 1000,
    "UpsertBatchWithFallbackTimeoutSeconds": 300,
    "FaultRetryProcessorEnabled": true
  }
}
```

### Paso 2: Ejecutar Verificaciones del Script

Ejecutar las secciones 1-6 del script SQL para verificar:

```sql
-- PASO 1: Verificar que las bases de datos existen
-- PASO 2: Verificar Change Tracking habilitado
-- PASO 3: Verificar tablas de control en CONSOLIDADA
-- PASO 4: Listar tablas con Change Tracking en BOGOTA
-- PASO 5: Listar tablas con Change Tracking en PASTO
-- PASO 6: Ver estado actual de watermarks
```

**? Checkpoint:** Todas las verificaciones deben pasar sin errores.

---

## ?? Escenarios de Prueba

### Escenario 1: Verificar Sincronización Actual

**Objetivo:** Comprobar el estado actual del sistema antes de hacer cambios.

**Pasos:**

1. Ejecutar la sección SQL del Escenario 1
2. Revisar:
   - Últimos logs de sincronización
   - Errores activos (si existen)
   - Watermarks actuales

**? Resultado Esperado:**
- Logs recientes del sistema
- Estado de errores (idealmente 0 activos)
- Watermarks con timestamps recientes

**Validaciones:**
```sql
USE CONSOLIDADA;

-- Ver últimos logs
SELECT TOP 20 * FROM dbo.ConsolidationEngineLogs ORDER BY Id DESC;

-- Ver errores activos
SELECT COUNT(*) FROM dbo.ConsolidationEngineErrors WHERE Retry = 1;

-- Ver watermarks
SELECT * FROM dbo.ConsolidationEngineWatermark;
```

---

### Escenario 2: INSERTS en BOGOTA

**Objetivo:** Verificar que los nuevos registros se replican correctamente desde BOGOTA.

**Pasos:**

1. **Identificar una tabla de tu esquema** que esté configurada en el ConsolidationEngine
2. **Insertar datos de prueba** en BOGOTA:
   ```sql
   USE BOGOTA;
   
   -- Ajustar según tu esquema real
   INSERT INTO dbo.[TuTabla] (Campo1, Campo2, FechaCreacion)
   VALUES 
       ('TestBOGOTA_' + CAST(NEWID() AS VARCHAR(36)), 'Dato de prueba', GETDATE()),
       ('TestBOGOTA_' + CAST(NEWID() AS VARCHAR(36)), 'Dato de prueba 2', GETDATE());
   
   -- Ver la versión de Change Tracking
   SELECT CHANGE_TRACKING_CURRENT_VERSION() AS CurrentVersion;
   ```

3. **Ejecutar ConsolidationEngine:**
   ```bash
   dotnet run --project ConsolidationEngine
   ```

4. **Verificar en CONSOLIDADA:**
   ```sql
   USE CONSOLIDADA;
   
   -- Buscar los registros replicados
   SELECT * FROM dbo.[TuTabla]
   WHERE Campo1 LIKE 'TestBOGOTA%'
   ORDER BY FechaCreacion DESC;
   
   -- Verificar SourceKey
   SELECT TOP 5
       Campo1,
       SourceKey  -- Debe ser: BOGOTA_{Id}
   FROM dbo.[TuTabla]
   WHERE SourceKey LIKE 'BOGOTA%'
   ORDER BY FechaCreacion DESC;
   ```

**? Resultado Esperado:**
- Registros insertados aparecen en CONSOLIDADA
- Campo `SourceKey` tiene formato: `BOGOTA_{PrimaryKey}`
- Watermark actualizado en `ConsolidationEngineWatermark`

---

### Escenario 3: UPDATES en PASTO

**Objetivo:** Verificar que las actualizaciones se replican correctamente desde PASTO.

**Pasos:**

1. **Actualizar registros existentes en PASTO:**
   ```sql
   USE PASTO;
   
   -- Ajustar según tu esquema
   UPDATE dbo.[TuTabla]
   SET Campo2 = 'Actualizado ' + CONVERT(VARCHAR, GETDATE(), 120),
       FechaModificacion = GETDATE()
   WHERE Id IN (SELECT TOP 3 Id FROM dbo.[TuTabla] ORDER BY FechaCreacion DESC);
   
   PRINT 'Registros actualizados: ' + CAST(@@ROWCOUNT AS VARCHAR);
   ```

2. **Ejecutar ConsolidationEngine**

3. **Verificar en CONSOLIDADA:**
   ```sql
   USE CONSOLIDADA;
   
   -- Ver los registros actualizados
   SELECT * FROM dbo.[TuTabla]
   WHERE SourceKey LIKE 'PASTO%'
     AND FechaModificacion >= DATEADD(MINUTE, -10, GETDATE())
   ORDER BY FechaModificacion DESC;
   ```

**? Resultado Esperado:**
- Cambios reflejados en CONSOLIDADA
- Timestamps actualizados
- Log en `ConsolidationEngineLogs` muestra operaciones UPDATE procesadas

---

### Escenario 4: Verificar Cambios Pendientes

**Objetivo:** Ver qué cambios están esperando ser sincronizados.

**Pasos:**

1. **Antes de ejecutar el motor**, verificar cambios pendientes:
   ```sql
   USE BOGOTA;
   
   DECLARE @LastWatermark BIGINT;
   SELECT @LastWatermark = LastVersion
   FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
   WHERE SourceDB = 'BOGOTA' AND TableName = 'dbo.TuTabla';
   
   IF @LastWatermark IS NULL
       SET @LastWatermark = 0;
   
   SELECT 
       'BOGOTA - Cambios Pendientes' AS Origen,
       COUNT(*) AS TotalCambios,
       COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'I' THEN 1 END) AS Inserts,
       COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'U' THEN 1 END) AS Updates,
       COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'D' THEN 1 END) AS Deletes,
       MIN(SYS_CHANGE_VERSION) AS DesdeVersion,
       MAX(SYS_CHANGE_VERSION) AS HastaVersion
   FROM CHANGETABLE(CHANGES dbo.TuTabla, @LastWatermark) AS ct;
   ```

2. Repetir para PASTO si es necesario

**? Resultado Esperado:**
- Query muestra el número de cambios pendientes
- Se puede ver el desglose por tipo de operación (I/U/D)

---

### Escenario 5: Simular Error (Testing Fallback)

**Objetivo:** Verificar que el mecanismo de fallback funciona correctamente.

**?? PRECAUCIÓN:** Solo ejecutar en ambiente de prueba/desarrollo.

**Pasos:**

1. **Agregar un constraint temporal en CONSOLIDADA:**
   ```sql
   USE CONSOLIDADA;
   
   -- Ejemplo: constraint que valide formato de email
   ALTER TABLE dbo.[TuTabla]
   ADD CONSTRAINT CK_TestEmail 
   CHECK (Email LIKE '%@%' OR Email IS NULL);
   
   PRINT '? Constraint de prueba agregado';
   ```

2. **Insertar datos en BOGOTA que violen el constraint:**
   ```sql
   USE BOGOTA;
   
   INSERT INTO dbo.[TuTabla] (Campo1, Email)
   VALUES 
       ('TestOK', 'correcto@email.com'),
       ('TestBAD', 'email-sin-arroba'),  -- Este fallará
       ('TestOK2', 'otro@email.com');
   ```

3. **Ejecutar ConsolidationEngine**

4. **Verificar resultados en CONSOLIDADA:**
   ```sql
   USE CONSOLIDADA;
   
   -- Ver registros exitosos (deben estar 2)
   SELECT * FROM dbo.[TuTabla]
   WHERE Campo1 IN ('TestOK', 'TestOK2', 'TestBAD');
   
   -- Ver error loggeado (debe haber 1)
   SELECT * FROM dbo.ConsolidationEngineErrors
   WHERE TableName = 'dbo.TuTabla'
     AND SourceKey LIKE '%TestBAD%'
     AND Retry = 1;
   
   -- Ver el payload SQL para retry
   SELECT Payload FROM dbo.ConsolidationEngineErrors
   WHERE SourceKey LIKE '%TestBAD%';
   ```

5. **Remover el constraint y ejecutar retry:**
   ```sql
   USE CONSOLIDADA;
   
   -- Eliminar constraint
   ALTER TABLE dbo.[TuTabla]
   DROP CONSTRAINT CK_TestEmail;
   
   -- Ejecutar ConsolidationEngine de nuevo
   -- El FaultRetryProcessor debería procesar el error pendiente
   ```

6. **Verificar que el retry funcionó:**
   ```sql
   -- El registro ahora debe existir
   SELECT * FROM dbo.[TuTabla] WHERE Campo1 = 'TestBAD';
   
   -- El error debe estar marcado como Retry=2
   SELECT * FROM dbo.ConsolidationEngineErrors
   WHERE SourceKey LIKE '%TestBAD%';
   -- Retry debe ser 2 (reintentado exitosamente)
   ```

**? Resultado Esperado:**
- 2 registros insertados inicialmente
- 1 error loggeado con `Retry = 1`
- Después de remover constraint: registro insertado y error marcado como `Retry = 2`
- Log de warning: `"Se procesaron 2/3 filas correctamente"`

---

### Escenario 6: Verificar Sistema de Retry

**Objetivo:** Inspeccionar el sistema de retry y errores pendientes.

**Pasos:**

1. **Ver errores pendientes:**
   ```sql
   USE CONSOLIDADA;
   
   -- Errores activos
   SELECT 
       Id,
       SourceKey,
       TableName,
       Operation,
       LEFT(ErrorMessage, 100) AS ErrorResumen,
       RetryCount,
       CreatedAt
   FROM dbo.ConsolidationEngineErrors
   WHERE Retry = 1
   ORDER BY CreatedAt DESC;
   ```

2. **Ver errores ya reintentados:**
   ```sql
   SELECT 
       Id,
       SourceKey,
       RetryCount,
       CreatedAt
   FROM dbo.ConsolidationEngineErrors
   WHERE Retry = 2
   ORDER BY Id DESC;
   ```

**? Resultado Esperado:**
- Lista de errores pendientes (si existen)
- Historial de errores reintentados exitosamente
- `RetryCount` muestra cuántos intentos se hicieron

---

### Escenario 7: Análisis de Performance

**Objetivo:** Evaluar el rendimiento del sistema.

**Pasos:**

1. Ejecutar la sección de análisis de performance del script
2. Revisar métricas

**Validaciones:**
```sql
USE CONSOLIDADA;

-- Estadísticas de sincronización (últimas 24 horas)
SELECT 
    SourceDB,
    TableName,
    COUNT(*) AS EjecucionesETL,
    MAX(CreatedAt) AS UltimaEjecucion,
    DATEDIFF(MINUTE, MAX(CreatedAt), GETDATE()) AS MinutosSinSync
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%OK%'
  AND CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY SourceDB, TableName;

-- Tasa de éxito
SELECT 
    COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) AS Exitosas,
    COUNT(CASE WHEN Message LIKE '%ERROR%' THEN 1 END) AS Fallidas,
    CAST(
        COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) * 100.0 / 
        NULLIF(COUNT(*), 0) 
    AS DECIMAL(5,2)) AS PorcentajeExito
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE());
```

**? Resultado Esperado:**
- Tasa de éxito > 95%
- Tiempo promedio de sincronización aceptable
- Sin warnings o errores críticos recientes

---

## ?? Validación de Resultados

### Queries de Validación General

```sql
USE CONSOLIDADA;

-- 1. Resumen de Watermarks
SELECT 
    SourceDB,
    TableName,
    LastVersion AS WatermarkActual,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar
FROM dbo.ConsolidationEngineWatermark
ORDER BY SourceDB, TableName;

-- 2. Historial de Errores
SELECT 
    Retry,
    COUNT(*) AS Cantidad,
    CASE Retry
        WHEN 0 THEN 'No reintentar'
        WHEN 1 THEN 'Pendiente de retry'
        WHEN 2 THEN 'Reintentado exitosamente'
    END AS Estado
FROM dbo.ConsolidationEngineErrors
GROUP BY Retry;

-- 3. Health Check Rápido
DECLARE @ErrorCount INT, @OldSync INT, @Status NVARCHAR(50);

SELECT @ErrorCount = COUNT(*) FROM dbo.ConsolidationEngineErrors WHERE Retry = 1;
SELECT @OldSync = COUNT(*) FROM dbo.ConsolidationEngineWatermark 
WHERE DATEDIFF(HOUR, UpdatedAt, GETDATE()) > 2;

SET @Status = CASE 
    WHEN @ErrorCount = 0 AND @OldSync = 0 THEN '? SISTEMA SALUDABLE'
    WHEN @ErrorCount > 0 OR @OldSync > 0 THEN '??  ATENCIÓN REQUERIDA'
    ELSE '?? ALERTA CRÍTICA'
END;

SELECT @Status AS EstadoGeneral, @ErrorCount AS ErroresActivos, @OldSync AS SyncAntiguas;
```

---

## ?? Troubleshooting

### Problema: Watermark no se actualiza

**Síntomas:**
- El motor procesa los mismos cambios repetidamente
- Watermark permanece en el mismo valor

**Diagnóstico:**
```sql
USE CONSOLIDADA;

-- Verificar permisos
SELECT 
    USER_NAME() AS UsuarioActual,
    HAS_PERMS_BY_NAME('dbo.ConsolidationEngineWatermark', 'OBJECT', 'UPDATE') AS TienePermisoUpdate;

-- Ver historial de watermark
SELECT TOP 20 * FROM dbo.ConsolidationEngineWatermark ORDER BY UpdatedAt DESC;
```

**Solución:**
1. Verificar permisos de UPDATE en la tabla
2. Revisar logs del ConsolidationEngine
3. Verificar que no hay errores críticos bloqueando la actualización

---

### Problema: Change Tracking no detecta cambios

**Síntomas:**
- Se insertan/actualizan datos pero no se replican
- CHANGETABLE retorna vacío

**Diagnóstico:**
```sql
-- En BOGOTA o PASTO
USE BOGOTA;

-- Verificar Change Tracking en la DB
SELECT * FROM sys.change_tracking_databases 
WHERE database_id = DB_ID();

-- Verificar Change Tracking en la tabla específica
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    is_track_columns_updated_on
FROM sys.change_tracking_tables
WHERE OBJECT_NAME(object_id) = 'TuTabla';

-- Ver versión actual
SELECT CHANGE_TRACKING_CURRENT_VERSION() AS CurrentVersion;
```

**Solución:**
```sql
-- Si Change Tracking no está habilitado:
ALTER DATABASE BOGOTA
SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);

-- Habilitar en la tabla:
ALTER TABLE dbo.TuTabla
ENABLE CHANGE_TRACKING
WITH (TRACK_COLUMNS_UPDATED = OFF);
```

---

### Problema: Errores no se reintentan

**Síntomas:**
- Errores permanecen con `Retry = 1` indefinidamente
- FaultRetryProcessor no los procesa

**Diagnóstico:**
```sql
USE CONSOLIDADA;

-- Ver errores antiguos
SELECT 
    Id,
    SourceKey,
    ErrorMessage,
    RetryCount,
    DATEDIFF(HOUR, CreatedAt, GETDATE()) AS HorasDesdeError
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
  AND CreatedAt < DATEADD(HOUR, -24, GETDATE());

-- Ver el payload SQL
SELECT Id, SourceKey, Payload
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1;
```

**Solución:**
1. Verificar que `FaultRetryProcessorEnabled = true` en appsettings.json
2. Intentar ejecutar el payload SQL manualmente para identificar el problema
3. Revisar logs del ConsolidationEngine durante la ejecución del retry

---

### Problema: Performance degradado

**Síntomas:**
- Sincronización muy lenta
- Alto uso de CPU/Memoria

**Diagnóstico:**
```sql
USE CONSOLIDADA;

-- Verificar fragmentación de índices
SELECT 
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.avg_fragmentation_in_percent,
    ips.page_count
FROM sys.dm_db_index_physical_stats(
    DB_ID(), NULL, NULL, NULL, 'LIMITED'
) ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id 
    AND ips.index_id = i.index_id
WHERE ips.avg_fragmentation_in_percent > 30
  AND ips.page_count > 1000
ORDER BY ips.avg_fragmentation_in_percent DESC;

-- Verificar índices en SourceKey
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE i.name LIKE '%SourceKey%';
```

**Solución:**
```sql
-- Reconstruir índices fragmentados
ALTER INDEX ALL ON dbo.TuTabla REBUILD;

-- Actualizar estadísticas
UPDATE STATISTICS dbo.TuTabla;

-- Ajustar BatchSize en appsettings.json (valores recomendados: 500-2000)
```

---

## ?? Notas Importantes

### ?? Consideraciones para Producción

1. **NO ejecutar INSERT/UPDATE/DELETE de prueba en producción**
2. **Verificar backups antes de hacer cambios**
3. **Probar primero en ambiente de desarrollo/QA**
4. **Monitorear recursos (CPU, memoria, disco) durante pruebas**

### ?? Scripts Relacionados

- **`MonitoringQueries.sql`** - Para monitoreo continuo en producción
- **`FunctionalTests_ConsolidationEngine.sql`** - Script principal de pruebas

### ?? Flujo de Trabajo Recomendado

1. Ejecutar `FunctionalTests_ConsolidationEngine.sql` Pasos 1-6 (verificación)
2. Realizar cambios de prueba en BOGOTA/PASTO
3. Ejecutar ConsolidationEngine
4. Validar resultados en CONSOLIDADA
5. Revisar logs y errores
6. Usar `MonitoringQueries.sql` para análisis profundo

---

## ?? Referencias

- [SQL Server Change Tracking Documentation](https://docs.microsoft.com/sql/relational-databases/track-changes/about-change-tracking-sql-server)
- [Monitoring Queries](MonitoringQueries.sql)
- [Configuration Guide](../appsettings.json)

---

**Última actualización:** Enero 2025  
**Versión:** 2.0 (Adaptado para BOGOTA, PASTO, CONSOLIDADA)
