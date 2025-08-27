-- Crear base distribution en el mismo servidor
USE master;
GO
EXEC sp_adddistributor @distributor = @@SERVERNAME, @password = N'distrib_pass123';

-- Crear la base distribution
EXEC sp_adddistributiondb 
    @database = N'distribution',
    @data_folder = N'C:\SQLData\', 
    @log_folder = N'C:\SQLData\', 
    @log_file_size = 2, 
    @min_distretention = 0, 
    @max_distretention = 72, 
    @history_retention = 48;

-- Asociar el distribuidor con el mismo servidor
EXEC sp_adddistpublisher 
    @publisher = @@SERVERNAME, 
    @distribution_db = N'distribution', 
    @security_mode = 1;
