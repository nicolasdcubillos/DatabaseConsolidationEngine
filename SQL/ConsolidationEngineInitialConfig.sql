DECLARE @dbs TABLE (DbName SYSNAME);
INSERT INTO @dbs VALUES ('PASTO'), ('BOGOTA');

DECLARE @tables TABLE (SchemaName SYSNAME, TableName SYSNAME);
INSERT INTO @tables VALUES ('dbo', 'MVTONIIF'),
                           ('dbo', 'NIT'),
                           ('dbo', 'CENTCOS');

DECLARE @db SYSNAME, @schema SYSNAME, @table SYSNAME, @sql NVARCHAR(MAX);

-- Primero asegurar que la tabla de watermark existe en CONSOLIDADA
IF OBJECT_ID('CONSOLIDADA.dbo.ConsolidationEngineWatermark', 'U') IS NULL
BEGIN
    USE CONSOLIDADA;

    CREATE TABLE dbo.ConsolidationEngineWatermark (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        SourceServer SYSNAME NOT NULL,
        SourceDB SYSNAME NOT NULL,
        TableName SYSNAME NOT NULL,
        LastVersion BIGINT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
    );
END;

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