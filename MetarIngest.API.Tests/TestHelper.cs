namespace MetarIngest.API.Tests;

using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

/// <summary>
/// Provides helper methods for test setup and common operations across test files.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Creates a new instance of DownloadService with mocked dependencies.
    /// </summary>
    /// <param name="httpClient">Optional HttpClient instance. If null, a new HttpClient will be created.</param>
    /// <returns>A configured DownloadService instance for testing.</returns>
    public static DownloadService CreateDownloadService(HttpClient? httpClient = null)
    {
        var mockLogger = new Mock<ILogger<DownloadService>>();
        var settings = Options.Create(new MetarSettings());
        return new DownloadService(httpClient ?? new HttpClient(), mockLogger.Object, settings);
    }

    /// <summary>
    /// Loads an embedded resource stream from the test assembly.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource file (e.g., "csvGood.csv").</param>
    /// <returns>A stream containing the embedded resource data.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resource cannot be found.</exception>
    public static Stream LoadEmbeddedResource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream($"MetarIngest.API.Tests.TestData.{resourceName}");
        
        if (stream == null)
        {
            throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        }
        
        return stream;
    }

    /// <summary>
    /// Reads the content of an embedded resource as a string.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource file (e.g., "csvGood.csv").</param>
    /// <returns>The content of the embedded resource as a string.</returns>
    public static string ReadEmbeddedResourceAsString(string resourceName)
    {
        using var stream = LoadEmbeddedResource(resourceName);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Creates an in-memory database context for testing with a unique database name.
    /// </summary>
    /// <param name="databaseName">Optional database name. If null, a unique name will be generated.</param>
    /// <returns>A configured AppDbContext using an in-memory database.</returns>
    public static AppDbContext CreateInMemoryDbContext(string? databaseName = null)
    {
        var dbName = databaseName ?? $"TestDatabase_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Creates an instance of IngestionService with an in-memory database and mocked dependencies.
    /// </summary>
    /// <param name="dbContext">Optional database context. If null, a new in-memory context will be created.</param>
    /// <param name="mockDownloadService">Optional mocked download service. If null, a new mock will be created.</param>
    /// <returns>A tuple containing the IngestionService, DbContext, and mock DownloadService.</returns>
    public static (IngestionService service, AppDbContext dbContext, Mock<IDownloadService> downloadServiceMock) 
        CreateIngestionService(AppDbContext? dbContext = null, Mock<IDownloadService>? mockDownloadService = null)
    {
        var context = dbContext ?? CreateInMemoryDbContext();
        var downloadMock = mockDownloadService ?? new Mock<IDownloadService>();
        var mockLogger = new Mock<ILogger<IngestionService>>();
        var service = new IngestionService(context, downloadMock.Object, mockLogger.Object);
        
        return (service, context, downloadMock);
    }

    /// <summary>
    /// Creates a test web application factory with the specified configuration settings.
    /// </summary>
    /// <param name="configuration">Dictionary of configuration key-value pairs.</param>
    /// <returns>A configured TestWebApplicationFactory instance.</returns>
    public static TestWebApplicationFactory CreateTestWebApplicationFactory(Dictionary<string, string?> configuration)
    {
        return new TestWebApplicationFactory(configuration);
    }

    /// <summary>
    /// Creates a test web application factory with default in-memory database configuration.
    /// </summary>
    /// <param name="enablePeriodicUpdates">Whether to enable periodic updates in the test application.</param>
    /// <returns>A configured TestWebApplicationFactory instance with in-memory database.</returns>
    public static TestWebApplicationFactory CreateTestWebApplicationFactoryWithInMemoryDb(bool enablePeriodicUpdates = false)
    {
        var databaseName = $"TestDatabase_{Guid.NewGuid()}";
        return new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "true",
            ["InMemoryDatabaseName"] = databaseName,
            ["EnablePeriodicUpdates"] = enablePeriodicUpdates.ToString().ToLower()
        });
    }

    /// <summary>
    /// Creates a test web application factory configured to use SQLite database.
    /// </summary>
    /// <param name="enablePeriodicUpdates">Whether to enable periodic updates in the test application.</param>
    /// <returns>A tuple containing the factory and the database file name.</returns>
    public static (TestWebApplicationFactory factory, string databaseFileName) 
        CreateTestWebApplicationFactoryWithSQLite(bool enablePeriodicUpdates = true)
    {
        var databaseName = $"TestDatabase_{Guid.NewGuid()}.db";
        var factory = new TestWebApplicationFactory(new Dictionary<string, string?>
        {
            ["UseInMemoryDatabase"] = "false",
            ["ConnectionString"] = $"Data Source={databaseName}",
            ["EnablePeriodicUpdates"] = enablePeriodicUpdates.ToString().ToLower()
        });
        
        return (factory, databaseName);
    }

    /// <summary>
    /// Cleans up resources used by an integration test including database deletion and factory disposal.
    /// </summary>
    /// <param name="factory">The TestWebApplicationFactory to dispose.</param>
    /// <param name="databaseFileName">Optional SQLite database file name to delete.</param>
    public static async Task CleanupIntegrationTestAsync(TestWebApplicationFactory factory, string? databaseFileName = null)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        
        factory.Dispose();
        
        // If a SQLite database file was specified, clean it up
        if (!string.IsNullOrEmpty(databaseFileName))
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(databaseFileName))
            {
                File.Delete(databaseFileName);
            }
        }
    }

    /// <summary>
    /// Creates a test observation with the specified parameters.
    /// </summary>
    /// <param name="stationId">The station identifier.</param>
    /// <param name="observationTime">The observation time. If null, uses current UTC time.</param>
    /// <param name="temperature">The temperature value.</param>
    /// <param name="rawMetar">The raw METAR string.</param>
    /// <returns>A new Observation instance for testing.</returns>
    public static Observation CreateTestObservation(
        string stationId = "TEST",
        DateTime? observationTime = null,
        float temperature = 20.5f,
        string rawMetar = "TEST METAR")
    {
        return new Observation(stationId, observationTime ?? DateTime.UtcNow, temperature, rawMetar);
    }
}
