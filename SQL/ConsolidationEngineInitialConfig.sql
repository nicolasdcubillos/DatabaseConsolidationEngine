USE CONSOLIDADA;
GO

-------------------------------------------------------------------
-- 1. Crear tablas de control si no existen
-------------------------------------------------------------------
IF OBJECT_ID('CONSOLIDADA.dbo.ConsolidationEngineWatermark', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsolidationEngineWatermark (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SourceServer SYSNAME NOT NULL,
        SourceDB SYSNAME NOT NULL,
        TableName SYSNAME NOT NULL,
        LastVersion BIGINT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('CONSOLIDADA.dbo.ConsolidationEngineErrors', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsolidationEngineErrors (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SourceKey NVARCHAR(200) NULL,
        SourceDatabase NVARCHAR(200) NULL,
        TableName NVARCHAR(200) NOT NULL,
        Operation NVARCHAR(50) NOT NULL, -- BULK | ROW
        ErrorMessage NVARCHAR(500) NOT NULL,
        ErrorDetails NVARCHAR(MAX) NULL,
        Payload NVARCHAR(MAX) NULL,
        RetryCount INT NOT NULL DEFAULT 0,
        Retry BIT DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('CONSOLIDADA.dbo.ConsolidationEngineLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsolidationEngineLogs (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        LogLevel NVARCHAR(50) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        SourceDatabase NVARCHAR(200) NULL,
        TargetDatabase NVARCHAR(200) NULL,
        TableName NVARCHAR(200) NULL,
        Payload NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;
GO

-------------------------------------------------------------------
-- 2. Crear vistas de errores y estado de consolidación
-------------------------------------------------------------------

CREATE OR ALTER VIEW dbo.ConsolidationEngineErrorsView
AS
SELECT TOP (25)
    ce.Id,
    ce.SourceDatabase,
    ce.TableName,
    ce.ErrorMessage,
    ce.CreatedAt
FROM CONSOLIDADA.dbo.ConsolidationEngineErrors ce
ORDER BY ce.CreatedAt DESC;
GO

CREATE OR ALTER PROCEDURE dbo.ConsolidationEngineStatus
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @db sysname, @sql NVARCHAR(MAX);

    -- tabla temporal para acumular resultados
    CREATE TABLE #result (
        SourceDB sysname,
        LocalVersion bigint,
        ConsolidadaVersion bigint,
        Estado varchar(50),
        Errores int,
        UltimoError datetime2
    );

    DECLARE dbs CURSOR FOR
    SELECT DISTINCT SourceDB
    FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark;

    OPEN dbs;
    FETCH NEXT FROM dbs INTO @db;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sql = N'
        USE ' + QUOTENAME(@db) + N';
        DECLARE @localVersion bigint = CHANGE_TRACKING_CURRENT_VERSION();
        USE CONSOLIDADA;
        INSERT INTO #result
        SELECT
            N''' + @db + N''' AS SourceDB,
            @localVersion AS LocalVersion,
            (SELECT MAX(LastVersion)
             FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
             WHERE SourceDB = N''' + @db + N''') AS ConsolidadaVersion,
            CASE
                WHEN (SELECT MAX(LastVersion)
                      FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
                      WHERE SourceDB = N''' + @db + N''') IS NULL THEN ''Sin consolidar''
                WHEN @localVersion =
                     (SELECT MAX(LastVersion)
                      FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
                      WHERE SourceDB = N''' + @db + N''') THEN ''Sincronizada''
                WHEN @localVersion <
                     (SELECT MAX(LastVersion)
                      FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
                      WHERE SourceDB = N''' + @db + N''') THEN ''Desactualizada''
                WHEN @localVersion >
                     (SELECT MAX(LastVersion)
                      FROM CONSOLIDADA.dbo.ConsolidationEngineWatermark
                      WHERE SourceDB = N''' + @db + N''') THEN ''Pendiente''
                ELSE ''Error''
            END AS Estado,
            ISNULL((SELECT COUNT(*) FROM CONSOLIDADA.dbo.ConsolidationEngineErrors WHERE SourceDatabase = N''' + @db + N'''), 0) AS Errores,
            (SELECT MAX(CreatedAt) FROM CONSOLIDADA.dbo.ConsolidationEngineErrors WHERE SourceDatabase = N''' + @db + N''') AS UltimoError;
        ';

        EXEC sys.sp_executesql @sql;

        FETCH NEXT FROM dbs INTO @db;
    END;

    CLOSE dbs;
    DEALLOCATE dbs;

    SELECT * FROM #result;
END;
GO

-------------------------------------------------------------------
-- 3. Configurar bases y tablas a consolidar con Change Tracking
-------------------------------------------------------------------
DECLARE @dbs TABLE (DbName SYSNAME);

INSERT INTO @dbs VALUES ('PASTO'), ('BOGOTA');
/*
INSERT INTO @dbs
VALUES 
    ('ADMCONCESIONES'),
    ('AMADEUS'),
    ('ARMENIA'),
    ('BICENTENARIO'),
    ('BICINDEPENDENCIA'),
    ('CALI'),
    ('COMDEPORCALI'),
    ('IBAGUE'),
    ('IPIALES'),
    ('LAESPERANZA'),
    ('LOSNARANJOS'),
    ('MANIZALES'),
    ('MARISTAS'),
    ('POPAYAN'),
    ('SOLACOSTA'),
    ('VILLAVICENCIO');*/

DECLARE @tables TABLE (SchemaName SYSNAME, TableName SYSNAME);
INSERT INTO @tables VALUES ('dbo', 'MVTONIIF'),
                           ('dbo', 'NIT'),
                           ('dbo', 'CENTCOS');

DECLARE @db SYSNAME, @schema SYSNAME, @table SYSNAME, @sql NVARCHAR(MAX);

DECLARE cur CURSOR FOR 
    SELECT d.DbName, t.SchemaName, t.TableName
    FROM @dbs d CROSS JOIN @tables t;

OPEN cur;
FETCH NEXT FROM cur INTO @db, @schema, @table;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = '
    USE ' + QUOTENAME(@db) + ';

    -- Habilitar Change Tracking a nivel de BD si no está
    IF NOT EXISTS (
        SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID()
    )
    BEGIN
        ALTER DATABASE ' + QUOTENAME(@db) + '
        SET CHANGE_TRACKING = ON
        (CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON);
    END;

    -- Habilitar Change Tracking en la tabla indicada si no está
    IF NOT EXISTS (
        SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(''' 
            + QUOTENAME(@schema) + '.' + QUOTENAME(@table) + ''')
    )
    BEGIN
        ALTER TABLE ' + QUOTENAME(@schema) + '.' + QUOTENAME(@table) + '
        ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
    END;

    -- Obtener versión actual de Change Tracking en esta BD
    DECLARE @cur BIGINT;
    SELECT @cur = CHANGE_TRACKING_CURRENT_VERSION();

    -- Registrar watermark en CONSOLIDADA
    USE CONSOLIDADA;
    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConsolidationEngineWatermark
        WHERE SourceServer = @@SERVERNAME
          AND SourceDB = ''' + @db + '''
          AND TableName = ''' + QUOTENAME(@schema) + '.' + QUOTENAME(@table) + ''')
    BEGIN
        INSERT INTO dbo.ConsolidationEngineWatermark (SourceServer, SourceDB, TableName, LastVersion)
        VALUES (@@SERVERNAME, ''' + @db + ''', ''' + QUOTENAME(@schema) + '.' + QUOTENAME(@table) + ''', @cur);
    END;';

    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @db, @schema, @table;
END

CLOSE cur;
DEALLOCATE cur;

-------------------------------------------------------------------
-- 4. Asegurar columna SourceKey en tablas CONSOLIDADA
-------------------------------------------------------------------
DECLARE curCons CURSOR FOR
SELECT SchemaName, TableName FROM @tables;

OPEN curCons;
FETCH NEXT FROM curCons INTO @schema, @table;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = '
    IF NOT EXISTS (
        SELECT 1
        FROM CONSOLIDADA.INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = ''' + @schema + '''
          AND TABLE_NAME = ''' + @table + '''
          AND COLUMN_NAME = ''SourceKey''
    )
    BEGIN
        ALTER TABLE CONSOLIDADA.' + QUOTENAME(@schema) + '.' + QUOTENAME(@table) + '
        ADD SourceKey VARCHAR(1000) NULL;
        PRINT ''Columna SourceKey agregada a ' + @schema + '.' + @table + '''; 
    END
    ELSE
    BEGIN
        PRINT ''La columna SourceKey ya existe en ' + @schema + '.' + @table + '''; 
    END';

    EXEC sp_executesql @sql;

    FETCH NEXT FROM curCons INTO @schema, @table;
END

CLOSE curCons;
DEALLOCATE curCons;
GO
