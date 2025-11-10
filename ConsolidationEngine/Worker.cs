using ConsolidationEngine.ChangeTracking;
using ConsolidationEngine.Config;
using ConsolidationEngine.FaultRetry;
using ConsolidationEngine.Logger.Exceptions;
using ConsolidationEngine.Repository;
using System.Text;

namespace ConsolidationEngine;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly ChangeTrackingOrchestator consolidationOrchestator;
    private readonly FaultRetryProcessor retryProcessor;
    private readonly SqlSchemaValidator schemaValidator;
    private readonly ConsolidationSettings settings;
    private readonly int _heartbeat;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        _heartbeat = int.TryParse(_config["HeartbeatSeconds"], out var value)
            ? value
            : 30;

        settings = config.GetSection("ConsolidationEngine").Get<ConsolidationSettings>() ?? new ConsolidationSettings();

        consolidationOrchestator = new ChangeTrackingOrchestator(settings, _logger);
        schemaValidator = new SqlSchemaValidator(settings, _logger);
        retryProcessor = new FaultRetryProcessor(settings, _logger);

        SqlConnectionBuilder.Instance.Configure(settings.Server, settings.User, settings.Password);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bool schemaValidated = false;

        try
        {
            schemaValidator.Validate();
            schemaValidated = true;
        }
        catch (DatabaseConnectionValidatorError ex)
        {
            _logger.LogError(ex, "Error de conexión a la base de datos {Db}", ex.DatabaseName);
        }

        if (schemaValidated)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("ConsolidationEngine heartbeat at {time}", DateTimeOffset.Now);
                    consolidationOrchestator.RunAll();
                    if (settings.FaultRetryProcessorEnabled)
                    {
                        await retryProcessor.RunForAllTargetsAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inesperado en el ciclo de consolidación");
                }

                await Task.Delay(TimeSpan.FromSeconds(_heartbeat), stoppingToken);
            }
        }
        
    }
}
