USE PASTO;
GO

-- Crear publicación transaccional
EXEC sp_addpublication 
    @publication = N'PASTO_MVTOAUXNIF_Pub',
    @status = N'active',
    @allow_push = N'true',
    @allow_pull = N'true',
    @independent_agent = N'true',
    @immediate_sync = N'false',
    @retention = 0,
    @repl_freq = N'continuous',
    @sync_method = N'concurrent_c',
    @description = N'Transactional replication of MVTOAUXNIF from PASTO to CONSOLIDADA',
    @enabled_for_internet = N'false',
    @replicate_ddl = 1;
GO

-- Agregar la tabla como artículo
EXEC sp_addarticle 
    @publication = N'PASTO_MVTOAUXNIF_Pub',
    @article = N'MVTOAUXNIF',
    @source_owner = N'dbo',
    @source_object = N'MVTOAUXNIF',
    @type = N'logbased',
    @schema_option = 0x000000000803509F,
    @identityrangemanagementoption = N'manual';
GO