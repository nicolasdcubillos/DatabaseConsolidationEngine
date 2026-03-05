/*******************************************************************************
 * SCRIPT DE PRUEBAS FUNCIONALES - CONSOLIDATION ENGINE
 * 
 * Propósito: Script completo para probar todos los escenarios funcionales
 *            del motor de consolidación con Change Tracking de SQL Server.
 *
 * IMPORTANTE: Este script trabaja con las bases de datos EXISTENTES:
 *             - BOGOTA (Origin)
 *             - PASTO (Origin)
 *             - CONSOLIDADA (Target)
 *
 * Autor: Consolidation Engine Team
 * Fecha: 2025
 *
 * PREREQUISITOS:
 * 1. Bases de datos BOGOTA, PASTO y CONSOLIDADA ya existentes
 * 2. Change Tracking ya habilitado
 * 3. Tablas de control ya creadas en CONSOLIDADA
 * 4. ConsolidationEngine configurado correctamente
 *
 * INSTRUCCIONES:
 * - Ejecutar cada sección paso a paso
 * - Verificar resultados después de cada prueba
 * - NO EJECUTAR EN PRODUCCIÓN - Solo para testing
 ******************************************************************************/

-- ============================================================================
-- CONFIGURACIÓN INICIAL
-- ============================================================================
USE master;
GO

PRINT '============================================================================';
PRINT 'CONSOLIDATION ENGINE - FUNCTIONAL TESTS (BASES EXISTENTES)';
PRINT '============================================================================';
PRINT 'Origin DB 1: BOGOTA';
PRINT 'Origin DB 2: PASTO';
PRINT 'Target DB: CONSOLIDADA';
PRINT 'Server: ' + @@SERVERNAME;
PRINT '============================================================================';
GO

-- ============================================================================
-- PASO 1: VERIFICAR BASES DE DATOS EXISTENTES
-- ============================================================================
PRINT '';
PRINT '-- PASO 1: Verificando bases de datos existentes...';
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'BOGOTA')
BEGIN
    PRINT '? ERROR: Base de datos BOGOTA no existe';
    RAISERROR('Base de datos BOGOTA no encontrada', 16, 1);
END
ELSE
    PRINT '? Base de datos BOGOTA encontrada';
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PASTO')
BEGIN
    PRINT '? ERROR: Base de datos PASTO no existe';
    RAISERROR('Base de datos PASTO no encontrada', 16, 1);
END
ELSE
    PRINT '? Base de datos PASTO encontrada';
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'CONSOLIDADA')
BEGIN
    PRINT '? ERROR: Base de datos CONSOLIDADA no existe';
    RAISERROR('Base de datos CONSOLIDADA no encontrada', 16, 1);
END
ELSE
    PRINT '? Base de datos CONSOLIDADA encontrada';
GO

-- ============================================================================
-- PASO 2: VERIFICAR CHANGE TRACKING
-- ============================================================================
PRINT '';
PRINT '-- PASO 2: Verificando Change Tracking...';
GO

-- Verificar BOGOTA
USE BOGOTA;
GO

IF NOT EXISTS (SELECT * FROM sys.change_tracking_databases WHERE database_id = DB_ID())
BEGIN
    PRINT '??  Change Tracking NO está habilitado en BOGOTA';
    PRINT '    Para habilitarlo: ALTER DATABASE BOGOTA SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);';
END
ELSE
    PRINT '? Change Tracking habilitado en BOGOTA';
GO

-- Verificar PASTO
USE PASTO;
GO

IF NOT EXISTS (SELECT * FROM sys.change_tracking_databases WHERE database_id = DB_ID())
BEGIN
    PRINT '??  Change Tracking NO está habilitado en PASTO';
    PRINT '    Para habilitarlo: ALTER DATABASE PASTO SET CHANGE_TRACKING = ON (CHANGE_RETENTION = 2 DAYS, AUTO_CLEANUP = ON);';
END
ELSE
    PRINT '? Change Tracking habilitado en PASTO';
GO

-- ============================================================================
-- PASO 3: VERIFICAR TABLAS DE CONTROL EN CONSOLIDADA
-- ============================================================================
PRINT '';
PRINT '-- PASO 3: Verificando tablas de control en CONSOLIDADA...';
GO

USE CONSOLIDADA;
GO

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'ConsolidationEngineWatermark' AND type = 'U')
    PRINT '??  Tabla ConsolidationEngineWatermark NO existe';
ELSE
    PRINT '? Tabla ConsolidationEngineWatermark existe';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'ConsolidationEngineErrors' AND type = 'U')
    PRINT '??  Tabla ConsolidationEngineErrors NO existe';
ELSE
    PRINT '? Tabla ConsolidationEngineErrors existe';

