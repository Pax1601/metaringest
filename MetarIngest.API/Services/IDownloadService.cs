public interface IDownloadService
{
    // Method to download METAR data for a given station ID
    Task<Stream> DownloadMetarsGZipAsync();

    // Method to unzip the GZipped file 
    Stream UnzipGzippedStream(Stream gzippedStream);

    // Method to parse the CSV stream and extract observations
    List<Observation> ParseCsvStream(Stream csvStream);

    // Method to extract all the latest observations from the URL. It downloads, unzips, parses the CSV data, and returns a list of Observation objects.
    Task<List<Observation>> FetchLatestObservationsAsync();
}