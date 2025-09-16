using ConsolidationEngine.ChangeTracking;
using ConsolidationEngine.Config;

namespace ConsolidationEngine.Orchestrator
{
    public class ConsolidationOrchestrator
    {
        private readonly ConsolidationSettings _settings;
        private readonly ILogger _logger;

        public ConsolidationOrchestrator(IConfiguration config, ILogger logger)
        {
            _settings = config.GetSection("ConsolidationEngine").Get<ConsolidationSettings>() ?? new ConsolidationSettings();
            _logger = logger;
        }

        public void RunAll()
        {
            foreach (var dbPair in _settings.Databases)
            {
                foreach (var table in _settings.Tables)
                {
                    try
                    {
                        _logger.LogInformation(
                            "Ejecutando ETL de {Origin}.{Table} -> {Target}",
                            dbPair.Origin, table.Name, dbPair.Target
                        );

                        ChangeTrackingETL etl = new ChangeTrackingETL(
                            server: _settings.Server,
                            originDb: dbPair.Origin,
                            targetDb: dbPair.Target,
                            originConnectionString: dbPair.OriginConnectionString,
                            targetConnectionString: dbPair.TargetConnectionString,
                            table: table.Name,
                            keyCol: table.KeyColumn,
                            batchSize: _settings.BatchSize,
                            logger: _logger
                        );

                        etl.Run();

                        _logger.LogInformation(
                            "ETL finalizado exitosamente para {Origin}.{Table} -> {Target}",
                            dbPair.Origin, table.Name, dbPair.Target
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "ETL {Origin}.{Table} -> {Target} falló",
                            dbPair.Origin, table.Name, dbPair.Target
                        );
                    }
                }
            }
        }
    }
}
