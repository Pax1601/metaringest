namespace MetarIngest.API.Tests;

using Microsoft.EntityFrameworkCore;

public class UnitTestIngestionService
{
    // Positive test case: Verify that IngestObservationAsync successfully adds a new observation to the database context and saves changes
    [Fact]
    public async Task IngestObservationAsync_AddsObservationToDatabase()
    {
        // Create an in-memory database context for testing
        var (ingestionService, dbContext, _) = TestHelper.CreateIngestionService();
        
        // Create a test observation to ingest
        var observation = TestHelper.CreateTestObservation();
        
        // Call the IngestObservationAsync method to add the observation to the database
        await ingestionService.IngestObservationAsync(observation);
        
        // Verify that the observation was added to the database context and saved
        var savedObservation = await dbContext.Observations.FindAsync(observation.StationId, observation.ObservationTime);
        Assert.NotNull(savedObservation);
        Assert.Equal(observation.StationId, savedObservation.StationId);
        Assert.Equal(observation.ObservationTime, savedObservation.ObservationTime);
        Assert.Equal(observation.Temperature, savedObservation.Temperature);
        Assert.Equal(observation.RawMetar, savedObservation.RawMetar);
    }

    // Negative test case: Verify that IngestObservationAsync throws an exception when given a null observation
    [Fact]
    public async Task IngestObservationAsync_ThrowsExceptionForNullObservation()
    {
        // Create an in-memory database context for testing
        var (ingestionService, _, _) = TestHelper.CreateIngestionService();
        
        // Verify that calling IngestObservationAsync with a null observation throws an ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(() => ingestionService.IngestObservationAsync(null!));
    }

    // Positive test case: Verify that IngestLatestObservationsAsync does not add duplicate observations to the database context
    [Fact]
    public async Task IngestLatestObservationsAsync_DoesNotAddDuplicateObservationsInSameBatch()
    {
        // Create an in-memory database context for testing
        var (ingestionService, dbContext, mockDownloadService) = TestHelper.CreateIngestionService(
            TestHelper.CreateInMemoryDbContext("TestDatabase1"));
        
        // Create a mock DownloadService that returns a list of observations, including a duplicate
        var time = DateTime.UtcNow;
        var observation1 = TestHelper.CreateTestObservation("TEST", time, 20.5f, "TEST METAR");
        var observation2 = TestHelper.CreateTestObservation("TEST", time, 20.5f, "TEST METAR"); // Duplicate observation
        mockDownloadService.Setup(s => s.FetchLatestObservationsAsync()).Returns(Task.FromResult(new List<Observation> { observation1, observation2 }));
        
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
        var (ingestionService, dbContext, mockDownloadService) = TestHelper.CreateIngestionService(
            TestHelper.CreateInMemoryDbContext("TestDatabase2"));
        
        // Create a mock DownloadService that returns a list of observations
        var time = DateTime.UtcNow;
        var observation1 = TestHelper.CreateTestObservation("TEST", time, 20.5f, "TEST METAR");
        mockDownloadService.Setup(s => s.FetchLatestObservationsAsync()).Returns(Task.FromResult(new List<Observation> { observation1 }));
        
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