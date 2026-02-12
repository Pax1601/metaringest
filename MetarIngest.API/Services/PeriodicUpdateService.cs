using Microsoft.Extensions.Options;

/// <summary>
/// This class is responsible for periodically updating the database with the latest METAR observations.
/// The service is a background service.
/// </summary>
public class PeriodicUpdateService: BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PeriodicUpdateService> _logger;
    private readonly TimeSpan _updateInterval;

    /// <summary>
    /// Constructor for the PeriodicUpdateService.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory for creating service scopes.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    /// <param name="settings">The settings for configuring the update interval.</param>
    public PeriodicUpdateService(IServiceScopeFactory serviceScopeFactory, ILogger<PeriodicUpdateService> logger, IOptions<MetarSettings> settings)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _updateInterval = settings.Value.UpdateInterval;

        _logger.LogInformation("PeriodicUpdateService initialized with update interval of {Minutes} minutes", _updateInterval.TotalMinutes);
    }

    /// <summary>
    /// This method is called when the service is started and runs a loop in the background.
    /// </summary>
    /// <param name="stoppingToken">A token that can be used to cancel the background task.</param>
    /// <returns>A task that represents the background operation.</returns>
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
                _logger.LogError(ex, "Error during periodic update");
            }

            // Wait for the specified update interval before the next update
            await Task.Delay(_updateInterval, stoppingToken);
        }
    }
}

