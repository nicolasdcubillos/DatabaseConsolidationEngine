USE CONSOLIDADA;
GO

EXEC sp_addsubscription 
    @publication = N'PASTO_MVTOAUXNIF_Pub',
    @subscriber = @@SERVERNAME,
    @destination_db = N'CONSOLIDADA',
    @subscription_type = N'Push',
    @sync_type = N'automatic',
    @article = N'all',
    @update_mode = N'read only',
    @subscriber_type = 0;
GO