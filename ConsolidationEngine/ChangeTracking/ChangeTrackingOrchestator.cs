using ConsolidationEngine.Config;

namespace ConsolidationEngine.ChangeTracking
{
    public class ChangeTrackingOrchestator
    {
        private readonly ConsolidationSettings _settings;
        private readonly ILogger _logger;

        public ChangeTrackingOrchestator(ConsolidationSettings settings, ILogger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public void RunAll()
        {
            foreach (var dbPair in _settings.Databases)
            {
                _logger.LogInformation("[ORCHESTATOR] Checkeando {origen} -> {destino}", dbPair.Origin, dbPair.Target);
                foreach (var table in _settings.Tables)
                {
                    try
                    {
                        ChangeTrackingETL etl = new ChangeTrackingETL(
                            server: _settings.Server,
                            originDb: dbPair.Origin,
                            targetDb: dbPair.Target,
                            user: _settings.User,
                            password: _settings.Password,
                            table: table.Name,
                            keyCol: table.KeyColumn,
                            batchSize: _settings.BatchSize,
                            logger: _logger
                        );

                        etl.Run();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "[ORCHESTATOR] {Origin}.{Table} -> {Target} falló",
                            dbPair.Origin, table.Name, dbPair.Target
                        );
                    }
                }
            }
        }
    }
}
