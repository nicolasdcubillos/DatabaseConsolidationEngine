DECLARE @dbs TABLE (DbName SYSNAME);
INSERT INTO @dbs VALUES ('PASTO'),('BOGOTA');

DECLARE @db SYSNAME, @sql NVARCHAR(MAX);
DECLARE cur CURSOR FOR SELECT DbName FROM @dbs;

OPEN cur;
FETCH NEXT FROM cur INTO @db;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- Deshabilitar Change Tracking a nivel de tabla y BD
    SET @sql = '
    USE ' + QUOTENAME(@db) + ';
    
    IF EXISTS (
        SELECT 1 FROM sys.change_tracking_tables WHERE object_id = OBJECT_ID(''dbo.MVTOAUXNIF'')
    )
    BEGIN
        ALTER TABLE dbo.MVTOAUXNIF
        DISABLE CHANGE_TRACKING;
    END;

    IF EXISTS (
        SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID()
    )
    BEGIN
        ALTER DATABASE ' + QUOTENAME(@db) + '
        SET CHANGE_TRACKING = OFF;
    END;

    USE CONSOLIDADA;
    DELETE FROM dbo.ConsolidationEngineWatermark
    WHERE SourceServer = @@SERVERNAME
      AND SourceDB = ''' + @db + '''
      AND TableName = ''dbo.MVTOAUXNIF'';';
    
    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @db;
END
CLOSE cur;
DEALLOCATE cur;
