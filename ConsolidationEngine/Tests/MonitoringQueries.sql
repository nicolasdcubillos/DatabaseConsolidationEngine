/*******************************************************************************
 * QUERIES DE MONITOREO - CONSOLIDATION ENGINE
 * 
 * Propósito: Colección de queries útiles para monitorear el sistema
 *            en producción y diagnosticar problemas.
 *
 * IMPORTANTE: Este script usa las bases de datos EXISTENTES:
 *             - BOGOTA (Origin)
 *             - PASTO (Origin)
 *             - CONSOLIDADA (Target - donde se ejecutan estos queries)
 *
 * Autor: Consolidation Engine Team
 * Fecha: 2025
 *
 * INSTRUCCIONES:
 * - La mayoría de queries se ejecutan en CONSOLIDADA
 * - Ejecutar queries individuales según necesidad
 * - Agregar a tareas programadas o dashboards de monitoreo
 ******************************************************************************/

-- ============================================================================
-- CONFIGURACIÓN
-- ============================================================================
USE CONSOLIDADA;
GO

PRINT '============================================================================';
PRINT 'CONSOLIDATION ENGINE - QUERIES DE MONITOREO';
PRINT '============================================================================';
PRINT 'Base de datos Target: CONSOLIDADA';
PRINT 'Bases de datos Origin: BOGOTA, PASTO';
PRINT 'Server: ' + @@SERVERNAME;
PRINT '============================================================================';
GO

-- ============================================================================
-- 1. ESTADO GENERAL DEL SISTEMA
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '1. ESTADO GENERAL DEL SISTEMA';
PRINT '============================================================================';
GO

-- Resumen de Watermarks (última sincronización por tabla)
SELECT 
    SourceDB AS OrigenDB,
    TableName AS Tabla,
    LastVersion AS Watermark,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar,
    CASE 
        WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 60 THEN '?? MÁS DE 1 HORA'
        WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 30 THEN '?? MÁS DE 30 MIN'
        ELSE '? OK'
    END AS Estado
FROM dbo.ConsolidationEngineWatermark
ORDER BY SourceDB, TableName, UpdatedAt DESC;
GO

-- Resumen por base de datos origen
SELECT 
    SourceDB AS OrigenDB,
    COUNT(*) AS TotalTablas,
    AVG(DATEDIFF(MINUTE, UpdatedAt, GETDATE())) AS PromedioMinutosSinSync,
    MAX(DATEDIFF(MINUTE, UpdatedAt, GETDATE())) AS MaxMinutosSinSync
FROM dbo.ConsolidationEngineWatermark
GROUP BY SourceDB
ORDER BY SourceDB;
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
    SourceDatabase AS OrigenDB,
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

-- Conteo de errores por origen y tabla
SELECT 
    SourceDatabase AS OrigenDB,
    TableName AS Tabla,
    COUNT(*) AS TotalErrores,
    COUNT(DISTINCT SourceKey) AS RegistrosAfectados,
    MIN(CreatedAt) AS PrimerError,
    MAX(CreatedAt) AS UltimoError
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
GROUP BY SourceDatabase, TableName
ORDER BY SourceDatabase, TotalErrores DESC;
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
    SourceDatabase AS OrigenDB,
    COUNT(*) AS ErroresGenerados,
    COUNT(CASE WHEN Retry = 2 THEN 1 END) AS ErroresResueltos,
    COUNT(CASE WHEN Retry = 1 THEN 1 END) AS ErroresPendientes
FROM dbo.ConsolidationEngineErrors
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY DATEPART(HOUR, CreatedAt), SourceDatabase
ORDER BY Hora DESC, OrigenDB;
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
    SourceDatabase AS OrigenDB,
    TableName AS Tabla,
    CreatedAt AS Fecha
FROM dbo.ConsolidationEngineLogs
ORDER BY Id DESC;
GO

-- Resumen de logs por nivel y origen
SELECT 
    SourceDatabase AS OrigenDB,
    LogLevel AS Nivel,
    COUNT(*) AS Cantidad,
    MAX(CreatedAt) AS UltimoLog
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY SourceDatabase, LogLevel
ORDER BY 
    OrigenDB,
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
    SourceDatabase AS OrigenDB,
    TableName AS Tabla,
    COUNT(*) AS Ocurrencias,
    MAX(CreatedAt) AS UltimaOcurrencia
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%WATERMARK MISMATCH%'
  AND CreatedAt >= DATEADD(DAY, -7, GETDATE())
GROUP BY SourceDatabase, TableName
HAVING COUNT(*) > 0;
GO

-- Alerta: Errores sin resolver por más de 24 horas
SELECT 
    '?? ERRORES ANTIGUOS' AS TipoAlerta,
    SourceDatabase AS OrigenDB,
    COUNT(*) AS ErroresPendientes,
    MIN(CreatedAt) AS ErrorMasAntiguo,
    MAX(CreatedAt) AS ErrorMasReciente
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
  AND CreatedAt < DATEADD(HOUR, -24, GETDATE())
