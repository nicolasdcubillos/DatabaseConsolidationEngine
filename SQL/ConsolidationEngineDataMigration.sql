------------------------------------------------------------
-- SCRIPT DE MIGRACIÓN A BASE CONSOLIDADA (Consolidadaniif)
-- Tablas: MTCIIU y NIT
-- Descripción: Copia datos desde múltiples bases de origen
-- evitando duplicados en la base consolidada.
------------------------------------------------------------

DECLARE @db NVARCHAR(100);
DECLARE @sql NVARCHAR(MAX);

------------------------------------------------------------
-- 1. LISTA DE BASES DE DATOS ORIGEN
------------------------------------------------------------
DECLARE @Origenes TABLE (DbName NVARCHAR(100));
INSERT INTO @Origenes (DbName)
VALUES 
('PASTO'),
('BOGOTA');

INSERT INTO @Origenes (DbName)
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
    ('VILLAVICENCIO');

------------------------------------------------------------
-- 2. MIGRACIÓN DE MTCDDAN
------------------------------------------------------------
PRINT '====================';
PRINT 'INICIANDO MIGRACIÓN DE MTCDDAN';
PRINT '====================';

DECLARE dbs1 CURSOR FOR SELECT DbName FROM @Origenes;
OPEN dbs1;
FETCH NEXT FROM dbs1 INTO @db;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Insertando datos de MTCDDAN desde ' + @db + '...';

    SET @sql = '
    INSERT INTO Consolidadaniif.dbo.MTCDDAN (
        CODIGO,
        NOMCIUD,
        NOMDPTO,
        STADSINCRO
    )
    SELECT 
        CODIGO,
        NOMCIUD,
        NOMDPTO,
        STADSINCRO
    FROM ' + QUOTENAME(@db) + '.dbo.MTCDDAN src
    WHERE NOT EXISTS (
        SELECT 1 FROM Consolidadaniif.dbo.MTCDDAN dest
        WHERE dest.CODIGO = src.CODIGO
    );';

    EXEC (@sql);

    PRINT 'Datos de MTCDDAN desde ' + @db + ' insertados correctamente.';
    FETCH NEXT FROM dbs1 INTO @db;
END

CLOSE dbs1;
DEALLOCATE dbs1;

PRINT '====================';
PRINT 'MIGRACIÓN DE MTCDDAN COMPLETADA';
PRINT '====================';

------------------------------------------------------------
-- 3. MIGRACIÓN DE MTCIIU
------------------------------------------------------------
PRINT '====================';
PRINT 'INICIANDO MIGRACIÓN DE MTCIIU';
PRINT '====================';

DECLARE dbs1 CURSOR FOR SELECT DbName FROM @Origenes;
OPEN dbs1;
FETCH NEXT FROM dbs1 INTO @db;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Insertando datos de MTCIIU desde ' + @db + '...';

    SET @sql = '
    INSERT INTO Consolidadaniif.dbo.MTCIIU (
        CODIGO,
        NOMBRE,
        STADSINCRO,
        CUENTACREE,
        PORCENTAJE,
        CTACREENIF
    )
    SELECT 
        CODIGO,
        NOMBRE,
        STADSINCRO,
        CUENTACREE,
        PORCENTAJE,
        CTACREENIF
    FROM ' + QUOTENAME(@db) + '.dbo.MTCIIU src
    WHERE NOT EXISTS (
        SELECT 1 FROM Consolidadaniif.dbo.MTCIIU dest
        WHERE dest.CODIGO = src.CODIGO
    );';

    EXEC (@sql);

    PRINT 'Datos de MTCIIU desde ' + @db + ' insertados correctamente.';
    FETCH NEXT FROM dbs1 INTO @db;
END

CLOSE dbs1;
DEALLOCATE dbs1;

PRINT '====================';
PRINT 'MIGRACIÓN DE MTCIIU COMPLETADA';
PRINT '====================';


------------------------------------------------------------
-- 4. MIGRACIÓN DE NIT
------------------------------------------------------------
PRINT '====================';
PRINT 'INICIANDO MIGRACIÓN DE NIT';
PRINT '====================';

