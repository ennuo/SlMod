using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that contains texture sample settings.
/// </summary>
public class SlSampler : ISumoResource, IWritable
{
    /// <summary>
    ///     Sampler state flags
    /// </summary>
    public int Flags;

    /// <summary>
    ///     Index of sampler.
    /// </summary>
    public int Index;

    /// <summary>
    ///     Texture used in this sampler.
    /// </summary>
    public SlResPtr<SlTexture> Texture = SlResPtr<SlTexture>.Empty();

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);
        Texture = context.LoadResource<SlTexture>(context.ReadInt32(offset + 0xc));
        Index = context.ReadInt32(offset + 0x10);
        Flags = context.ReadInt32(offset + 0x14);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SaveObject(buffer, Header, 0x0);
        context.SaveResource(buffer, Texture, 0xc);
        context.WriteInt32(buffer, Index, 0x10);
        context.WriteInt32(buffer, Flags, 0x14);
        context.SavePointer(buffer, this, 0x1c);
    }

    /// <inheritdoc />
    public int GetAllocatedSize()
    {
        return 0x28;
    }
}