GROUP BY SourceDatabase;
GO

-- Alerta: Watermark sin actualizar por más de 2 horas
SELECT 
    '?? SINCRONIZACIÓN DETENIDA' AS TipoAlerta,
    SourceDB AS OrigenDB,
    TableName AS Tabla,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosSinActualizar,
    UpdatedAt AS UltimaActualizacion
FROM dbo.ConsolidationEngineWatermark
WHERE UpdatedAt < DATEADD(HOUR, -2, GETDATE())
ORDER BY SourceDB, UpdatedAt;
GO

-- ============================================================================
-- 6. PERFORMANCE Y ESTADÍSTICAS
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '6. PERFORMANCE Y ESTADÍSTICAS';
PRINT '============================================================================';
GO

-- Estadísticas de procesamiento por origen (últimas 24 horas)
SELECT 
    SourceDatabase AS OrigenDB,
    DATEPART(HOUR, CreatedAt) AS Hora,
    COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) AS SincronizacionesExitosas,
    COUNT(CASE WHEN Message LIKE '%UPSERT:%' THEN 1 END) AS BatchsConWarning,
    COUNT(CASE WHEN Message LIKE '%ERROR%' THEN 1 END) AS ErroresCriticos
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
  AND LogLevel IN ('Information', 'Warning', 'Error')
GROUP BY SourceDatabase, DATEPART(HOUR, CreatedAt)
ORDER BY OrigenDB, Hora DESC;
GO

-- Volumen de datos procesados por origen (últimas 24 horas)
SELECT 
    SourceDatabase AS OrigenDB,
    TableName AS Tabla,
    COUNT(*) AS EjecucionesETL,
    MAX(CreatedAt) AS UltimaEjecucion,
    DATEDIFF(MINUTE, MAX(CreatedAt), GETDATE()) AS MinutosDesdeUltimaEjecucion
FROM dbo.ConsolidationEngineLogs
WHERE Message LIKE '%Procesados:%'
  AND CreatedAt >= DATEADD(DAY, -1, GETDATE())
GROUP BY SourceDatabase, TableName
ORDER BY OrigenDB, UltimaEjecucion DESC;
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

-- Health check por base de datos origen
SELECT 
    SourceDB AS OrigenDB,
    COUNT(*) AS TotalTablas,
    COUNT(CASE WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) < 30 THEN 1 END) AS TablasOK,
    COUNT(CASE WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) >= 30 THEN 1 END) AS TablasConRetraso,
    CASE 
        WHEN COUNT(CASE WHEN DATEDIFF(MINUTE, UpdatedAt, GETDATE()) >= 30 THEN 1 END) = 0 
        THEN '? OK'
        ELSE '??  REVISAR'
    END AS Estado
FROM dbo.ConsolidationEngineWatermark
GROUP BY SourceDB
ORDER BY SourceDB;
GO

-- ============================================================================
-- 8. DIAGNÓSTICO DE TABLA ESPECÍFICA
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '8. DIAGNÓSTICO DE TABLA ESPECÍFICA';
PRINT '============================================================================';
PRINT 'Ajustar @OriginDB y @TableName según necesidad';
GO

-- AJUSTAR ESTOS VALORES según la tabla que quieras diagnosticar
DECLARE @OriginDB NVARCHAR(128) = 'BOGOTA';  -- o 'PASTO'
DECLARE @TableName NVARCHAR(128) = 'dbo.TuTabla';  -- Ajustar al nombre real

-- Watermark actual
SELECT 
    'Watermark Info' AS Tipo,
    SourceDB AS OrigenDB,
    LastVersion AS Watermark,
    UpdatedAt AS UltimaActualizacion,
    DATEDIFF(MINUTE, UpdatedAt, GETDATE()) AS MinutosDesdeUpdate
FROM dbo.ConsolidationEngineWatermark
WHERE SourceDB = @OriginDB AND TableName = @TableName;

-- Errores para esta tabla
SELECT 
    'Errores Activos' AS Tipo,
    COUNT(*) AS Total,
    MIN(CreatedAt) AS PrimerError,
    MAX(CreatedAt) AS UltimoError
FROM dbo.ConsolidationEngineErrors
WHERE SourceDatabase = @OriginDB AND TableName = @TableName AND Retry = 1;

-- Últimos logs
SELECT TOP 10
    'Logs Recientes' AS Tipo,
    LogLevel AS Nivel,
    LEFT(Message, 100) AS Mensaje,
    CreatedAt AS Fecha
FROM dbo.ConsolidationEngineLogs
WHERE SourceDatabase = @OriginDB AND TableName = @TableName
ORDER BY Id DESC;
GO

