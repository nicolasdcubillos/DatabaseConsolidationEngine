/*******************************************************************************
 * QUERIES DE MONITOREO - CONSOLIDATION ENGINE
 * 
 * Propósito: Colección de queries útiles para monitorear el sistema
 *            en producción y diagnosticar problemas.
 *
 * Autor: Consolidation Engine Team
 * Fecha: 2025
 *
 * INSTRUCCIONES:
 * - Ajustar nombres de base de datos según tu entorno
 * - Ejecutar queries individuales según necesidad
 * - Agregar a tareas programadas o dashboards de monitoreo
 ******************************************************************************/

-- ============================================================================
-- CONFIGURACIÓN
-- ============================================================================
-- Ajustar según tu entorno de producción
DECLARE @TargetDB NVARCHAR(128) = 'ConsolidationTest_Target';
DECLARE @OriginDB NVARCHAR(128) = 'ConsolidationTest_Origin';
GO

-- ============================================================================
-- 1. ESTADO GENERAL DEL SISTEMA
-- ============================================================================
PRINT '============================================================================';
PRINT '1. ESTADO GENERAL DEL SISTEMA';
PRINT '============================================================================';
GO

-- Resumen de Watermarks (última sincronización por tabla)
SELECT 
    TableName AS Tabla,
    SourceDB AS OrigenDB,
    LastVersion AS Watermark,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar,
    CASE 
        WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 60 THEN '?? MÁS DE 1 HORA'
        WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 30 THEN '?? MÁS DE 30 MIN'
        ELSE '? OK'
    END AS Estado
FROM dbo.ConsolidationEngineWatermark
ORDER BY UpdatedAt DESC;
GO

-- ============================================================================
-- 2. ERRORES ACTIVOS (Requieren Atención)
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '2. ERRORES ACTIVOS (Retry = 1)';
PRINT '============================================================================';
GO

SELECT 
    Id,
    SourceKey,
    TableName AS Tabla,
    Operation AS Operacion,
    LEFT(ErrorMessage, 100) AS ErrorResumen,
    RetryCount AS Intentos,
    DATEDIFF(HOUR, CreatedAt, GETDATE()) AS HorasDesdeError,
    CreatedAt AS FechaError
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
ORDER BY CreatedAt DESC;
GO

-- Conteo de errores por tabla
SELECT 
    TableName AS Tabla,
    COUNT(*) AS TotalErrores,
    COUNT(DISTINCT SourceKey) AS RegistrosAfectados,
    MIN(CreatedAt) AS PrimerError,
    MAX(CreatedAt) AS UltimoError
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
GROUP BY TableName
ORDER BY TotalErrores DESC;
GO

-- ============================================================================
-- 3. TENDENCIA DE ERRORES (Últimas 24 horas)
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '3. TENDENCIA DE ERRORES (Últimas 24 horas)';
PRINT '============================================================================';
GO

SELECT 
    DATEPART(HOUR, CreatedAt) AS Hora,
    COUNT(*) AS ErroresGenerados,
    COUNT(CASE WHEN Retry = 2 THEN 1 END) AS ErroresResueltos,
    COUNT(CASE WHEN Retry = 1 THEN 1 END) AS ErroresPendientes
FROM dbo.ConsolidationEngineErrors
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY DATEPART(HOUR, CreatedAt)
ORDER BY Hora;
GO

-- ============================================================================
-- 4. LOGS RECIENTES (Últimos 50)
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '4. LOGS RECIENTES';
PRINT '============================================================================';
GO

SELECT TOP 50
    Id,
    LogLevel AS Nivel,
    LEFT(Message, 150) AS Mensaje,
    TableName AS Tabla,
    CreatedAt AS Fecha
FROM dbo.ConsolidationEngineLogs
ORDER BY Id DESC;
GO

-- Resumen de logs por nivel
SELECT 
    LogLevel AS Nivel,
    COUNT(*) AS Cantidad,
    MAX(CreatedAt) AS UltimoLog
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY LogLevel
ORDER BY 
    CASE LogLevel
        WHEN 'Error' THEN 1
        WHEN 'Warning' THEN 2
        WHEN 'Information' THEN 3
        ELSE 4
    END;
GO

-- ============================================================================
-- 5. ALERTAS CRÍTICAS
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '5. ??  ALERTAS CRÍTICAS';
PRINT '============================================================================';
GO

-- Alerta: Watermark Mismatch
SELECT 
    '?? WATERMARK MISMATCH' AS TipoAlerta,
    COUNT(*) AS Ocurrencias,
    MAX(CreatedAt) AS UltimaOcurrencia
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%WATERMARK MISMATCH%'
  AND CreatedAt >= DATEADD(DAY, -7, GETDATE())
HAVING COUNT(*) > 0;
GO

