/*********************************************************************************
* METAR Periodic Update Service
* This class is responsible for periodically updating the database with the latest METAR observations.
* The service is a background service.
*********************************************************************************/

using Microsoft.Extensions.Options;

public class PeriodicUpdateService: BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PeriodicUpdateService> _logger;
    private readonly TimeSpan _updateInterval;

    public PeriodicUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<PeriodicUpdateService> logger, IOptions<MetarSettings> settings)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _updateInterval = settings.Value.UpdateInterval;

        _logger.LogInformation("PeriodicUpdateService initialized with update interval of {Minutes} minutes", _updateInterval.TotalMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ingest the latest observations into the database
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
                    await ingestionService.IngestLatestObservationsAsync();
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the update process
                _logger.LogError(ex, "Error during periodic update");
            }

            // Wait for the specified update interval before the next update
            await Task.Delay(_updateInterval, stoppingToken);
        }
    }
}

