using SlLib.Resources.Database;

namespace SlLib.Serialization;

/// <summary>
///     Represents a resource in Sonic & All-Stars Racing Transformed.
/// </summary>
public interface ISumoResource : ILoadable
{
    /// <summary>
    ///     Resource metadata header.
    /// </summary>
    public SlResourceHeader Header { get; set; }
}