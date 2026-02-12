namespace MetarIngest.API.Tests;

public class UnitTestDownloadService
{
    // Positive test case: Verify that DownloadMetarsGZipAsync successfully downloads a GZipped file and returns a non-empty stream
    // Note: This test will make a real HTTP request to download the GZipped METAR data, so it may fail if there are network issues or if the URL is not accessible.
    [Fact]
    public async Task DownloadMetarsGZipAsync_ReturnsStream()
    {
        // Download a real GZipped METAR file from the URL and verify that it returns a non-empty stream
        var service = TestHelper.CreateDownloadService(new HttpClient());
        var stream = await service.DownloadMetarsGZipAsync();
        Assert.NotNull(stream);
        Assert.True(stream.Length > 0, "The downloaded stream should not be empty.");
    }

    // Positive test case: Verify that UnzipGzippedStream successfully unzips a valid GZipped stream and returns the expected CSV content
    [Fact]
    public void UnzipGzippedStream_ReturnsUncompressedStream()
    {
        // Load a test GZipped file from the embedded resources and verify that it can be unzipped correctly
        using var gzippedStream = TestHelper.LoadEmbeddedResource("gzipGood.gz");
        
        // Create an instance of the DownloadService to call the UnzipGzippedStream method
        var service = TestHelper.CreateDownloadService();
        var uncompressedStream = service.UnzipGzippedStream(gzippedStream);
        Assert.NotNull(uncompressedStream);

        // Verify that the unzipped stream contains the expected CSV data, compare it to the csvGood.csv embedded resource
        using var reader = new StreamReader(uncompressedStream);
        var uncompressedContent = reader.ReadToEnd();
        var expectedContent = TestHelper.ReadEmbeddedResourceAsString("csvGood.csv");
        Assert.Equal(expectedContent, uncompressedContent);
    }

    // Negative test case: Verify that UnzipGzippedStream returns an empty stream when given a null input stream
    [Fact]
    public void UnzipGzippedStream_ReturnsEmptyStreamForNullInput()
    {
        // Verify that passing a null stream to UnzipGzippedStream returns an empty stream
        var service = TestHelper.CreateDownloadService();
        var resultStream = service.UnzipGzippedStream(null!);
        Assert.NotNull(resultStream);
        Assert.Equal(Stream.Null, resultStream);
    }

    // Positive test case: Verify that ParseCsvStream successfully parses a valid CSV stream and returns a list of Observation objects
    [Fact]
    public void ParseCsvStream_ReturnsObservations()
    {
        // Load a test CSV file from the embedded resources and verify that it can be parsed into a list of Observation objects
        using var csvStream = TestHelper.LoadEmbeddedResource("csvGood.csv");

        // Create an instance of the DownloadService to call the ParseCsvStream method
        var service = TestHelper.CreateDownloadService();
        var observations = service.ParseCsvStream(csvStream);
        Assert.NotNull(observations);
        Assert.NotEmpty(observations);
    }

    // Negative test case: Verify that ParseCsvStream returns an empty list when given a null input stream
    [Fact]
    public void ParseCsvStream_ReturnsEmptyListForNullInput()
    {
        // Verify that passing a null stream to ParseCsvStream returns an empty list
        var service = TestHelper.CreateDownloadService();
        var result = service.ParseCsvStream(null!);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Negative test case: Verify that ParseCsvStream returns an empty list when the CSV data is missing required columns
    [Fact]
    public void ParseCsvStream_ReturnsEmptyListForNullInputForMissingColumn()
    {
        // Load a test CSV file with missing columns from the embedded resources and verify that it returns an empty list
        using var csvStream = TestHelper.LoadEmbeddedResource("csvBad1.csv");

        // Create an instance of the DownloadService to call the ParseCsvStream method
        var service = TestHelper.CreateDownloadService();
        var result = service.ParseCsvStream(csvStream);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Negative test case: Verify that ParseCsvStream returns an empty list when the CSV data has invalid values in the required columns
    [Fact]
    public void ParseCsvStream_ReturnsEmptyListForMissingData()
    {
        // Load a test CSV file with invalid data from the embedded resources and verify that it returns an empty list
        using var csvStream = TestHelper.LoadEmbeddedResource("csvBad2.csv");

        // Create an instance of the DownloadService to call the ParseCsvStream method
        var service = TestHelper.CreateDownloadService();
        var result = service.ParseCsvStream(csvStream);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // Positive test case: Verify that ExtractLatestObservationsAsync successfully downloads, unzips, parses the CSV data, and returns a list of Observation objects
    // Note: This test will make a real HTTP request to download the GZipped METAR data, so it may fail if there are network issues or if the URL is not accessible.
    [Fact]
    public async Task ExtractLatestObservationsAsync_ReturnsObservations()
    {
        // This test will call the ExtractLatestObservationsAsync method and verify that it returns a non-empty list of Observation objects
        var service = TestHelper.CreateDownloadService(new HttpClient());
        var observations = await service.FetchLatestObservationsAsync();
        Assert.NotNull(observations);
        Assert.NotEmpty(observations);
    }
}
