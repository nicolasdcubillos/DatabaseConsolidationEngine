DECLARE @dbs TABLE (DbName SYSNAME);
INSERT INTO @dbs VALUES ('PASTO'), ('BOGOTA');   -- <-- tus bases

DECLARE @tables TABLE (SchemaName SYSNAME, TableName SYSNAME);
INSERT INTO @tables VALUES ('dbo', 'MVTONIIF'),
                           ('dbo', 'NIT'),
                           ('dbo', 'CENTCOS'),
                           ('dbo', 'MVTOAUXNIF');   -- <-- agregada si aplica

DECLARE @db SYSNAME, @schema SYSNAME, @table SYSNAME;
DECLARE @sql NVARCHAR(MAX);

-- Tabla temporal para resultados
IF OBJECT_ID('tempdb..#CT_Versiones') IS NOT NULL DROP TABLE #CT_Versiones;
CREATE TABLE #CT_Versiones (
    SourceDB SYSNAME,
    TableName SYSNAME,
    LocalVersion BIGINT,
    ConsolidadaVersion BIGINT
);

DECLARE cur CURSOR FOR
    SELECT d.DbName, t.SchemaName, t.TableName
    FROM @dbs d
    CROSS JOIN @tables t;

OPEN cur;
FETCH NEXT FROM cur INTO @db, @schema, @table;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = '
    USE ' + QUOTENAME(@db) + ';
    DECLARE @cur BIGINT;
    SELECT @cur = CHANGE_TRACKING_CURRENT_VERSION();

    USE CONSOLIDADA;
    INSERT INTO #CT_Versiones (SourceDB, TableName, LocalVersion, ConsolidadaVersion)
    SELECT
        ''' + @db + ''',
        ''' + @schema + '.' + @table + ''',
        @cur,
        ISNULL((
            SELECT LastVersion
            FROM dbo.ConsolidationEngineWatermark
            WHERE SourceServer = @@SERVERNAME
              AND SourceDB = ''' + @db + '''
              AND TableName = ''' + @schema + '.' + @table + '''
        ), NULL);';

    EXEC sp_executesql @sql;

    FETCH NEXT FROM cur INTO @db, @schema, @table;
END

CLOSE cur;
DEALLOCATE cur;

-- Mostrar resultado final
SELECT * FROM #CT_Versiones;
