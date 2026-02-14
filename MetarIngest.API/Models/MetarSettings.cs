/// <summary>
/// Configuration class for the METAR API.
/// </summary>
public class MetarAPISettings
{
    /// <summary>
    /// URL of the METAR data source. Default is https://aviationweather.gov/data/cache/metars.cache.csv.gz.
    /// </summary>
    public string Url { get; set; } = "https://aviationweather.gov/data/cache/metars.cache.csv.gz";
    /// <summary>
    /// Interval between METAR data updates. Default is 10 minutes.
    /// </summary>
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(10);
    /// <summary>
    /// Whether to enable periodic updates of METAR data. Default is true. If false, the data must be fetched manually.
    /// </summary>
    public bool EnablePeriodicUpdates { get; set; } = true;
    /// <summary>
    /// Connection string for the SQLite database. Default is "Data Source=metaringest.db".
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=metaringest.db";
}
