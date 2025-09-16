DECLARE @dbs TABLE (DbName SYSNAME);
INSERT INTO @dbs VALUES ('PASTO'), ('BOGOTA');

DECLARE @db SYSNAME, @sql NVARCHAR(MAX);

DECLARE cur CURSOR FOR SELECT DbName FROM @dbs;

OPEN cur;
FETCH NEXT FROM cur INTO @db;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = '
    USE ' + QUOTENAME(@db) + ';

    -- Solo si el Change Tracking está habilitado a nivel BD
    IF EXISTS (SELECT 1 FROM sys.change_tracking_databases WHERE database_id = DB_ID())
    BEGIN
        DECLARE @table NVARCHAR(MAX), @stmt NVARCHAR(MAX);

        -- Recorremos solo tablas con Change Tracking habilitado
        DECLARE tab_cur CURSOR FOR
            SELECT QUOTENAME(s.name) + ''.'' + QUOTENAME(t.name)
            FROM sys.change_tracking_tables ct
            JOIN sys.tables t ON ct.object_id = t.object_id
            JOIN sys.schemas s ON t.schema_id = s.schema_id;

        OPEN tab_cur;
        FETCH NEXT FROM tab_cur INTO @table;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @stmt = ''ALTER TABLE '' + @table + '' DISABLE CHANGE_TRACKING;'';
            EXEC (@stmt);
            FETCH NEXT FROM tab_cur INTO @table;
        END

        CLOSE tab_cur;
        DEALLOCATE tab_cur;

        -- Ahora sí apagamos Change Tracking a nivel BD
        ALTER DATABASE ' + QUOTENAME(@db) + ' SET CHANGE_TRACKING = OFF;

        PRINT ''Change Tracking deshabilitado en base de datos: ' + @db + ''';
    END
    ELSE
    BEGIN
        PRINT ''Change Tracking no estaba habilitado en base de datos: ' + @db + ''';
    END;

    -- Regresamos a CONSOLIDADA y borramos la tabla watermark si existe
    USE CONSOLIDADA;

    IF OBJECT_ID(''dbo.ConsolidationEngineWatermark'', ''U'') IS NOT NULL
        DROP TABLE dbo.ConsolidationEngineWatermark;
    ';

    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @db;
END

CLOSE cur;
DEALLOCATE cur;
