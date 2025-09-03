DECLARE @dbs TABLE (DbName SYSNAME);
INSERT INTO @dbs VALUES ('PASTO'),('BOGOTA');

DECLARE @db SYSNAME, @sql NVARCHAR(MAX);
DECLARE cur CURSOR FOR SELECT DbName FROM @dbs;

OPEN cur;
FETCH NEXT FROM cur INTO @db;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = '
    USE ' + QUOTENAME(@db) + ';
    IF NOT EXISTS (
        SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID()
    )
    BEGIN
        ALTER DATABASE ' + QUOTENAME(@db) + '
        SET CHANGE_TRACKING = ON
        (CHANGE_RETENTION = 7 DAYS, AUTO_CLEANUP = ON);
    END;

    IF NOT EXISTS (
        SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(''dbo.MVTOAUXNIF'')
    )
    BEGIN
        ALTER TABLE dbo.MVTOAUXNIF
        ENABLE CHANGE_TRACKING WITH (TRACK_COLUMNS_UPDATED = OFF);
    END;

    USE CONSOLIDADA;
    DECLARE @cur BIGINT;
    SET @cur = ISNULL(
        (SELECT CHANGE_TRACKING_CURRENT_VERSION() FROM ' + QUOTENAME(@db) + '.sys.change_tracking_databases), 0);

    IF NOT EXISTS (
        SELECT 1 FROM dbo.ConsolidationEngineWatermark
        WHERE SourceServer = @@SERVERNAME
          AND SourceDB = ''' + @db + '''
          AND TableName = ''dbo.MVTOAUXNIF'')
    BEGIN
        INSERT INTO dbo.ConsolidationEngineWatermark (SourceServer, SourceDB, TableName, LastVersion)
        VALUES (@@SERVERNAME, ''' + @db + ''', ''dbo.MVTOAUXNIF'', @cur);
    END;';
    
    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @db;
END
CLOSE cur;
DEALLOCATE cur;
