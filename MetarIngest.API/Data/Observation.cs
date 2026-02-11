/*********************************************************************************
* METAR Observation Model
* This class represents a METAR observation. It contains the Station ID (ICAO code), the observation time,
* the temperature in Celsius, and the raw METAR string.
*********************************************************************************/

public class Observation
{
    // Observations are immutable after creation, so we use private setters to ensure that the properties cannot be modified after the object is created.
    // All parameters are required because they are always present in a valid METAR report.
    public string StationId { get; private set; } // ICAO code of the station
    public DateTime ObservationTime { get; private set; } // Time of the observation
    public double Temperature { get; private set; } // Temperature in Celsius
    public string RawMetar { get; private set; } // Raw METAR string

    // Constructor to initialize the Observation object
    public Observation(string stationId, DateTime observationTime, double temperature, string rawMetar)
    {
        StationId = stationId;
        ObservationTime = observationTime;
        Temperature = temperature;
        RawMetar = rawMetar;
    }
}