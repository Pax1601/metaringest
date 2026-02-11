/*********************************************************************************
* METAR Periodic Update Service
* This class is responsible for periodically updating the database with the latest METAR observations.
* The service is a background service.
*********************************************************************************/

public class PeriodicUpdateService: BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PeriodicUpdateService> _logger;
    private TimeSpan _updateInterval = TimeSpan.FromMinutes(10); // Update every 10 minutes by default

    public PeriodicUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<PeriodicUpdateService> logger, TimeSpan? updateInterval = null)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        if (updateInterval.HasValue)
        {
            _updateInterval = updateInterval.Value;
        }

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

