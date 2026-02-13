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
        // Wait for a short delay before starting the first update to allow the application to finish starting up and the database to be ready
        // We will also wait for the database to be ready before attempting to ingest observations, but this avoids unnecessary attempts and related exceptions, which are ugly :)
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO: Is there a better approach to validate the database is ready before starting the periodic updates? 
            // Using exceptions is not very elegant.
            try
            {
                // Ingest the latest observations into the database
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
                    // Wait for the database to be ready before attempting to ingest observations
                    await ingestionService.WaitForDatabaseReadyAsync(stoppingToken);
                    // Ingest the latest observations into the database
                    await ingestionService.IngestLatestObservationsAsync();
                }

                _logger.LogInformation("Successfully ingested latest observations. Next update in {Minutes} minutes.", _updateInterval.TotalMinutes);

                // Wait for the specified update interval before the next update
                await Task.Delay(_updateInterval, stoppingToken);
            }
            catch (TimeoutException)
            {
                // Exit the loop, if the database is not up after 30 seconds, we have a big problem! :)
                _logger.LogError("Timed out waiting for the database to be ready.");
                break; 
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Periodic update service is stopping due to cancellation request.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic update");
            }
        }
    }
}