-- ============================================================================
-- 9. CAMBIOS PENDIENTES EN ORIGIN
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '9. CAMBIOS PENDIENTES EN BOGOTA Y PASTO';
PRINT '============================================================================';
PRINT 'Queries para ejecutar en cada base de datos origen';
GO

-- Query para ejecutar en BOGOTA
PRINT '-- Para ejecutar en BOGOTA:';
PRINT '';
/*
USE BOGOTA;
GO

-- Ajustar @TableName según necesidad
DECLARE @TableName NVARCHAR(128) = 'dbo.TuTabla';
DECLARE @LastWatermark BIGINT;

SELECT @LastWatermark = LastVersion
FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
WHERE SourceDB = 'BOGOTA' AND TableName = @TableName;

IF @LastWatermark IS NULL
    SET @LastWatermark = 0;

SELECT 
    'BOGOTA - Cambios Pendientes' AS Estado,
    @TableName AS Tabla,
    COUNT(*) AS TotalCambios,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'I' THEN 1 END) AS Inserts,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'U' THEN 1 END) AS Updates,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'D' THEN 1 END) AS Deletes,
    MIN(SYS_CHANGE_VERSION) AS DesdeVersion,
    MAX(SYS_CHANGE_VERSION) AS HastaVersion,
    CHANGE_TRACKING_CURRENT_VERSION() AS VersionActual
FROM CHANGETABLE(CHANGES dbo.TuTabla, @LastWatermark) AS ct;
GO
*/

-- Query para ejecutar en PASTO
PRINT '-- Para ejecutar en PASTO:';
PRINT '';
/*
USE PASTO;
GO

DECLARE @TableName NVARCHAR(128) = 'dbo.TuTabla';
DECLARE @LastWatermark BIGINT;

SELECT @LastWatermark = LastVersion
FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
WHERE SourceDB = 'PASTO' AND TableName = @TableName;

IF @LastWatermark IS NULL
    SET @LastWatermark = 0;

SELECT 
    'PASTO - Cambios Pendientes' AS Estado,
    @TableName AS Tabla,
    COUNT(*) AS TotalCambios,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'I' THEN 1 END) AS Inserts,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'U' THEN 1 END) AS Updates,
    COUNT(CASE WHEN SYS_CHANGE_OPERATION = 'D' THEN 1 END) AS Deletes,
    MIN(SYS_CHANGE_VERSION) AS DesdeVersion,
    MAX(SYS_CHANGE_VERSION) AS HastaVersion,
    CHANGE_TRACKING_CURRENT_VERSION() AS VersionActual
FROM CHANGETABLE(CHANGES dbo.TuTabla, @LastWatermark) AS ct;
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

USE CONSOLIDADA;
GO

-- Ver datos candidatos a limpieza
SELECT 
    'Logs antiguos (>30 días)' AS Tipo,
    COUNT(*) AS Registros,
    MIN(CreatedAt) AS MasAntiguo,
    MAX(CreatedAt) AS MasReciente,
    CAST(COUNT(*) * 8.0 / 1024 / 1024 AS DECIMAL(10,2)) AS MB_Aprox
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt < DATEADD(DAY, -30, GETDATE());
GO

SELECT 
    'Errores resueltos (>30 días)' AS Tipo,
    COUNT(*) AS Registros,
    MIN(CreatedAt) AS MasAntiguo,
    MAX(CreatedAt) AS MasReciente
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 2  -- Reintentados exitosamente
  AND CreatedAt < DATEADD(DAY, -30, GETDATE());
GO

-- Queries de limpieza (comentadas por seguridad)
/*
-- ?? PRECAUCIÓN: Solo ejecutar en mantenimiento programado

-- Limpiar logs antiguos (>30 días)
DELETE FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt < DATEADD(DAY, -30, GETDATE());

PRINT '? ' + CAST(@@ROWCOUNT AS VARCHAR) + ' logs eliminados';

-- Limpiar errores resueltos antiguos (>30 días)
DELETE FROM dbo.ConsolidationEngineErrors
WHERE Retry = 2  -- Solo los exitosamente reintentados
  AND CreatedAt < DATEADD(DAY, -30, GETDATE());

PRINT '? ' + CAST(@@ROWCOUNT AS VARCHAR) + ' errores resueltos eliminados';

-- Reorganizar índices después de limpieza
ALTER INDEX ALL ON dbo.ConsolidationEngineLogs REORGANIZE;
ALTER INDEX ALL ON dbo.ConsolidationEngineErrors REORGANIZE;

PRINT '? Limpieza completada';
*/

-- ============================================================================
-- 11. QUERIES PARA DASHBOARDS / MONITOREO AUTOMÁTICO
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '11. QUERIES PARA DASHBOARDS / MONITOREO AUTOMÁTICO';
PRINT '============================================================================';
GO