IF NOT EXISTS (SELECT * FROM sys.objects WHERE name = 'ConsolidationEngineLogs' AND type = 'U')
    PRINT '??  Tabla ConsolidationEngineLogs NO existe';
ELSE
    PRINT '? Tabla ConsolidationEngineLogs existe';
GO

-- ============================================================================
-- PASO 4: LISTAR TABLAS CON CHANGE TRACKING EN BOGOTA
-- ============================================================================
PRINT '';
PRINT '-- PASO 4: Listando tablas con Change Tracking en BOGOTA...';
GO

USE BOGOTA;
GO

SELECT 
    OBJECT_SCHEMA_NAME(t.object_id) + '.' + OBJECT_NAME(t.object_id) AS TableName,
    ct.is_track_columns_updated_on AS TrackColumnsUpdated,
    CHANGE_TRACKING_MIN_VALID_VERSION(t.object_id) AS MinValidVersion,
    CHANGE_TRACKING_CURRENT_VERSION() AS CurrentVersion
FROM sys.change_tracking_tables ct
INNER JOIN sys.tables t ON ct.object_id = t.object_id
ORDER BY TableName;
GO

-- ============================================================================
-- PASO 5: LISTAR TABLAS CON CHANGE TRACKING EN PASTO
-- ============================================================================
PRINT '';
PRINT '-- PASO 5: Listando tablas con Change Tracking en PASTO...';
GO

USE PASTO;
GO

SELECT 
    OBJECT_SCHEMA_NAME(t.object_id) + '.' + OBJECT_NAME(t.object_id) AS TableName,
    ct.is_track_columns_updated_on AS TrackColumnsUpdated,
    CHANGE_TRACKING_MIN_VALID_VERSION(t.object_id) AS MinValidVersion,
    CHANGE_TRACKING_CURRENT_VERSION() AS CurrentVersion
FROM sys.change_tracking_tables ct
INNER JOIN sys.tables t ON ct.object_id = t.object_id
ORDER BY TableName;
GO

-- ============================================================================
-- PASO 6: ESTADO ACTUAL DE WATERMARKS
-- ============================================================================
PRINT '';
PRINT '-- PASO 6: Estado actual de watermarks en CONSOLIDADA...';
GO

USE CONSOLIDADA;
GO

SELECT 
    SourceServer,
    SourceDB,
    TableName,
    LastVersion AS WatermarkActual,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar
FROM dbo.ConsolidationEngineWatermark
ORDER BY SourceDB, TableName;
GO

-- Si no hay watermarks, mostrar mensaje
IF NOT EXISTS (SELECT 1 FROM dbo.ConsolidationEngineWatermark)
BEGIN
    PRINT '';
    PRINT '??  No hay watermarks registrados. El sistema se inicializará en la primera ejecución.';
END
GO

-- ============================================================================
-- ESCENARIO 1: VERIFICAR SINCRONIZACIÓN ACTUAL
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 1: VERIFICAR SINCRONIZACIÓN ACTUAL';
PRINT '============================================================================';
PRINT 'Este escenario verifica el estado actual del sistema';
PRINT '';
GO

USE CONSOLIDADA;
GO

-- Ver últimos logs de sincronización
SELECT TOP 20
    LogLevel,
    LEFT(Message, 150) AS Mensaje,
    SourceDatabase,
    TableName,
    CreatedAt
FROM dbo.ConsolidationEngineLogs
ORDER BY Id DESC;
GO

-- Ver errores activos
SELECT 
    'Errores Activos' AS Estado,
    COUNT(*) AS Total
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1;
GO

-- ============================================================================
-- ESCENARIO 2: INSERTAR DATOS DE PRUEBA EN BOGOTA
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 2: INSERTS EN BOGOTA';
PRINT '============================================================================';
PRINT 'Este escenario inserta datos de prueba en BOGOTA para verificar';
PRINT 'que se replican correctamente a CONSOLIDADA';
PRINT '';
GO

-- NOTA: Ajustar según las tablas reales de tu esquema
-- Ejemplo con una tabla hipotética. DEBES AJUSTAR esto según tu esquema real.

/*
USE BOGOTA;
GO

-- Ejemplo: Insertar en tabla de prueba (AJUSTAR según tu esquema)
INSERT INTO dbo.TuTabla (Campo1, Campo2, FechaCreacion)
VALUES 
    ('TestBOGOTA_' + CAST(NEWID() AS VARCHAR(36)), 'Dato de prueba', GETDATE()),
    ('TestBOGOTA_' + CAST(NEWID() AS VARCHAR(36)), 'Dato de prueba 2', GETDATE());
GO

PRINT '? ' + CAST(@@ROWCOUNT AS VARCHAR) + ' registros insertados en BOGOTA';

-- Ver la versión actual
DECLARE @CurrentVersion BIGINT = CHANGE_TRACKING_CURRENT_VERSION();
PRINT '? Current Change Tracking Version en BOGOTA: ' + CAST(@CurrentVersion AS VARCHAR);
GO
*/