-- Alerta: Errores sin resolver por más de 24 horas
SELECT 
    '?? ERRORES ANTIGUOS' AS TipoAlerta,
    COUNT(*) AS ErroresPendientes,
    MIN(CreatedAt) AS ErrorMasAntiguo,
    MAX(CreatedAt) AS ErrorMasReciente
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
  AND CreatedAt < DATEADD(HOUR, -24, GETDATE());
GO

-- Alerta: Watermark sin actualizar por más de 2 horas
SELECT 
    '?? SINCRONIZACIÓN DETENIDA' AS TipoAlerta,
    TableName AS Tabla,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar,
    UpdatedAt AS UltimaActualizacion
FROM dbo.ConsolidationEngineWatermark
WHERE UpdatedAt < DATEADD(HOUR, -2, GETDATE())
ORDER BY UpdatedAt;
GO

-- ============================================================================
-- 6. PERFORMANCE Y ESTADÍSTICAS
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '6. PERFORMANCE Y ESTADÍSTICAS';
PRINT '============================================================================';
GO

-- Estadísticas de procesamiento (últimas 24 horas)
SELECT 
    DATEPART(HOUR, CreatedAt) AS Hora,
    COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) AS SincronizacionesExitosas,
    COUNT(CASE WHEN Message LIKE '%UPSERT:%' THEN 1 END) AS BatchsConWarning,
    COUNT(CASE WHEN Message LIKE '%ERROR%' THEN 1 END) AS ErroresCriticos
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
  AND LogLevel IN ('Information', 'Warning', 'Error')
GROUP BY DATEPART(HOUR, CreatedAt)
ORDER BY Hora DESC;
GO

-- Volumen de datos procesados (extraído de logs)
SELECT 
    TableName AS Tabla,
    COUNT(*) AS EjecucionesETL,
    MAX(CreatedAt) AS UltimaEjecucion
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%Procesados:%'
  AND CreatedAt >= DATEADD(DAY, -7, GETDATE())
GROUP BY TableName
ORDER BY UltimaEjecucion DESC;
GO

-- ============================================================================
-- 7. HEALTH CHECK RÁPIDO
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '7. ?? HEALTH CHECK RÁPIDO';
PRINT '============================================================================';
GO

-- Sistema saludable si todos los checks pasan
DECLARE @ErroresPendientes INT;
DECLARE @SyncAntigua INT;
DECLARE @WatermarkMismatch INT;
DECLARE @HealthStatus VARCHAR(50);

SELECT @ErroresPendientes = COUNT(*)
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1 AND CreatedAt < DATEADD(HOUR, -24, GETDATE());

SELECT @SyncAntigua = COUNT(*)
FROM dbo.ConsolidationEngineWatermark
WHERE UpdatedAt < DATEADD(HOUR, -2, GETDATE());

SELECT @WatermarkMismatch = COUNT(*)
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%WATERMARK MISMATCH%'
  AND CreatedAt >= DATEADD(DAY, -1, GETDATE());

IF @ErroresPendientes = 0 AND @SyncAntigua = 0 AND @WatermarkMismatch = 0
    SET @HealthStatus = '? SISTEMA SALUDABLE';
ELSE IF @ErroresPendientes > 0 OR @SyncAntigua > 0
    SET @HealthStatus = '??  ATENCIÓN REQUERIDA';
ELSE IF @WatermarkMismatch > 0
    SET @HealthStatus = '?? ALERTA CRÍTICA';

SELECT 
    @HealthStatus AS EstadoGeneral,
    @ErroresPendientes AS ErroresAntiguos,
    @SyncAntigua AS TablasDesactualizadas,
    @WatermarkMismatch AS WatermarkMismatchDetectados;
GO

-- ============================================================================
-- 8. DIAGNÓSTICO DE TABLA ESPECÍFICA
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '8. DIAGNÓSTICO DE TABLA ESPECÍFICA';
PRINT '============================================================================';
PRINT 'Ajustar @TableName según necesidad';
GO

DECLARE @TableName NVARCHAR(128) = 'dbo.Customers';

-- Watermark actual
SELECT 
    'Watermark Info' AS Tipo,
    LastVersion AS Watermark,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosDesdeUpdate
FROM dbo.ConsolidationEngineWatermark
WHERE TableName = @TableName;

-- Errores para esta tabla
SELECT 
    'Errores Activos' AS Tipo,
    COUNT(*) AS Total,
    MIN(CreatedAt) AS PrimerError,
    MAX(CreatedAt) AS UltimoError
FROM dbo.ConsolidationEngineErrors
WHERE TableName = @TableName AND Retry = 1;

-- Últimos logs
SELECT TOP 10
    'Logs Recientes' AS Tipo,
    LogLevel AS Nivel,
    LEFT(Message, 100) AS Mensaje,
    CreatedAt AS Fecha
FROM dbo.ConsolidationEngineLogs
WHERE TableName = @TableName
ORDER BY Id DESC;
GO

