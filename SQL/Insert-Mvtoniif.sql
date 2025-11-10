USE PASTO;
GO

DECLARE @lote INT = 1;
DECLARE @totalLotes INT = 20; 
DECLARE @cantidadLote INT = 15; 
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
            'NICO' + RIGHT('000000' + CAST(((@lote-1)*500 + @i) AS VARCHAR(6)), 6), -- CHEQUE único
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
            'FAC-2025-' + CAST(((@lote-1)*500 + @i) AS VARCHAR(6)), -- DCTO único
            1200000.00,                                 -- DEBITO
            'Compra de materia prima',                 -- DESCRIPCIO
            'Detalle del movimiento ' + CAST(((@lote-1)*500 + @i) AS VARCHAR(6)), -- DETALLE único
            GETDATE(),                                 -- FECHAMVTO
            GETDATE(),                                 -- FECHAREAL
            GETDATE(),                                 -- FECING
            GETDATE(),                                 -- FECMOD
            0,                                         -- NIIF
            '900141112-4',                              -- NIT - NO EXISTE EN CONSOLIDADA
            '',                                        -- NITCONTRAT
            'Sin observación',                         -- NOTA
            'usuario_in',                              -- PASSWORDIN
            '',                                        -- PASSWORDMO
            ((@lote-1)*500 + @i),                      -- REGISTRO único
            0,                                         -- STADSINCRO
            1,                                         -- SUCURSAL
            NEWID(),                                   -- IDINTEGRA
            '0'                                    -- CODIGOUEN (asegúrate que existe en MTUEN)
        );

        SET @i += 1;
    END;

    PRINT 'Lote ' + CAST(@lote AS VARCHAR) + ' insertado.';

    -- Pausa de 1 segundo entre lotes
    WAITFOR DELAY '00:00:01';

    SET @lote += 1;
END;
GO