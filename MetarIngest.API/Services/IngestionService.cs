/*********************************************************************************
* METAR Ingestion Service
* This class is responsible for ingesting METAR observations into the database. 
* It provides a method to add a new observation to the database context and save the changes.
* It can be executed as a background service to automatically ingest METAR data at regular intervals or can be called from a controller to ingest data on demand.
*********************************************************************************/

public class IngestionService : IIngestionService       
{
    private readonly AppDbContext _dbContext;
    private readonly IDownloadService _DownloadService;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(AppDbContext dbContext, IDownloadService DownloadService, ILogger<IngestionService> logger)
    {
        _dbContext = dbContext;
        _DownloadService = DownloadService;
        _logger = logger;
    }

    // Method to ingest a new observation into the database
    public async Task IngestObservationAsync(Observation observation)
    {
        // Verify that the observation is not null
        if (observation == null)
        {
            // We throw an exception if the observation is null, as we cannot ingest a null observation into the database.
            // The download service should never return null observation.
            throw new ArgumentNullException(nameof(observation), "Observation cannot be null.");
        }

        // Add the new observation to the database context
        _dbContext.Observations.Add(observation);

        // Save changes to the database
        await _dbContext.SaveChangesAsync();
    }

    // Download and ingest the latest observations from the external source
    public async Task IngestLatestObservationsAsync()
    {
        // Extract the latest observations from the external source
        var observations = await _DownloadService.FetchLatestObservationsAsync();
        if (observations == null)
        {
            // We throw an exception if the download service returns null. The download service should never return null, 
            // so this indicates a programming error or an unexpected condition. If the download service fails to fetch the latest observations, 
            // it returns an empty list.
            throw new InvalidOperationException("Failed to fetch latest observations.");
        }

        _logger.LogInformation("Fetched {Count} observations from the download service", observations.Count);

        // Store the number of observations before adding new ones, for logging purposes
        var existingCount = _dbContext.Observations.Count();
        _logger.LogInformation("Existing observations in the database: {Count}", existingCount);

        // First, deduplicate observations within the batch itself by grouping by the composite key (StationId, ObservationTime)
        var uniqueObservations = observations
            .GroupBy(o => new { o.StationId, o.ObservationTime })
            .Select(g => g.First())
            .ToList();

        // Filter out observations that already exist in the database to avoid duplicates. We check for existing entries with the same station ID and observation time.
        var newObservations = uniqueObservations.Where(o => 
        !_dbContext.Observations.Any(existing => existing.StationId == o.StationId && existing.ObservationTime == o.ObservationTime)).ToList();

        // Log the number of new observations that will be added for debugging purposes
        _logger.LogInformation("{Count} new observations will be added to the database", newObservations.Count);

        // Add the new observations to the database context. If an entry exists with the same station ID and observation time, skip it.
        _dbContext.Observations.AddRange(newObservations);

        // Save changes to the database
        await _dbContext.SaveChangesAsync();

        // Log the number of new observations added for debugging purposes
        var newCount = _dbContext.Observations.Count() - existingCount;
        _logger.LogInformation("Added {Count} new observations to the database", newCount);
    }
}