DECLARE dbs2 CURSOR FOR SELECT DbName FROM @Origenes;
OPEN dbs2;
FETCH NEXT FROM dbs2 INTO @db;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Insertando datos de NIT desde ' + @db + '...';

    SET @sql = '
    INSERT INTO Consolidadaniif.dbo.NIT (
        APELLIDO1, APELLIDO2, CDCIIU, CIIU, CLASE, COMENTARIO, CURP, DIRECCION, 
        EXCLUYE, EXTERIOR, NITEXT, NOMBRE, NOMBRE1, NOMBRE2, NRONIT, PAIS, 
        PERSONANJ, PORCENTA, REGIMENPT, REGSIMP, SOCIOACCIO, STADSINCRO, 
        TIPODCTO, VPARTICIPA, VPATRIMONI, IDADJUNTOS, TIDENTI, CELULAR, EMAIL, 
        NACIONALID, NOMBREXT, TELEFONO, TIPOPER, TIPTERC, CIUDADMX, CODPOSTAL, 
        COLONIA, DELEGACION, ENTRECALLES, ESTADOMX, LOCALIDAD, NROEXTERIOR, 
        NROINTERIOR, PAGWEB, REGTRSIMP, VALPRIMA, SourceKey, NuevaColumna
    )
    SELECT 
        APELLIDO1, APELLIDO2, CDCIIU, CIIU, CLASE, COMENTARIO, CURP, DIRECCION, 
        EXCLUYE, EXTERIOR, NITEXT, NOMBRE, NOMBRE1, NOMBRE2, NRONIT, PAIS, 
        PERSONANJ, PORCENTA, REGIMENPT, REGSIMP, SOCIOACCIO, STADSINCRO, 
        TIPODCTO, VPARTICIPA, VPATRIMONI, IDADJUNTOS, TIDENTI, CELULAR, EMAIL, 
        NACIONALID, NOMBREXT, TELEFONO, TIPOPER, TIPTERC, CIUDADMX, CODPOSTAL, 
        COLONIA, DELEGACION, ENTRECALLES, ESTADOMX, LOCALIDAD, NROEXTERIOR, 
        NROINTERIOR, PAGWEB, REGTRSIMP, VALPRIMA, SourceKey, NuevaColumna
    FROM ' + QUOTENAME(@db) + '.dbo.NIT src
    WHERE NOT EXISTS (
        SELECT 1 FROM Consolidadaniif.dbo.NIT dest
        WHERE dest.NRONIT = src.NRONIT
    );';

    EXEC (@sql);

    PRINT 'Datos de NIT desde ' + @db + ' insertados correctamente.';
    FETCH NEXT FROM dbs2 INTO @db;
END

CLOSE dbs2;
DEALLOCATE dbs2;

------------------------------------------------------------
-- 5. MIGRACIÓN DE CUENTASNIF
------------------------------------------------------------
PRINT '====================';
PRINT 'INICIANDO MIGRACIÓN DE CUENTASNIF';
PRINT '====================';

DECLARE dbs3 CURSOR FOR SELECT DbName FROM @Origenes;
OPEN dbs3;
FETCH NEXT FROM dbs3 INTO @db;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Insertando datos de CUENTASNIF desde ' + @db + '...';

    SET @sql = '
    INSERT INTO Consolidadaniif.dbo.CUENTASNIF (
        AJUSTACM, AJUSTAXINF, CABRIL, CAGOSTO, CDICIEMBRE, CENERO, CFEBRERO, CJULIO, CJUNIO, 
        CMARZO, CMAYO, CMES13, CNOVIEMBRE, COCTUBRE, CODIAUX, CODIGOCTA, CODIMPUEST, CORRIENTE, 
        CSEPTIEMBR, CTAMATRIZ, CUENTADEST, DABRIL, DAGOSTO, DDICIEMBRE, DENERO, DESCRIPCIO, 
        DFEBRERO, DJULIO, DJUNIO, DMARZO, DMAYO, DMES13, DNOVIEMBRE, DOBLEMONED, DOCTUBRE, 
        DSEPTIEMBR, GRANCONTRB, HABILITADA, MONETARIA, NITBASE, NODEDUCE, PABRIL, PAGOSTO, 
        PCASAMAT, PDICIEMBRE, PENERO, PFEBRERO, PJULIO, PJUNIO, PMARZO, PMAYO, PNOVIEMBRE, 
        POCTUBRE, PRESUPUEST, PSEPTIEMBR, REQCODCC, REQMULTIM, SALDOANTCR, SALDOANTDB, 
        STADSINCRO, TIPOCUENTA, TIPOFLUJO, TIPOINGRES, DESCTRIBU
    )
    SELECT 
        AJUSTACM, AJUSTAXINF, CABRIL, CAGOSTO, CDICIEMBRE, CENERO, CFEBRERO, CJULIO, CJUNIO, 
        CMARZO, CMAYO, CMES13, CNOVIEMBRE, COCTUBRE, CODIAUX, CODIGOCTA, CODIMPUEST, CORRIENTE, 
        CSEPTIEMBR, CTAMATRIZ, CUENTADEST, DABRIL, DAGOSTO, DDICIEMBRE, DENERO, DESCRIPCIO, 
        DFEBRERO, DJULIO, DJUNIO, DMARZO, DMAYO, DMES13, DNOVIEMBRE, DOBLEMONED, DOCTUBRE, 
        DSEPTIEMBR, GRANCONTRB, HABILITADA, MONETARIA, NITBASE, NODEDUCE, PABRIL, PAGOSTO, 
        PCASAMAT, PDICIEMBRE, PENERO, PFEBRERO, PJULIO, PJUNIO, PMARZO, PMAYO, PNOVIEMBRE, 
        POCTUBRE, PRESUPUEST, PSEPTIEMBR, REQCODCC, REQMULTIM, SALDOANTCR, SALDOANTDB, 
        STADSINCRO, TIPOCUENTA, TIPOFLUJO, TIPOINGRES, DESCTRIBU
    FROM ' + QUOTENAME(@db) + '.dbo.CUENTASNIF src
    WHERE NOT EXISTS (
        SELECT 1 FROM Consolidadaniif.dbo.CUENTASNIF dest
        WHERE dest.CODIGOCTA = src.CODIGOCTA
    );';

    EXEC (@sql);

    PRINT 'Datos de CUENTASNIF desde ' + @db + ' insertados correctamente.';
    FETCH NEXT FROM dbs3 INTO @db;
END

CLOSE dbs3;
DEALLOCATE dbs3;

PRINT '====================';
PRINT 'MIGRACIÓN DE CUENTASNIF COMPLETADA';
PRINT '====================';
