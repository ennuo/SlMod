using SlLib.Resources.Database;

namespace SlLib.Serialization;

/// <summary>
///     Represents a resource that is able to be converted
/// </summary>
public interface IPlatformConvertable
{
    /// <summary>
    ///     Converts the resource to a specified target platform.
    /// </summary>
    /// <param name="target">Target platform</param>
    void Convert(SlPlatform target);

    /// <summary>
    ///     Checks if a resource can be converted to a target platform.
    /// </summary>
    /// <param name="target">Target platform</param>
    /// <returns>Whether or not the resource can be converted</returns>
    bool CanConvert(SlPlatform target);
}