using ConsolidationEngine;
using System.Runtime.InteropServices;

IHostBuilder builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.Sources.Clear();
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddEventLog();
    });

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.UseWindowsService();
}

builder.Build().Run();