PRINT '';
PRINT '>>> ACCIÓN MANUAL REQUERIDA <<<';
PRINT 'Debes insertar datos de prueba en una tabla de BOGOTA que tengas configurada.';
PRINT 'Ejemplo:';
PRINT '  USE BOGOTA;';
PRINT '  INSERT INTO dbo.[TuTabla] (Campos...) VALUES (Valores...);';
PRINT '';
PRINT 'Después de insertar, ejecutar el ConsolidationEngine y verificar la replicación.';
PRINT '';

-- ============================================================================
-- ESCENARIO 3: ACTUALIZAR DATOS DE PRUEBA EN PASTO
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 3: UPDATES EN PASTO';
PRINT '============================================================================';
PRINT 'Este escenario actualiza datos en PASTO para verificar replicación';
PRINT '';
GO

/*
USE PASTO;
GO

-- Ejemplo: Actualizar registros (AJUSTAR según tu esquema)
UPDATE dbo.TuTabla
SET Campo2 = 'Actualizado desde prueba ' + CONVERT(VARCHAR, GETDATE(), 120),
    FechaModificacion = GETDATE()
WHERE ID IN (SELECT TOP 5 ID FROM dbo.TuTabla ORDER BY FechaCreacion DESC);
GO

PRINT '? ' + CAST(@@ROWCOUNT AS VARCHAR) + ' registros actualizados en PASTO';
GO
*/

PRINT '';
PRINT '>>> ACCIÓN MANUAL REQUERIDA <<<';
PRINT 'Debes actualizar datos de prueba en una tabla de PASTO.';
PRINT 'Después ejecutar el ConsolidationEngine para verificar la replicación.';
PRINT '';

-- ============================================================================
-- ESCENARIO 4: VERIFICAR CAMBIOS PENDIENTES
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 4: VERIFICAR CAMBIOS PENDIENTES';
PRINT '============================================================================';
PRINT 'Este escenario muestra qué cambios están pendientes de sincronizar';
PRINT '';
GO

-- Ver cambios pendientes en BOGOTA
/*
USE BOGOTA;
GO

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
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'D' THEN 1 END) AS Deletes
FROM CHANGETABLE(CHANGES dbo.TuTabla, @LastWatermark) AS ct;
GO
*/

PRINT '';
PRINT '>>> INFORMACIÓN <<<';
PRINT 'Para ver cambios pendientes, ejecuta queries CHANGETABLE en cada base origen.';
PRINT 'Ejemplo en el código comentado arriba (ajustar según tu esquema).';
PRINT '';

-- ============================================================================
-- ESCENARIO 5: SIMULAR ERROR PARA PROBAR FALLBACK
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 5: SIMULAR ERROR (Testing Fallback)';
PRINT '============================================================================';
PRINT 'Este escenario simula un error de constraint para probar el mecanismo';
PRINT 'de fallback y logging de errores';
PRINT '';
GO

USE CONSOLIDADA;
GO

-- Ejemplo: Agregar un constraint temporal que cause errores
/*
-- PRECAUCIÓN: Solo ejecutar en ambiente de prueba
ALTER TABLE dbo.TuTabla
ADD CONSTRAINT CK_TestConstraint 
CHECK ([Campo] LIKE '%@%');
GO

PRINT '? Constraint de prueba agregado';
PRINT '  Ahora inserta datos en BOGOTA que violen este constraint';
PRINT '  y ejecuta el ConsolidationEngine para ver el fallback en acción.';
*/

PRINT '';
PRINT '>>> ACCIÓN MANUAL (OPCIONAL) <<<';
PRINT 'Para probar el fallback:';
PRINT '1. Agrega un constraint temporal en CONSOLIDADA';
PRINT '2. Inserta datos en BOGOTA/PASTO que violen el constraint';
PRINT '3. Ejecuta ConsolidationEngine';
PRINT '4. Verifica que los errores se loggean en ConsolidationEngineErrors';
PRINT '5. Elimina el constraint y ejecuta nuevamente para ver el retry';
PRINT '';

-- ============================================================================
-- ESCENARIO 6: VERIFICAR SISTEMA DE RETRY
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 6: VERIFICAR SISTEMA DE RETRY';
PRINT '============================================================================';
GO

USE CONSOLIDADA;
GO

-- Ver errores pendientes de retry
SELECT 
    'Errores Pendientes (Retry=1)' AS Estado,
    COUNT(*) AS Total
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1;
GO

