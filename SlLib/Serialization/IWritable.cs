namespace SlLib.Serialization;

/// <summary>
///     Represents a class that is able to be serialized to binary.
/// </summary>
public interface IWritable
{
    /// <summary>
    ///     Saves this structure to a buffer.
    /// </summary>
    /// <param name="context">The current save context</param>
    /// <param name="buffer">Buffer to write structure to</param>
    void Save(ResourceSaveContext context, ISaveBuffer buffer);

    /// <summary>
    ///     Gets the base size of this structure when it's serialized to a buffer.
    /// </summary>
    /// <returns>Base size of this structure</returns>
    int GetAllocatedSize();
}