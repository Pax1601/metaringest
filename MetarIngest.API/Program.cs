using Microsoft.EntityFrameworkCore;

/// <summary>
/// <c> METAR Ingest API </c>.
/// The application uses Entity Framework Core for database access and is configured to use either an in-memory database or a SQLite database based on configuration settings. 
/// It uses a Minimal API with a Swagger UI for testing and documentation.
/// </summary>
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args, Action<IConfigurationBuilder>? configureAppConfiguration = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // Allow tests to override configuration
                configureAppConfiguration?.Invoke(config);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices((context, services) =>
                {
                    var configuration = context.Configuration;

                    // Configure the database context to use either an in-memory database or SQLite based on configuration
                    // In-memory database is used for testing and development, while SQLite is used for production
                    var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase");
                    if (useInMemoryDatabase)
                    {
                        var databaseName = configuration.GetValue<string>("InMemoryDatabaseName") ?? "MetarIngestDatabase";
                        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
                    }
                    else
                    {
                        var connectionString = configuration.GetValue<string>("ConnectionString") ?? "Data Source=metaringest.db";
                        services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
                    }

                    // Configure settings
                    services.Configure<MetarAPISettings>(options =>
                    {
                        options.Url = configuration.GetValue<string>("DownloadUrl") ?? "https://aviationweather.gov/data/cache/metars.cache.csv.gz";
                        options.UpdateInterval = configuration.GetValue("UpdateInterval", TimeSpan.FromMinutes(10));

                        // Check that the update interval is a positive value and is greater than 10 seconds to prevent excessively frequent updates
                        if (options.UpdateInterval < TimeSpan.FromSeconds(10))
                        {
                            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Program>();
                            logger.LogWarning("The configured update interval of {Interval} is lower than the minimum value of 10 seconds. Using 10 seconds instead.", options.UpdateInterval);
                            options.UpdateInterval = TimeSpan.FromSeconds(10);
                        }

                        options.EnablePeriodicUpdates = configuration.GetValue("EnablePeriodicUpdates", true);
                        options.ConnectionString = configuration.GetValue<string>("ConnectionString") ?? "Data Source=metaringest.db";
                    });

                    // Register application services
                    services.AddHttpClient<IDownloadService, DownloadService>();
                    services.AddScoped<IIngestionService, IngestionService>();

                    // Conditionally register PeriodicUpdateService based on configuration
                    if (configuration.GetValue("EnablePeriodicUpdates", true))
                    {
                        services.AddHostedService<PeriodicUpdateService>();
                    }

                    // Add services for controllers and Swagger support
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen();
                });

                webBuilder.Configure((context, app) =>
                {
                    var configuration = context.Configuration;
                    var useInMemoryDatabase = configuration.GetValue<bool>("UseInMemoryDatabase");
                    var aspNetCoreUrls = configuration["ASPNETCORE_URLS"];
                    var hasHttpsUrl = !string.IsNullOrWhiteSpace(aspNetCoreUrls)
                        && aspNetCoreUrls.Split(';', StringSplitOptions.RemoveEmptyEntries)
                            .Any(url => url.TrimStart().StartsWith("https://", StringComparison.OrdinalIgnoreCase));
                    var httpsPort = configuration.GetValue<int?>("HTTPS_PORT")
                        ?? configuration.GetValue<int?>("ASPNETCORE_HTTPS_PORT")
                        ?? configuration.GetValue<int?>("HttpsPort");

                    // Apply database migrations at startup (only for SQLite)
                    if (!useInMemoryDatabase)
                    {
                        using (var scope = app.ApplicationServices.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            dbContext.Database.Migrate();
                        }
                    }

                    // Configure the HTTP request pipeline and enable Swagger UI
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    if (hasHttpsUrl || httpsPort.HasValue)
                    {
                        app.UseHttpsRedirection();
                    }
                    app.UseRouting();
                    app.UseStaticFiles();

                    app.UseEndpoints(endpoints =>
                    {
                        // Add a get method to retrieve the latest METAR observation for a given station
                        endpoints.MapGet("/observations/{stationId}", async (string stationId, AppDbContext dbContext) =>
                        {
                            // Retrieve the latest observation for the specified station ID
                            var observation = await dbContext.Observations
                                .Where(o => o.StationId == stationId)
                                .OrderByDescending(o => o.ObservationTime)
                                .FirstOrDefaultAsync(); 

                            // Return 404 if no observation is found for the specified station ID
                            if (observation == null)
                            {
                                return Results.NotFound();
                            }

                            return Results.Ok(observation);
                        });

                        // Add a get method to retrieve the average temperature for a given station over the last 24 hours
                        endpoints.MapGet("/observations/{stationId}/average-temperature", async (string stationId, AppDbContext dbContext) =>
                        {
                            // Calculate the cutoff time for the last 24 hours
                            var cutoffTime = DateTime.UtcNow.AddHours(-24); 

                            // Calculate the average temperature for the specified station ID over the last 24 hours
                            var averageTemperature = await dbContext.Observations
                                .Where(o => o.StationId == stationId && o.ObservationTime >= cutoffTime)
                                .AverageAsync(o => (double?)o.Temperature); 

                            // Return 404 if no observations are found for the specified station ID in the last 24 hours
                            if (averageTemperature == null)
                            {
                                return Results.NotFound();
                            }

                            return Results.Ok(new { StationId = stationId, AverageTemperature = averageTemperature });
                        });

                        // Download the average temperature for all stations over the last 24 hours
                        endpoints.MapGet("/observations/average-temperature", async (AppDbContext dbContext) =>
                        {
                            // Calculate the cutoff time for the last 24 hours
                            var cutoffTime = DateTime.UtcNow.AddHours(-24); 

                            // Calculate the average temperature for all stations over the last 24 hours
                            var averageTemperatures = await dbContext.Observations
                                .Where(o => o.ObservationTime >= cutoffTime)
                                .GroupBy(o => o.StationId)
                                .Select(g => new { StationId = g.Key, AverageTemperature = g.Average(o => (double?)o.Temperature) })
                                .ToListAsync();

                            return Results.Ok(averageTemperatures);
                        });
                    });
                });
            });
    }
}

