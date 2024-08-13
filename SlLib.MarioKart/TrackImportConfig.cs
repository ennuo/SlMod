namespace SlLib.MarioKart;

public class TrackImportConfig
{
    /// <summary>
    ///     The id of the track in Mario Kart 8 to load.
    /// </summary>
    public string CourseId { get; set; } = string.Empty;
    
    /// <summary>
    ///     The level in Sonic & All Stars Racing Transformed to copy presets from.
    /// </summary>
    public string TrackSource { get; set; } = string.Empty;

    /// <summary>
    ///     Track to overwrite
    /// </summary>
    public string TrackTarget { get; set; } = "seasidehill2";
}