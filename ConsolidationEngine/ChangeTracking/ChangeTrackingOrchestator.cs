using ConsolidationEngine.Config;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ConsolidationEngine.ChangeTracking
{
    public class ChangeTrackingOrchestator
    {
        private readonly ConsolidationSettings _settings;
        private readonly ConcurrentDictionary<string, Task> _runningJobs = new();
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
                foreach (var table in _settings.Tables)
                {
                    var key = $"{dbPair.Origin}.{table.Name}";
                    if (_runningJobs.ContainsKey(key))
                    {
                        _logger.LogWarning("[ORCHESTRATOR] Ya hay un job corriendo para {key}, se omite.", key);
                        continue;
                    }

                    var task = Task.Run(() =>
                    {
                        try
                        {
                            //_logger.LogInformation("[ORCHESTRATOR] Iniciando job {key}", key);

                            ChangeTrackingETL etl = new ChangeTrackingETL(
                                server: _settings.Server,
                                originDb: dbPair.Origin,
                                targetDb: dbPair.Target,
                                user: _settings.User,
                                password: _settings.Password,
                                table: table.Name,
                                keyCol: table.KeyColumn,
                                skipPrimaryKey: table.SkipPrimaryKey,
                                batchSize: _settings.BatchSize,
                                upsertBatchWithFallbackTimeoutSeconds: _settings.UpsertBatchWithFallbackTimeoutSeconds,
                                logger: _logger
                            );

                            etl.Run();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[ORCHESTRATOR] Job {key} falló.", key);
                        }
                        finally
                        {
                            _runningJobs.TryRemove(key, out _);
                            //_logger.LogInformation("[ORCHESTRATOR] Job {key} finalizado.", key);
                        }
                    });

                    _runningJobs[key] = task;
                }
            }
        }
    }
}
