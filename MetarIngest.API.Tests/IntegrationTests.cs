namespace MetarIngest.API.Tests;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// Custom factory that configures settings before Program runs
// This is necessary to avoid the fact that Program.cs will by default start to ingest data automatically
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Dictionary<string, string?> _configuration;

    public TestWebApplicationFactory(Dictionary<string, string?> configuration)
    {
        _configuration = configuration;
    }

    protected override IHostBuilder CreateHostBuilder()
    {
        return Program.CreateHostBuilder(Array.Empty<string>(), config =>
        {
            // Add test configuration which will override default sources
            config.AddInMemoryCollection(_configuration);
        });
    }
}

public class IntegrationTests
{
    // Positive test case: Verify that IngestRealMetarData successfully ingests real METAR data without errors
    [Fact]
    public async Task IngestRealMetarData_ShouldSucceed()
    {
        var (factory, _) = TestHelper.CreateTestWebApplicationFactory();

        using var scope = factory.Services.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Verify that the database is empty before the test
            var initialObservationsCount = await dbContext.Observations.CountAsync();
            Assert.Equal(0, initialObservationsCount);

            // Ingest the latest observations
            await ingestionService.IngestLatestObservationsAsync();

            // Verify that observations were ingested into the database
            var observationsCount = await dbContext.Observations.CountAsync();
            Assert.True(observationsCount > 0, "No observations were ingested into the database.");

            // Verify that the ingested observations have valid data (e.g., non-null StationId, valid ObservationTime)
            var ingestedObservation = await dbContext.Observations.FirstOrDefaultAsync();
            Assert.NotNull(ingestedObservation);
            Assert.False(string.IsNullOrEmpty(ingestedObservation!.StationId), "Ingested observation has an empty StationId.");
            Assert.True(ingestedObservation.ObservationTime > DateTime.MinValue, "Ingested observation has an invalid ObservationTime.");
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory);
        }
    }

    // Negative test case: Verify that IngestRealMetarData handles errors gracefully by forcing a network error using an invalid URL
    [Fact]
    public async Task IngestRealMetarData_ShouldFail()
    {
        var (factory, _) = TestHelper.CreateTestWebApplicationFactory(
            downloadUrl: "https://invalid-url-for-testing.com/metars.cache.csv.gz");

        using var scope = factory.Services.CreateScope();
        var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            // Verify that the database is empty before the test
            var initialObservationsCount = await dbContext.Observations.CountAsync();
            Assert.Equal(0, initialObservationsCount);

            // Attempt to ingest the latest observations and verify that it handles the error gracefully
            await ingestionService.IngestLatestObservationsAsync();

            // Verify that no observations were ingested into the database
            var observationsCount = await dbContext.Observations.CountAsync();
            Assert.Equal(0, observationsCount);
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory);
        }
    }

    // Positive test case: Start the application, fetch the latest observations, and then try to retrieve both the latest observation and 
    // the average temperature testing the HTTP endpoints
    [Fact]
    public async Task FetchLatestObservations_ShouldReturnData()
    {
        var (factory, _) = TestHelper.CreateTestWebApplicationFactory();

        using var client = factory.CreateClient();

        try
        {
            // Ingest the latest observations by calling the ingestion service directly
            using var scope = factory.Services.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            await ingestionService.IngestLatestObservationsAsync();

            // Fetch the latest observation for a known station ID (Milan Linate Airport - LIML)
            var response = await client.GetAsync("/observations/LIML");
            response.EnsureSuccessStatusCode();
            var observationJson = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(observationJson), "The response should contain observation data.");

            // Fetch the average temperature for a known station ID (Milan Linate Airport - LIML)
            var avgTempResponse = await client.GetAsync("/observations/LIML/average-temperature");
            avgTempResponse.EnsureSuccessStatusCode();
            var avgTempJson = await avgTempResponse.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(avgTempJson), "The response should contain average temperature data.");
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory);
        }
    }

    // Negative test case: Start the application, fetch the latest observations, and then try to retrieve both the latest observation and 
    // the average temperature testing the HTTP endpoints for a non-existent station ID, verifying that the endpoints return 404 Not Found
    [Fact]
    public async Task FetchLatestObservations_ShouldReturnNotFoundForNonExistentStation()
    {
        var (factory, _) = TestHelper.CreateTestWebApplicationFactory();

        using var client = factory.CreateClient();

        try
        {
            // Ingest the latest observations by calling the ingestion service directly
            using var scope = factory.Services.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            await ingestionService.IngestLatestObservationsAsync();

            // Fetch the latest observation for a non-existent station ID
            var response = await client.GetAsync("/observations/XXXX");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);

            // Fetch the average temperature for a non-existent station ID
            var avgTempResponse = await client.GetAsync("/observations/XXXX/average-temperature");
            Assert.Equal(System.Net.HttpStatusCode.NotFound, avgTempResponse.StatusCode);
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory);
        }
    }

    // Positive test case: Check that observations older than 24 hours are deleted by the periodic update service
    [Fact]
    public async Task PeriodicUpdateService_ShouldDeleteOldObservations()
    {
        var (factory, databaseFileName) = TestHelper.CreateTestWebApplicationFactory(useInMemoryDatabase: true,
         enablePeriodicUpdates: true, updateInterval: TimeSpan.FromSeconds(10));
        using var client = factory.CreateClient();

        try
        {
            // Wait until the application is ready and the database is initialized
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            await ingestionService.WaitForDatabaseReadyAsync(CancellationToken.None);

            // Add an observation older than 24 hours to the database
            var oldObservation = TestHelper.CreateTestObservation("OLD1", DateTime.UtcNow.AddHours(-25), 15.0f, "OLD METAR");
            dbContext.Observations.Add(oldObservation);
            await dbContext.SaveChangesAsync();
            var oldStationId = oldObservation.StationId;
            var oldObservationTime = oldObservation.ObservationTime;

            // Wait for the periodic update service to run and delete old observations. Timeout after 10 seconds
            var maxWaitTime = TimeSpan.FromSeconds(30);
            var startTime = DateTime.UtcNow;
            while (true)
            {
                // Use a fresh scope/context each iteration so we read current database state,
                // not tracked entities from an earlier context.
                using var pollScope = factory.Services.CreateScope();
                var pollDbContext = pollScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var observationExists = await pollDbContext.Observations
                    .AsNoTracking()
                    .AnyAsync(o => o.StationId == oldStationId && o.ObservationTime == oldObservationTime);

                if (!observationExists)
                {
                    break; // Old observation has been deleted, test passed
                }

                if (DateTime.UtcNow - startTime > maxWaitTime)
                {
                    throw new TimeoutException("Timed out waiting for the periodic update service to delete old observations.");
                }
                await Task.Delay(1000);
            }
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory, databaseFileName);
        }
    }

    // Positive test case: Verify that the periodic update service successfully ingests data at the specified interval
        [Fact]
    public async Task PeriodicUpdateService_ShouldIngestDataAtInterval()
    {
        var (factory, databaseFileName) = TestHelper.CreateTestWebApplicationFactory(useInMemoryDatabase: true,
         enablePeriodicUpdates: true, updateInterval: TimeSpan.FromSeconds(10));
        using var client = factory.CreateClient();

        try
        {
            // Wait until the application is ready and the database is initialized
            using var scope = factory.Services.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            await ingestionService.WaitForDatabaseReadyAsync(CancellationToken.None);

            // Wait for the periodic update service to run and ingest data. Timeout after 30 seconds
            var maxWaitTime = TimeSpan.FromSeconds(30);
            await TestHelper.WaitForObservationsAsync(
                factory,
                maxWaitTime,
                "Timed out waiting for the periodic update service to ingest data.");

            // Delete all the ingested observations to prepare for the next ingestion cycle
            using (var deleteScope = factory.Services.CreateScope())
            {
                var deleteDbContext = deleteScope.ServiceProvider.GetRequiredService<AppDbContext>();
                deleteDbContext.Observations.RemoveRange(deleteDbContext.Observations);
                await deleteDbContext.SaveChangesAsync();
            }

            // Verify that the database is empty after deletion
            using var verifyScope = factory.Services.CreateScope();
            var verifyDbContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var observationsCountAfterDeletion = await verifyDbContext.Observations.AsNoTracking().CountAsync();
            Assert.Equal(0, observationsCountAfterDeletion);

            // Wait for the periodic update service to run again and ingest data. Timeout after 30 seconds
            await TestHelper.WaitForObservationsAsync(
                factory,
                maxWaitTime,
                "Timed out waiting for the periodic update service to ingest data again.");
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory, databaseFileName);
        }
    }

    // Positive test case: Start the application using a test SQLite database, wait for the periodic updates, and then try to retrieve 
    // both the latest observation and the average temperature testing the HTTP endpoints
    [Fact]
    public async Task FetchLatestObservations_ShouldReturnData_WithSQLite()
    {
        var (factory, databaseFileName) = TestHelper.CreateTestWebApplicationFactory(useInMemoryDatabase: false);
        using var client = factory.CreateClient();

        try
        {
            // Wait until the application is ready and the database is initialized
            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IIngestionService>();
            await ingestionService.WaitForDatabaseReadyAsync(CancellationToken.None);

            // Wait for the ingestion service to ingest some data into the database. Timeout after 30 seconds
            var maxWaitTime = TimeSpan.FromSeconds(30);
            await TestHelper.WaitForObservationsAsync(
                factory,
                maxWaitTime,
                "Timed out waiting for data to be ingested into the database.");
            
            // Fetch the latest observation for a known station ID (Milan Linate Airport - LIML)
            var response = await client.GetAsync("/observations/LIML");
            response.EnsureSuccessStatusCode();
            var observationJson = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(observationJson), "The response should contain observation data.");

            // Fetch the average temperature for a known station ID (Milan Linate Airport - LIML)
            var avgTempResponse = await client.GetAsync("/observations/LIML/average-temperature");
            avgTempResponse.EnsureSuccessStatusCode();
            var avgTempJson = await avgTempResponse.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(avgTempJson), "The response should contain average temperature data.");

            // Fetch all the average temperature observations and check at least one is present
            var allAvgTempResponse = await client.GetAsync("/observations/average-temperature");
            allAvgTempResponse.EnsureSuccessStatusCode();
            var allAvgTempJson = await allAvgTempResponse.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrEmpty(allAvgTempJson), "The response should contain average temperature data.");
        }
        finally
        {
            // Cleanup: Delete the database after the test
            await TestHelper.CleanupIntegrationTestAsync(factory, databaseFileName);
        }
    }
}
