USE PASTO;

INSERT INTO dbo.mvtoauxnif (
    BASE,
    CHEQUE,
    CODCC,
    CODCOMPROB,
    CODIGOCTA,
    CODMONEDA,
    CODTRIBUTA,
    CREDITO,
    CRMONEXT,
    CRMULTIM,
    DBMONEXT,
    DBMULTIM,
    DCTO,
    DEBITO,
    DESCRIPCIO,
    DETALLE,
    FECHAMVTO,
    FECHAREAL,
    FECING,
    FECMOD,
    NIIF,
    NIT,
    NOMINCONSI,
    NOTA,
    PASSWORDIN,
    PASSWORDMO,
    REGISTRO,
    STADSINCRO,
    SUCURSAL,
    IDINTEGRA,
    CODIGOUEN
)
VALUES (
    0.00,                       -- BASE
    '00012345',                 -- CHEQUE
    '0732001',                  -- CODCC
    '99',                       -- CODCOMPROB
    '11051002',                 -- CODIGOCTA
    'COP',                      -- CODMONEDA
    'IVA',                      -- CODTRIBUTA
    0.00,                       -- CREDITO
    0.00,                       -- CRMONEXT
    0.00,                       -- CRMULTIM
    0.00,                       -- DBMONEXT
    0.00,                       -- DBMULTIM
    'DOC12345',                 -- DCTO
    500000.00,                  -- DEBITO
    'ASIENTO DE PRUEBA',        -- DESCRIPCIO
    'REGISTRO DE PRUEBA PARA REPLICACIÓN', -- DETALLE
    '2025-09-01 00:00:00',      -- FECHAMVTO
    '2025-09-01 00:00:00',      -- FECHAREAL
    GETDATE(),                  -- FECING
    GETDATE(),                  -- FECMOD
    1,                          -- NIIF (bit)
    '900141348-7',               -- NIT
    'CONSOLIDADO NIF',          -- NOMINCONSI
    'Prueba inicial',           -- NOTA
    'usr_test',                 -- PASSWORDIN
    'mod_test',                 -- PASSWORDMO
    1,                          -- REGISTRO
    0,                          -- STADSINCRO
    101,                        -- SUCURSAL
    NEWID(),                    -- IDINTEGRA (GUID único)
    '0'                       -- CODIGOUEN
);


USE PASTO;
SELECT * FROM MVTOAUXNIF;

USE CONSOLIDADA;
SELECT * FROM MVTOAUXNIF;