-- Ver detalle de errores
SELECT TOP 10
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
GO

-- Ver errores ya reintentados
SELECT 
    'Errores Reintentados (Retry=2)' AS Estado,
    COUNT(*) AS Total
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 2;
GO

PRINT '';
PRINT '>>> INFORMACIÓN <<<';
PRINT 'Si hay errores con Retry=1, el FaultRetryProcessor los reintentará';
PRINT 'en la próxima ejecución del ConsolidationEngine.';
PRINT '';

-- ============================================================================
-- ESCENARIO 7: ANÁLISIS DE PERFORMANCE
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'ESCENARIO 7: ANÁLISIS DE PERFORMANCE';
PRINT '============================================================================';
GO

USE CONSOLIDADA;
GO

-- Estadísticas de sincronización (últimas 24 horas)
SELECT 
    SourceDB AS BaseDatos,
    TableName AS Tabla,
    COUNT(*) AS EjecucionesETL,
    MAX(CreatedAt) AS UltimaEjecucion,
    DATEDIFF(MINUTE, MAX(CreatedAt), GETDATE()) AS MinutosDesdeUltimaSync
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%OK%'
  AND CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY SourceDB, TableName
ORDER BY SourceDB, TableName;
GO

-- Tasa de errores (últimas 24 horas)
SELECT 
    COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) AS Exitosas,
    COUNT(CASE WHEN Message LIKE '%ERROR%' THEN 1 END) AS Fallidas,
    CAST(
        COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) * 100.0 / 
        NULLIF(COUNT(*), 0) 
    AS DECIMAL(5,2)) AS PorcentajeExito
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
  AND Message LIKE '%CHANGE TRACKING ETL%';
GO

-- ============================================================================
-- REPORTE FINAL - HEALTH CHECK
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'REPORTE FINAL - HEALTH CHECK DEL SISTEMA';
PRINT '============================================================================';
GO

USE CONSOLIDADA;
GO

-- Resumen de watermarks
PRINT '';
PRINT '-- Watermarks Actuales --';
SELECT 
    SourceDB,
    TableName,
    LastVersion AS Watermark,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar,
    CASE 
        WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 60 THEN '?? MÁS DE 1 HORA'
        WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 30 THEN '?? MÁS DE 30 MIN'
        ELSE '? OK'
    END AS Estado
FROM dbo.ConsolidationEngineWatermark
ORDER BY SourceDB, TableName;
GO

-- Errores activos
PRINT '';
PRINT '-- Errores Activos --';
SELECT 
    COUNT(*) AS ErroresActivos,
    COUNT(CASE WHEN DATEDIFF(HOUR, CreatedAt, GETDATE()) > 24 THEN 1 END) AS ErroresAntiguos
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1;
GO

-- Últimos logs de error
PRINT '';
PRINT '-- Últimos Errores Críticos --';
SELECT TOP 5
    LogLevel,
    LEFT(Message, 200) AS Mensaje,
    SourceDatabase,
    TableName,
    CreatedAt
FROM dbo.ConsolidationEngineLogs
WHERE LogLevel = 'Error'
ORDER BY Id DESC;
GO

-- Health Score
DECLARE @ErrorCount INT;
DECLARE @OldSyncCount INT;
DECLARE @HealthStatus NVARCHAR(50);

SELECT @ErrorCount = COUNT(*)
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1;

SELECT @OldSyncCount = COUNT(*)
FROM dbo.ConsolidationEngineWatermark
WHERE DATEDIFF(HOUR, UpdatedAt, GETDATE()) > 2;

IF @ErrorCount = 0 AND @OldSyncCount = 0
    SET @HealthStatus = '? SISTEMA SALUDABLE';
ELSE IF @ErrorCount > 0 OR @OldSyncCount > 0
    SET @HealthStatus = '??  ATENCIÓN REQUERIDA';

PRINT '';
PRINT '-- Health Status --';
SELECT 
    @HealthStatus AS EstadoGeneral,
    @ErrorCount AS ErroresActivos,
    @OldSyncCount AS SincronizacionesAntiguas;
GO

PRINT '';
PRINT '============================================================================';
PRINT 'FIN DE PRUEBAS FUNCIONALES';
PRINT '============================================================================';
PRINT '';
PRINT 'IMPORTANTE:';
PRINT '- Este script está adaptado para trabajar con tus BDs existentes';
PRINT '- Las secciones comentadas deben ajustarse según tu esquema real';
PRINT '- NO ejecutar los ejemplos de INSERT/UPDATE sin revisar el impacto';
PRINT '- Usar MonitoringQueries.sql para monitoreo continuo';
PRINT '';
PRINT '============================================================================';
GO
