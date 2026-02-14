using Microsoft.EntityFrameworkCore;

/// <inheritdoc cref="IIngestionService" />

public class IngestionService : IIngestionService
{
    private static readonly SemaphoreSlim IngestionLock = new(1, 1);
    private readonly AppDbContext _dbContext;
    private readonly IDownloadService _DownloadService;
    private readonly ILogger<IngestionService> _logger;

    /// <summary>
    /// Constructor for the IngestionService.
    /// </summary>
    /// <param name="dbContext">The database context for accessing the database.</param>
    /// <param name="DownloadService">The download service for fetching the latest observations.</param>
    /// <param name="logger">The logger for logging information and errors.</param>
    public IngestionService(AppDbContext dbContext, IDownloadService DownloadService, ILogger<IngestionService> logger)
    {
        _dbContext = dbContext;
        _DownloadService = DownloadService;
        _logger = logger;
    }

    public async Task WaitForDatabaseReadyAsync(CancellationToken cancellationToken)
    {
        var maxWaitTime = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        // Wait for the database to be ready and the table has beem created
        while (true)
        {
            try
            {
                // Check if the "Observations" table exists 
                await _dbContext.Observations.OrderBy(o => o.ObservationTime).FirstOrDefaultAsync(cancellationToken);
                break; 
            }
            catch (Exception)
            {
                // If the database is not ready yet, it will throw an exception. Just ignore it and keep waiting.
            }

            await Task.Delay(1000, cancellationToken); // Wait for 1 second before checking again
            if (DateTime.UtcNow - startTime > maxWaitTime)
            {
                _logger.LogError("Timed out waiting for the database to be ready after {Seconds} seconds.", maxWaitTime.TotalSeconds);
                throw new TimeoutException("Timed out waiting for the database to be ready.");
            }

            // Exit the loop if cancellation is requested
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Waiting for database readiness was cancelled.");
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }

    public async Task IngestObservationAsync(Observation observation)
    {
        // Verify that the observation is not null
        if (observation == null)
        {
            // We throw an exception if the observation is null, as we cannot ingest a null observation into the database.
            // The download service should never return null observation.
            throw new ArgumentNullException(nameof(observation), "Observation cannot be null.");
        }

        _dbContext.Observations.Add(observation);
        await _dbContext.SaveChangesAsync();
    }

    public async Task IngestLatestObservationsAsync()
    {
        // Use a semaphore to ensure that only one ingestion process can run at a time, preventing race conditions
        await IngestionLock.WaitAsync();
        try
        {
            // Extract the latest observations from the external source
            var observations = await _DownloadService.FetchLatestObservationsAsync();
            if (observations == null)
            {
                // We throw an exception if the download service returns null. The download service should never return null. 
                // If the download service fails to fetch the latest observations, it returns an empty list.
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

            _logger.LogInformation("{Count} new observations will be added to the database", newObservations.Count);
            _dbContext.Observations.AddRange(newObservations);
            await _dbContext.SaveChangesAsync();

            // Log the number of new observations added
            var newCount = _dbContext.Observations.Count() - existingCount;
            _logger.LogInformation("Added {Count} new observations to the database", newCount);
        }
        finally
        {
            IngestionLock.Release();
        }
    }
}
