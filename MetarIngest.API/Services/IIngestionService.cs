/// <summary>
/// Interface for the ingestion service. This service is responsible for ingesting METAR observations into the database. 
/// It provides methods to ingest a single observation and to ingest the latest observations from the data source.
/// </summary>
public interface IIngestionService
{
    /// <summary>
    /// Waits for the database to be ready before proceeding with ingestion. This method will repeatedly check if the database can connect, and will wait until it is ready or until a timeout occurs.
    /// </summary> <param name="cancellationToken">A token that can be used to cancel the waiting operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task WaitForDatabaseReadyAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ingests a single METAR observation into the database. It adds the observation to the database context and saves changes to the database. 
    /// If the observation is null, it throws an ArgumentNullException, as we cannot ingest a null observation into the database. The download service should never return null observation.
    /// </summary>
    /// <param name="observation">The METAR observation to ingest.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the observation is null.</exception>
    Task IngestObservationAsync(Observation observation);

    /// <summary>
    /// Removes all observations from the database that are older than 24 hours. This method is called periodically to keep the database clean and prevent it from growing indefinitely.
    /// </summary> <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveOldObservationsAsync();

    /// <summary>
    /// Ingests the latest METAR observations into the database. It fetches the latest observations from the DownloadService and adds them to the database.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task IngestLatestObservationsAsync();
}