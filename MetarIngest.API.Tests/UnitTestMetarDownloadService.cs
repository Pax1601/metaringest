namespace MetarIngest.API.Tests;

public class UnitTestDownloadService
{
    // Positive test case: Verify that DownloadMetarsGZipAsync successfully downloads a GZipped file and returns a non-empty stream
    // Note: This test will make a real HTTP request to download the GZipped METAR data, so it may fail if there are network issues or if the URL is not accessible.
    [Fact]
    public async Task DownloadMetarsGZipAsync_ReturnsStream()
    {
        // Download a real GZipped METAR file from the URL and verify that it returns a non-empty stream
        var service = TestHelper.CreateDownloadService();
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

        // Verify that the parsed observations contain the expected data based on the csvGood.csv embedded resource
        var expectedObservations = new List<Observation>
        {
            new Observation("UMKK", DateTime.Parse("2026-02-11T13:27:00.000Z").ToUniversalTime(), -2.0, "SPECI UMKK 111327Z 08004MPS 1000 0900S R24/1000D +SN SCT004 OVC017 M02/M02 Q0989 R24/590393 NOSIG RMK QBB120 OBST OBSC QFE740/0987"),
            new Observation("ULLI", DateTime.Parse("2026-02-11T13:27:00.000Z").ToUniversalTime(), -6.0, "SPECI ULLI 111327Z 31002MPS 9000 -SHSN BKN015CB OVC040 M06/M08 Q0996 R88/450542 NOSIG"),
            new Observation("PAKI", DateTime.Parse("2026-02-11T13:26:00.000Z").ToUniversalTime(), -1.0, "SPECI PAKI 111326Z AUTO 16023KT 5SM BR OVC010 M01/M01 A2867 RMK AO2 PK WND 16032/1306 SNE20 CIG 007V012 P0000 FZRANO"),
        };
        Assert.Equal(expectedObservations.Count, observations.Count);
        for (int i = 0; i < expectedObservations.Count; i++)
        {
            Assert.Equal(expectedObservations[i].StationId, observations[i].StationId);
            Assert.Equal(expectedObservations[i].ObservationTime, observations[i].ObservationTime);
            Assert.Equal(expectedObservations[i].Temperature, observations[i].Temperature);
            Assert.Equal(expectedObservations[i].RawMetar, observations[i].RawMetar);
        }
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
        var service = TestHelper.CreateDownloadService();
        var observations = await service.FetchLatestObservationsAsync();
        Assert.NotNull(observations);
        Assert.NotEmpty(observations);
    }
}
