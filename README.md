# metaringest
Exercise on creation of an ASP.NET Core project that periodically ingests aviation weather data from NOAA, and provides two endpoints for querying said data.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later

## Building the Project

### Building and running
```bash
git clone <repository-url>
cd metaringest
dotnet restore
dotnet build

cd MetarIngest.API
dotnet run --launch-profile http
```
or
```
dotnet run --launch-profile https
```
Note that using the https profile may require SSL certificates to be installed.

The application will start and be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

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

### Running Tests for a Specific Project

```bash
cd MetarIngest.API.Tests
dotnet test
```

### Running Tests with Coverage

```bash
dotnet test --collect:"XPlat Code Coverage" --settings:.runsettings
```

The `.runsettings` file configures coverage to only include your code (`MetarIngest.API`) and exclude:
- Test assemblies
- Framework code (Microsoft.*, System.*)
- Database migrations
- Auto-generated code

### Generating HTML Coverage Reports

After running tests with coverage, generate an HTML report:

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

### Test Categories

**Unit Tests**: Test individual components in isolation with mocked dependencies

**Integration Tests**: Test the full application stack with real HTTP requests and in-memory database

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

## Project Structure

```
metaringest/
├── MetarIngest.API/           # Main API project
│   ├── Data/                  # Database context and models
│   ├── Models/                # Configuration models
│   ├── Services/              # Business logic services
│   ├── Migrations/            # EF Core migrations
│   └── Program.cs             # Application entry point
└── MetarIngest.API.Tests/     # Test project
    ├── TestData/              # Test data files
    └── *Tests.cs              # Test classes
```

## Troubleshooting

**Database locked errors**: Ensure no other instances of the application are running.

**Port already in use**: Change the port in `launchSettings.json` or set the `ASPNETCORE_URLS` environment variable.

**Tests failing with database conflicts**: Tests use isolated in-memory databases to prevent conflicts.

