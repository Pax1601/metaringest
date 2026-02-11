namespace MetarIngest.API.Tests;

using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class UnitTestIngestionService
{
    // Positive test case: Verify that IngestObservationAsync successfully adds a new observation to the database context and saves changes
    [Fact]
    public async Task IngestObservationAsync_AddsObservationToDatabase()
    {
        // Create an in-memory database context for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        var dbContext = new AppDbContext(options);
        
        // Create a mock DownloadService (not used in this test, but required for the constructor)
        var mockDownloadService = new Mock<IDownloadService>();
        var mockLogger = new Mock<ILogger<IngestionService>>();
        
        // Create an instance of the IngestionService with the in-memory database context and mock download service
        var ingestionService = new IngestionService(dbContext, mockDownloadService.Object, mockLogger.Object);
        
        // Create a test observation to ingest
        var observation = new Observation("TEST", DateTime.UtcNow, 20.5f, "TEST METAR");
        
        // Call the IngestObservationAsync method to add the observation to the database
        await ingestionService.IngestObservationAsync(observation);
        
        // Verify that the observation was added to the database context and saved
        var savedObservation = await dbContext.Observations.FindAsync(observation.StationId, observation.ObservationTime);
        Assert.NotNull(savedObservation);
        Assert.Equal(observation.StationId, savedObservation!.StationId);
        Assert.Equal(observation.ObservationTime, savedObservation.ObservationTime);
        Assert.Equal(observation.Temperature, savedObservation.Temperature);
        Assert.Equal(observation.RawMetar, savedObservation.RawMetar);
    }

    // Negative test case: Verify that IngestObservationAsync throws an exception when given a null observation
    [Fact]
    public async Task IngestObservationAsync_ThrowsExceptionForNullObservation()
    {
        // Create an in-memory database context for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        var dbContext = new AppDbContext(options);
        
        // Create a mock DownloadService (not used in this test, but required for the constructor)
        var mockDownloadService = new Mock<IDownloadService>();
        var mockLogger = new Mock<ILogger<IngestionService>>();
        
        // Create an instance of the IngestionService with the in-memory database context and mock download service
        var ingestionService = new IngestionService(dbContext, mockDownloadService.Object, mockLogger.Object);
        
        // Verify that calling IngestObservationAsync with a null observation throws an ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(() => ingestionService.IngestObservationAsync(null!));
    }

    // Positive test case: Verify that IngestLatestObservationsAsync does not add duplicate observations to the database context
    [Fact]
    public async Task IngestLatestObservationsAsync_DoesNotAddDuplicateObservationsInSameBatch()
    {
        // Create an in-memory database context for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        var dbContext = new AppDbContext(options);
        
        // Create a mock DownloadService that returns a list of observations, including a duplicate
        var mockDownloadService = new Mock<IDownloadService>();
        var time = DateTime.UtcNow;
        var observation1 = new Observation("TEST", time, 20.5f, "TEST METAR");
        var observation2 = new Observation("TEST", time, 20.5f, "TEST METAR"); // Duplicate observation
        mockDownloadService.Setup(s => s.FetchLatestObservationsAsync()).ReturnsAsync(new List<Observation> { observation1, observation2 });
        
        // Create an instance of the IngestionService with the in-memory database context and mock download service
        var mockLogger = new Mock<ILogger<IngestionService>>();
        var ingestionService = new IngestionService(dbContext, mockDownloadService.Object, mockLogger.Object);
        
        // Call the IngestLatestObservationsAsync method to add the observations to the database
        await ingestionService.IngestLatestObservationsAsync();
        
        // Verify that only one observation was added to the database context and saved (the duplicate should be skipped)
        var savedObservations = await dbContext.Observations.ToListAsync();
        Assert.Single(savedObservations);
        Assert.Equal(observation1.StationId, savedObservations[0].StationId);
        Assert.Equal(observation1.ObservationTime, savedObservations[0].ObservationTime);
        Assert.Equal(observation1.Temperature, savedObservations[0].Temperature);
        Assert.Equal(observation1.RawMetar, savedObservations[0].RawMetar);
    }

    // Positive test case: Verify that IngestLatestObservationsAsync does not add observations that already exist in the database context
    [Fact]
    public async Task IngestLatestObservationsAsync_DoesNotAddExistingObservations()
    {
        // Create an in-memory database context for testing
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDatabase")
            .Options;
        var dbContext = new AppDbContext(options);
        
        // Create a mock DownloadService that returns a list of observations
        var mockDownloadService = new Mock<IDownloadService>();
        var time = DateTime.UtcNow;
        var observation1 = new Observation("TEST", time, 20.5f, "TEST METAR");
        mockDownloadService.Setup(s => s.FetchLatestObservationsAsync()).ReturnsAsync(new List<Observation> { observation1 });
        
        // Create an instance of the IngestionService with the in-memory database context and mock download service
        var mockLogger = new Mock<ILogger<IngestionService>>();
        var ingestionService = new IngestionService(dbContext, mockDownloadService.Object, mockLogger.Object);
        
        // Add the observation to the database context and save changes to simulate an existing observation
        dbContext.Observations.Add(observation1);
        await dbContext.SaveChangesAsync();
        
        // Call the IngestLatestObservationsAsync method to attempt to add the same observation again
        await ingestionService.IngestLatestObservationsAsync();
        
        // Verify that only one observation exists in the database context (the duplicate should be skipped)
        var savedObservations = await dbContext.Observations.ToListAsync();
        Assert.Single(savedObservations);
        Assert.Equal(observation1.StationId, savedObservations[0].StationId);
        Assert.Equal(observation1.ObservationTime, savedObservations[0].ObservationTime);
        Assert.Equal(observation1.Temperature, savedObservations[0].Temperature);
        Assert.Equal(observation1.RawMetar, savedObservations[0].RawMetar);
    }
}