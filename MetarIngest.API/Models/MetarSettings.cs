/*********************************************************************************
* METAR Settings
* Configuration class for METAR download and update settings
*********************************************************************************/

public class MetarSettings
{
    public string Url { get; set; } = "https://aviationweather.gov/data/cache/metars.cache.csv.gz";
    public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(10);
    public bool EnablePeriodicUpdates { get; set; } = true;
}
