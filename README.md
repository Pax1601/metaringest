# metaringest
Exercise on creation of an ASP.NET Core project that periodically ingests aviation weather data from NOAA, and provides two endpoints for querying said data.
Available data are raw text METAR and temperature, and average temperature over a 24h period.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Building the Project

### Building and running
```bash
git clone <https://github.com/Pax1601/metaringest.git>
cd metaringest
dotnet restore
dotnet build

cd MetarIngest.API
dotnet run --launch-profile http
```

or

```bash
dotnet run --launch-profile https
```
Note that using the https profile may require SSL certificates to be installed.

The application will start and be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `http://localhost:5000/swagger`
- Swagger UI: `https://localhost:5001/swagger`

## Sample

A simple sample map is available to visualize the available airports and their observations. Visit `http://localhost:5000/index.html` to inspect it.
Each airport is colored depending on the average temperature in the last 24 hours. Hover on the airport circle to inspect the METAR observation.

## Configuration

The application can be configured via `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=metaringest.db"
  },
  "UpdateInterval": "00:10:00",
  "DownloadUrl": "https://aviationweather.gov/data/cache/metars.cache.csv.gz",
  "EnablePeriodicUpdates": true,
  "UseInMemoryDatabase": false
}
```

**Configuration Options:**
- `UpdateInterval`: How often to fetch new METAR data (default: 10 minutes)
- `DownloadUrl`: URL to download METAR data from
- `EnablePeriodicUpdates`: Whether to automatically fetch data in the background
- `UseInMemoryDatabase`: Use in-memory database instead of SQLite (useful for testing)

## Testing

### Running All Tests

```bash
dotnet test
```

### Running Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings:.runsettings
```

After running tests with coverage you can generate a HTML report. First install the necessary tool with:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
```

then run:

```bash
reportgenerator -reports:"MetarIngest.API.Tests\TestResults\**\coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

Then open `coveragereport\index.html` in your browser.

### Test Projects

The solution includes the following test projects:

- **MetarIngest.API.Tests**: Contains unit tests and integration tests
  - `UnitTestMetarDownloadService.cs`: Tests for METAR download functionality
  - `UnitTestIngestionService.cs`: Tests for data ingestion logic
  - `IntegrationTests.cs`: End-to-end integration tests


## API Endpoints

Once running, the following endpoints are available:

### Get Latest Observation for a Station
```
GET /observations/{stationId}
```
Returns the most recent METAR observation for the specified ICAO station code.

**Example:**
```bash
curl https://localhost:5001/observations/KJFK
```

### Get Average Temperature
```
GET /observations/{stationId}/average-temperature
```
Returns the average temperature for the specified station over the last 24 hours.

**Example:**
```bash
curl https://localhost:5001/observations/KJFK/average-temperature
```

## Database

The application uses SQLite by default, creating a `metaringest.db` file in the API project directory. The database schema is managed through Entity Framework Core migrations.

### Applying Migrations

Migrations are automatically applied on application startup. To manually create or apply migrations:

```bash
cd MetarIngest.API
dotnet ef migrations add MigrationName
dotnet ef database update
```

