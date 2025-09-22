-- Configuración
DECLARE @LoginName SYSNAME = N'ConsolidationEngineUser4';
DECLARE @Password NVARCHAR(128) = N'consolidationengine123';
DECLARE @Databases TABLE (DbName SYSNAME);

-- Lista de bases donde quieres el usuario
INSERT INTO @Databases VALUES ('PASTO'), ('BOGOTA'), ('CONSOLIDADA');

-- 1. Crear el LOGIN a nivel de servidor (si no existe)
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName)
BEGIN
    DECLARE @sqlLogin NVARCHAR(MAX) =
        N'CREATE LOGIN [' + @LoginName + N'] WITH PASSWORD = ''' + @Password + N''', CHECK_POLICY = OFF;';
    PRINT @sqlLogin;
    EXEC(@sqlLogin);
END

-- 2. Crear USER en cada base y asignar permisos
DECLARE @DbName SYSNAME, @sql NVARCHAR(MAX);

DECLARE cur CURSOR FOR
    SELECT DbName FROM @Databases;

OPEN cur;
FETCH NEXT FROM cur INTO @DbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = '
    USE [' + @DbName + '];

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = ''' + @LoginName + ''')
    BEGIN
        CREATE USER [' + @LoginName + '] FOR LOGIN [' + @LoginName + '];
        EXEC sp_addrolemember ''db_datareader'', ''' + @LoginName + ''';
        EXEC sp_addrolemember ''db_datawriter'', ''' + @LoginName + ''';
    END

    -- Asegurar permisos SELECT sobre todas las tablas del esquema dbo
    GRANT SELECT ON SCHEMA::dbo TO [' + @LoginName + '];
    ';
    PRINT @sql;
    EXEC(@sql);

    FETCH NEXT FROM cur INTO @DbName;
END

CLOSE cur;
DEALLOCATE cur;