USE CONSOLIDADA;
GO

-- Métrica 1: Errores en las últimas 24 horas por origen
SELECT 
    SourceDatabase AS OrigenDB,
    COUNT(*) AS ErroresUltimas24H
FROM dbo.ConsolidationEngineErrors
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
GROUP BY SourceDatabase
ORDER BY SourceDatabase;
GO

-- Métrica 2: Tiempo desde última sincronización por origen (minutos)
SELECT 
    SourceDB AS OrigenDB,
    TableName AS Tabla,
    DATEDIFF(MINUTE, MAX(UpdatedAt), GETDATE()) AS MinutosSinSync
FROM dbo.ConsolidationEngineWatermark
GROUP BY SourceDB, TableName
ORDER BY OrigenDB, MinutosSinSync DESC;
GO

-- Métrica 3: Tasa de éxito por origen (últimas 24 horas)
SELECT 
    SourceDatabase AS OrigenDB,
    COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) AS Exitosas,
    COUNT(CASE WHEN Message LIKE '%ERROR%' THEN 1 END) AS Fallidas,
    CAST(
        COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) * 100.0 / 
        NULLIF(COUNT(*), 0) 
    AS DECIMAL(5,2)) AS PorcentajeExito
FROM dbo.ConsolidationEngineLogs
WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())
  AND Message LIKE '%CHANGE TRACKING ETL%'
GROUP BY SourceDatabase
ORDER BY OrigenDB;
GO

-- Métrica 4: Errores críticos activos por origen
SELECT 
    SourceDatabase AS OrigenDB,
    COUNT(*) AS ErroresCriticosActivos
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
GROUP BY SourceDatabase
ORDER BY OrigenDB;
GO

-- Métrica 5: Resumen ejecutivo (para dashboard principal)
SELECT 
    (SELECT COUNT(*) FROM dbo.ConsolidationEngineWatermark) AS TotalTablasSincronizadas,
    (SELECT COUNT(*) FROM dbo.ConsolidationEngineErrors WHERE Retry = 1) AS ErroresActivos,
    (SELECT COUNT(*) FROM dbo.ConsolidationEngineWatermark WHERE DATEDIFF(MINUTE, UpdatedAt, GETDATE()) > 60) AS TablasDesactualizadas,
    (SELECT 
        CAST(COUNT(CASE WHEN Message LIKE '%OK%' THEN 1 END) * 100.0 / NULLIF(COUNT(*), 0) AS DECIMAL(5,2))
     FROM dbo.ConsolidationEngineLogs
     WHERE CreatedAt >= DATEADD(HOUR, -24, GETDATE())) AS TasaExito24H;
GO

-- ============================================================================
-- 12. ANÁLISIS DE ERRORES RECURRENTES
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT '12. ANÁLISIS DE ERRORES RECURRENTES';
PRINT '============================================================================';
GO

USE CONSOLIDADA;
GO

-- Top 10 errores más frecuentes por origen
SELECT TOP 10
    SourceDatabase AS OrigenDB,
    LEFT(ErrorMessage, 100) AS ErrorResumen,
    COUNT(*) AS Ocurrencias,
    COUNT(DISTINCT SourceKey) AS RegistrosAfectados,
    MIN(CreatedAt) AS PrimeraOcurrencia,
    MAX(CreatedAt) AS UltimaOcurrencia
FROM dbo.ConsolidationEngineErrors
WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())
GROUP BY SourceDatabase, LEFT(ErrorMessage, 100)
ORDER BY Ocurrencias DESC;
GO

-- Registros con múltiples errores (problemáticos)
SELECT 
    SourceDatabase AS OrigenDB,
    SourceKey,
    TableName AS Tabla,
    COUNT(*) AS NumeroDeErrores,
    MAX(ErrorMessage) AS UltimoError,
    MAX(CreatedAt) AS UltimaOcurrencia
FROM dbo.ConsolidationEngineErrors
WHERE Retry = 1
GROUP BY SourceDatabase, SourceKey, TableName
HAVING COUNT(*) > 3
ORDER BY NumeroDeErrores DESC;
GO

-- ============================================================================
-- FIN DE QUERIES DE MONITOREO
-- ============================================================================
PRINT '';
PRINT '============================================================================';
PRINT 'MONITOREO COMPLETADO - CONSOLIDADA';
PRINT '============================================================================';
PRINT 'Todas las queries ejecutadas en la base de datos CONSOLIDADA';
PRINT 'Para consultar cambios pendientes, ejecutar queries comentadas en BOGOTA/PASTO';
PRINT '';
PRINT 'Bases monitoreadas:';
PRINT '  - BOGOTA (Origen)';
PRINT '  - PASTO (Origen)';
PRINT '  - CONSOLIDADA (Destino)';
PRINT '============================================================================';
GO
