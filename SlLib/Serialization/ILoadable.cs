namespace SlLib.Serialization;

/// <summary>
///     Represents a class that is able to be serialized from a binary file.
/// </summary>
public interface ILoadable
{
    /// <summary>
    ///     Loads this structure from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to load from</param>
    void Load(ResourceLoadContext context, int offset);
}