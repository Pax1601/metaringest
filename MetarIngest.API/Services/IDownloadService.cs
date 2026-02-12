/// <summary>
/// This service is responsible for downloading METAR observations from an external web source. 
/// The observations are expected to be in a GZipped CSV format and contain at least the following fields: station_id, observation_time, temperature_c, and raw_text.
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Downloads the GZipped METAR data from the specified URL. It returns a stream of the downloaded data, which can be further processed (e.g., unzipped and parsed). If the download fails, it returns an empty stream.
    /// </summary>
    /// <returns>A stream containing the GZipped METAR data. If the download fails, it returns an empty stream.</returns>
    Task<Stream> DownloadMetarsGZipAsync();

    /// <summary>
    /// Unzips the provided GZipped stream and returns a stream of the uncompressed data. If the input stream is null or if decompression fails, it returns an empty stream.
    /// </summary>
    /// <param name="gzippedStream">The GZipped stream to be decompressed.</param>
    /// <returns>A stream containing the uncompressed data. If the input stream is null or if decompression fails, it returns an empty stream.</returns>
    Stream UnzipGzippedStream(Stream gzippedStream);

    /// <summary>
    /// Parses the provided CSV stream and extracts a list of Observation objects. If the input stream is null or if parsing fails, it returns an empty list.
    /// </summary>
    /// <param name="csvStream">The CSV stream to be parsed.</param>
    /// <returns>A list of Observation objects. If the input stream is null or if parsing fails, it returns an empty list.</returns>
    List<Observation> ParseCsvStream(Stream csvStream);

    /// <summary>
    /// Extracts all the latest observations from the URL. It downloads, unzips, parses the CSV data, and returns a list of Observation objects.
    /// </summary>
    /// <returns>A list of the latest Observation objects.</returns>
    Task<List<Observation>> FetchLatestObservationsAsync();
}