public interface IIngestionService
{
    // Method to ingest a single observation into the database
    Task IngestObservationAsync(Observation observation);

    // Method to ingest a list of observations into the database. It fetches the latest observations from the DownloadService and adds them to the database.
    Task IngestLatestObservationsAsync();
}