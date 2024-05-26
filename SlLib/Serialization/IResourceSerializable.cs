using SlLib.Resources.Database;

namespace SlLib.Serialization;

/// <summary>
///     Represents a resource that can be serialized to/from binary.
/// </summary>
public interface IResourceSerializable
{
    /// <summary>
    ///     Loads this structure from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    void Load(ResourceLoadContext context);

    /// <summary>
    ///     Saves this structure to a buffer.
    /// </summary>
    /// <param name="context">The current save context</param>
    /// <param name="buffer">Buffer to write structure to</param>
    void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the base size of this structure for serialization, not including allocations.
    /// </summary>
    /// <param name="platform">The target platform</param>
    /// <param name="version">The target file version</param>
    /// <returns>Size for serialization</returns>
    int GetSizeForSerialization(SlPlatform platform, int version)
    {
        throw new NotImplementedException();
    }
}