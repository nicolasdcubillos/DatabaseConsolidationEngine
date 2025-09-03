using ConsolidationEngine.Orchestrator;
using System.Text;

namespace ConsolidationEngine;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _config;
    private readonly ConsolidationOrchestrator consolidationOrchestator;
    private readonly int _heartbeat;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;

        _heartbeat = int.TryParse(_config["HeartbeatSeconds"], out var value)
            ? value
            : 30;

        consolidationOrchestator = new ConsolidationOrchestrator(_config, _logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                consolidationOrchestator.RunAll();
            }
            await Task.Delay(TimeSpan.FromSeconds(_heartbeat), stoppingToken);
        }
    }
}