-- ============================================================================
-- 9. CAMBIOS PENDIENTES EN ORIGIN (Requiere conexión a Origin)
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '9. CAMBIOS PENDIENTES EN ORIGIN';
PRINT '============================================================================';
PRINT 'NOTA: Ajustar nombres de DB y ejecutar en contexto correcto';
GO

/*
-- Ejemplo para tabla Customers
USE ConsolidationTest_Origin;
GO

DECLARE @LastWatermark BIGINT;
SELECT @LastWatermark = LastVersion
FROM ConsolidationTest_Target.dbo.ConsolidationEngineWatermark
WHERE TableName = 'dbo.Customers';

SELECT 
    'Cambios Pendientes' AS Estado,
    COUNT(*) AS TotalCambios,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'I' THEN 1 END) AS Inserts,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'U' THEN 1 END) AS Updates,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'D' THEN 1 END) AS Deletes,
    MIN(SYS_CHANGE_VERSION) AS PrimeraVersion,
    MAX(SYS_CHANGE_VERSION) AS UltimaVersion
FROM CHANGETABLE(CHANGES dbo.Customers, @LastWatermark) AS ct;
GO
*/

-- ============================================================================
-- 10. LIMPIEZA DE DATOS HISTÓRICOS
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '10. LIMPIEZA DE DATOS HISTÓRICOS';
PRINT '============================================================================';
PRINT 'Queries para limpieza de datos antiguos (ejecutar con precaución)';
GO

-- Ver datos candidatos a limpieza
SELECT 
    'Logs antiguos (>30 días)' AS Tipo,
    COUNT(*) AS Registros,
    MIN(CreatedAt) AS MasAntiguo,
    MAX(CreatedAt) AS MasReciente
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt < DATEADD(DAY, -30, GETDATE());
GO

SELECT 
    'Errores resueltos (>30 días)' AS Tipo,
    COUNT(*) AS Registros
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 2  -- Reintentados exitosamente
  AND CreatedAt < DATEADD(DAY, -30, GETDATE());
GO

-- Queries de limpieza (comentadas por seguridad)
/*
-- Limpiar logs antiguos (>30 días)
DELETE FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt < DATEADD(DAY, -30, GETDATE());

-- Limpiar errores resueltos antiguos (>30 días)
DELETE FROM dbo.ConsolidationEngineErrors
WHERE Retry = 2  -- Solo los exitosamente reintentados
  AND CreatedAt < DATEADD(DAY, -30, GETDATE());

PRINT '? Limpieza completada';
*/

-- ============================================================================
-- 11. QUERIES PARA DASHBOARDS
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '11. QUERIES PARA DASHBOARDS / MONITOREO AUTOMÁTICO';
PRINT '============================================================================';
GO

-- Métrica 1: Errores en las últimas 24 horas
SELECT COUNT(*) AS ErroresUltimas24H
FROM dbo.ConsolidationEngineErrors
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE());
GO

-- Métrica 2: Tiempo desde última sincronización (minutos)
SELECT 
    TableName AS Tabla,
    DATEDIFF(MINUTE, MAX(UpdatedAt), GETDATE()) AS MinutosSinSync
FROM dbo.ConsolidationEngineWatermark
GROUP BY TableName;
GO

-- Métrica 3: Tasa de éxito (últimas 24 horas)
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

-- Métrica 4: Errores críticos activos
SELECT COUNT(*) AS ErroresCriticosActivos
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1;
GO

-- ============================================================================
-- 12. ANÁLISIS DE ERRORES RECURRENTES
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '12. ANÁLISIS DE ERRORES RECURRENTES';
PRINT '============================================================================';
GO

-- Top 10 errores más frecuentes
SELECT TOP 10
    LEFT(ErrorMessage, 100) AS ErrorResumen,
    COUNT(*) AS Ocurrencias,
    COUNT(DISTINCT SourceKey) AS RegistrosAfectados,
    MIN(CreatedAt) AS PrimeraOcurrencia,
    MAX(CreatedAt) AS UltimaOcurrencia
FROM dbo.ConsolidationEngineErrors
WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())
GROUP BY LEFT(ErrorMessage, 100)
ORDER BY Ocurrencias DESC;
GO

-- Registros con múltiples errores (problemáticos)
SELECT 
    SourceKey,
    TableName AS Tabla,
    COUNT(*) AS NumeroDeErrores,
    MAX(ErrorMessage) AS UltimoError,
    MAX(CreatedAt) AS UltimaOcurrencia
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
GROUP BY SourceKey, TableName
HAVING COUNT(*) > 3
ORDER BY NumeroDeErrores DESC;
GO

-- ============================================================================
-- FIN DE QUERIES DE MONITOREO
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'MONITOREO COMPLETADO';
PRINT '============================================================================';
PRINT 'Para queries específicas, ejecutar secciones individuales según necesidad.';
GO
