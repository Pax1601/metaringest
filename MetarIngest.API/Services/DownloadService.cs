/*********************************************************************************
* METAR Download Service
* This class is responsible for downloading METAR observations from the external source. 
* The observations are in a GZipped CSV format.
*********************************************************************************/

using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloadService> _logger;
    private readonly string _url;

    public DownloadService(HttpClient httpClient, ILogger<DownloadService> logger, IOptions<MetarSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _url = settings.Value.Url;

    }

    // Method to download METAR data for a given station ID
    public async Task<Stream> DownloadMetarsGZipAsync()
    {
        // Download the GZipped file
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(_url);
            response.EnsureSuccessStatusCode(); // Ensure the request was successful
        }
        catch (HttpRequestException ex)
        {
            // Handle HTTP request errors (e.g., network issues, non-success status codes)
            _logger.LogError(ex, "Error downloading METAR data");
            return Stream.Null; // Return an empty stream if the download fails
        }

        // Read the response content as a stream
        Stream stream;
        try
        {
            stream = await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            // Handle errors while reading the response stream
            _logger.LogError(ex, "Error reading METAR data stream");
            return Stream.Null; // Return an empty stream if reading fails
        }
        return stream;
    }

    // Method to unzip the GZipped file 
    public Stream UnzipGzippedStream(Stream gzippedStream)
    {
        // Check that the input stream is not null
        if (gzippedStream == null)
        {
            _logger.LogWarning("GZipped stream is null");
            return Stream.Null; // Return an empty stream if the input stream is null
        }

        // Create a GZipStream to decompress the data
        Stream unzippedStream;
        try
        {
            unzippedStream = new System.IO.Compression.GZipStream(gzippedStream, System.IO.Compression.CompressionMode.Decompress);
        }
        catch (Exception ex)
        {
            // Handle errors during decompression
            _logger.LogError(ex, "Error decompressing METAR data");
            return Stream.Null; // Return an empty stream if decompression fails
        }

        return unzippedStream;
    }

    // Method to parse the CSV data and extract observations
    public List<Observation> ParseCsvStream(Stream csvStream)
    {
        // Check that the input stream is not null
        if (csvStream == null)
        {
            _logger.LogWarning("CSV stream is null");
            return new List<Observation>(); // Return an empty list if the input stream is null
        }

        var observations = new List<Observation>();
        try
        {
            // Create a StreamReader to read the CSV data
            using var reader = new StreamReader(csvStream);
            // Configure CsvHelper to read the CSV data
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true, // The CSV file has a header record
                MissingFieldFound = null, // Ignore missing fields
                BadDataFound = null // Ignore bad data
            };
            using var csv = new CsvReader(reader, csvConfig);

            // Read the header 
            csv.Read();
            csv.ReadHeader();

            // Read the CSV records and map them to Observation objects
            while (csv.Read())
            {
                // Verify that the required fields are present before attempting to parse
                if (!csv.TryGetField<string>("station_id", out var stationId) ||
                    !csv.TryGetField<DateTime>("observation_time", out var observationTime) ||
                    !csv.TryGetField<double>("temp_c", out var temperature) ||
                    !csv.TryGetField<string>("raw_text", out var rawMetar))
                {
                    _logger.LogDebug("Missing required fields in METAR record. Skipping record");
                    continue; // Skip this record if any required field is missing
                }

                var observation = CreateObservation(stationId, observationTime, temperature, rawMetar);
                if (observation != null)
                {
                    observations.Add(observation);
                }
            }
        }
        catch (Exception ex)
        {
            // Handle errors during CSV parsing
            _logger.LogError(ex, "Error parsing METAR CSV data");
            return new List<Observation>(); // Return an empty list if parsing fails
        }
        return observations;
    }

    // Method to create an Observation object from the CSV fields
    public Observation? CreateObservation(string? stationId, DateTime observationTime, double temperature, string? rawMetar)
    {
        // Validate the input parameters before creating the Observation object
        if (string.IsNullOrWhiteSpace(stationId))
        {
            _logger.LogWarning("Station ID is null or empty. Cannot create Observation");
            return null; // Return null if the station ID is invalid
        }
        if (string.IsNullOrWhiteSpace(rawMetar))
        {
            _logger.LogWarning("Raw METAR is null or empty. Cannot create Observation");
            return null; // Return null if the raw METAR is invalid
        }

        // Create and return a new Observation object
        Observation observation;
        try
        {
            observation = new Observation(stationId, observationTime, temperature, rawMetar);
        }
        catch (Exception ex)
        {
            // Handle errors during Observation object creation
            _logger.LogError(ex, "Error creating Observation object");
            return null; // Return null if there was an error creating the Observation
        }
        return observation;
    }

    // Method to extract all the latest observations from the URL. It downloads, unzips, parses the CSV data, and returns a list of Observation objects.
    public async Task<List<Observation>> FetchLatestObservationsAsync()
    {
        // Download the GZipped METAR data
        var gzippedStream = await DownloadMetarsGZipAsync();

        // Unzip the GZipped stream to get the CSV data
        var csvStream = UnzipGzippedStream(gzippedStream);

        // Parse the CSV stream to extract observations
        var observations = ParseCsvStream(csvStream);

        // Log the number of observations extracted for debugging purposes
        _logger.LogInformation("Extracted {Count} observations from the METAR data", observations.Count);

        return observations;
    }
}