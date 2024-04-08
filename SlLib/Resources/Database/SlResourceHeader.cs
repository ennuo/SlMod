using SlLib.Serialization;

namespace SlLib.Resources.Database;

public class SlResourceHeader : ILoadable, IWritable
{
    /// <summary>
    ///     The unique identifier associated with this resource
    ///     <remarks>
    ///         This hash is often just the hash of the name.
    ///     </remarks>
    /// </summary>
    public int Id;

    /// <summary>
    ///     The name of this resource.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The reference count for this resource.
    ///     <remarks>
    ///         I don't use this in this library, but it's a field that gets serialized.
    ///     </remarks>
    /// </summary>
    public int Ref = 1;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Id = context.ReadInt32(offset);
        Name = context.ReadStringPointer(offset + 4);
        Ref = context.ReadInt32(offset + 8);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Id, 0);
        context.WriteStringPointer(buffer, Name, 0x4);
        context.WriteInt32(buffer, Ref, 0x8);
    }

    /// <inheritdoc />
    public int GetAllocatedSize()
    {
        return 0xc;
    }
}