/// <summary>
/// This class represents a METAR observation. It contains the Station ID (ICAO code), the observation time,
/// the temperature in Celsius, and the raw METAR string.
/// </summary>
public class Observation
{
    // Observations are immutable after creation (an observation can not be changed unless we bend physics :)).
    // All parameters are required because they are always present in a valid METAR report.
    /// <summary>
    /// ICAO code of the station.
    /// </summary>
    public string StationId { get; private set; }
    /// <summary>
    /// Time of the observation (UTC).
    /// </summary>
    public DateTime ObservationTime { get; private set; }
    /// <summary>
    /// Temperature in Celsius. 
    /// </summary> 
    public double Temperature { get; private set; } 
    /// <summary>
    /// Raw METAR string.
    /// </summary>
    public string RawMetar { get; private set; }

    /// <summary>
    /// Observation constructor.
    /// </summary>
    /// <param name="stationId">ICAO code of the station.</param>
    /// <param name="observationTime">Time of the observation (UTC).</param>
    /// <param name="temperature">Temperature in Celsius.</param>
    /// <param name="rawMetar">Raw METAR string.</param>
    public Observation(string stationId, DateTime observationTime, double temperature, string rawMetar)
    {
        StationId = stationId;
        ObservationTime = observationTime;
        Temperature = temperature;
        RawMetar = rawMetar;
    }
}