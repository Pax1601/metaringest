/*********************************************************************************
* METAR Ingest API
* This is the main entry point for the METAR Ingest API application. It sets up the web host, configures services, and defines the HTTP request pipeline.
* The application uses Entity Framework Core for database access and is configured to use a SQLite database for simplicity. 
* It uses a Minimal API with a Swagger UI for testing and documentation.
*********************************************************************************/

using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))); // Configure the database context to use SQLite with the connection string from appsettings.json

// Register application services
builder.Services.AddHttpClient<IDownloadService, DownloadService>();
builder.Services.AddScoped<IIngestionService, IngestionService>(); 
builder.Services.AddHostedService(
    serviceProvider =>
    {
        var serviceScopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var logger = serviceProvider.GetRequiredService<ILogger<PeriodicUpdateService>>();
        var updateInterval = builder.Configuration.GetValue("UpdateInterval", TimeSpan.FromMinutes(10)); // Read the update interval from configuration, default to 10 minutes if not specified
        return new PeriodicUpdateService(serviceScopeFactory, logger, updateInterval);
    }
); // Read the update interval from configuration and pass it to the PeriodicUpdateService constructor

// Add services for controllers and Swagger support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); 

var app = builder.Build();

// Apply database migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate(); // Apply any pending migrations
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // Enable Swagger middleware in development environment
    app.UseSwaggerUI(); // Enable Swagger UI middleware in development environment
}

app.UseHttpsRedirection(); // Redirect HTTP requests to HTTPS

// Add a get method to retrieve the latest METAR observation for a given station
app.MapGet("/observations/{stationId}", async (string stationId, AppDbContext dbContext) =>
{
    var observation = await dbContext.Observations
        .Where(o => o.StationId == stationId)
        .OrderByDescending(o => o.ObservationTime)
        .FirstOrDefaultAsync(); // Retrieve the latest observation for the specified station ID

    if (observation == null)
    {
        return Results.NotFound(); 
    }

    return Results.Ok(observation); 
});

// Add a get method to retrieve the average temperature for a given station over the last 24 hours
app.MapGet("/observations/{stationId}/average-temperature", async (string stationId, AppDbContext dbContext) =>
{
    var cutoffTime = DateTime.UtcNow.AddHours(-24); // Calculate the cutoff time for the last 24 hours
    var averageTemperature = await dbContext.Observations
        .Where(o => o.StationId == stationId && o.ObservationTime >= cutoffTime)
        .AverageAsync(o => (double?)o.Temperature); // Calculate the average temperature for the specified station ID over the last 24 hours

    if (averageTemperature == null)
    {
        return Results.NotFound(); 
    }

    return Results.Ok(new { StationId = stationId, AverageTemperature = averageTemperature }); 
});

app.Run(); // Run the application

