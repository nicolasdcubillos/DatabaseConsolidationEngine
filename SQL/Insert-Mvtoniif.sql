USE BOGOTA;
GO

DECLARE @lote INT = 1;
DECLARE @totalLotes INT = 5; 
DECLARE @cantidadLote INT = 20; 
DECLARE @i INT;

WHILE @lote <= @totalLotes
BEGIN
    SET @i = 1;

    WHILE @i <= @cantidadLote
    BEGIN
        INSERT INTO dbo.MVTONIIF
        (
            BASE, CHEQUE, CODCC, CODCOMPROB, CODIGOCTA, CODMONEDA,
            CODTRIBUTA, CREDITO, CRMONEXT, CRMULTIM,
            DBMONEXT, DBMULTIM, DCTO, DEBITO, DESCRIPCIO, DETALLE,
            FECHAMVTO, FECHAREAL, FECING, FECMOD,
            NIIF, NIT, NITCONTRAT, NOTA, PASSWORDIN, PASSWORDMO,
            REGISTRO, STADSINCRO, SUCURSAL, IDINTEGRA, CODIGOUEN
        )
        VALUES
        (
            500000.00,                                 -- BASE
            'NICO' + RIGHT('000000' + CAST(((@lote-1)*500 + @i) AS VARCHAR(6)), 6), -- CHEQUE �nico
            'CC001',                                   -- CODCC
            'CMP01',                                   -- CODCOMPROB
            '222001',                                -- CODIGOCTA (existe en CUENTASNIF)
            'COP',                                     -- CODMONEDA
            'IVA19',                                   -- CODTRIBUTA
            0.00,                                      -- CREDITO
            0.00,                                      -- CRMONEXT
            0.00,                                      -- CRMULTIM
            0.00,                                      -- DBMONEXT
            0.00,                                      -- DBMULTIM
            'FAC-2025-' + CAST(((@lote-1)*500 + @i) AS VARCHAR(6)), -- DCTO �nico
            1200000.00,                                 -- DEBITO
            'Compra de materia prima',                 -- DESCRIPCIO
            'Detalle del movimiento ' + CAST(((@lote-1)*500 + @i) AS VARCHAR(6)), -- DETALLE �nico
            GETDATE(),                                 -- FECHAMVTO
            GETDATE(),                                 -- FECHAREAL
            GETDATE(),                                 -- FECING
            GETDATE(),                                 -- FECMOD
            0,                                         -- NIIF
            '900141348-7',                              -- NIT (aseg�rate que existe en NIT)
            '',                                        -- NITCONTRAT
            'Sin observaci�n',                         -- NOTA
            'usuario_in',                              -- PASSWORDIN
            '',                                        -- PASSWORDMO
            ((@lote-1)*500 + @i),                      -- REGISTRO �nico
            0,                                         -- STADSINCRO
            1,                                         -- SUCURSAL
            NEWID(),                                   -- IDINTEGRA
            '0'                                    -- CODIGOUEN (aseg�rate que existe en MTUEN)
        );

        SET @i += 1;
    END;

    PRINT 'Lote ' + CAST(@lote AS VARCHAR) + ' insertado.';

    -- Pausa de 1 segundo entre lotes
    WAITFOR DELAY '00:00:01';

    SET @lote += 1;
END;
GO