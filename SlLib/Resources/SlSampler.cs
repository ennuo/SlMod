using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Resource that contains texture sample settings.
/// </summary>
public class SlSampler : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     Texture used in this sampler.
    /// </summary>
    public SlResPtr<SlTexture> Texture = new();

    /// <summary>
    ///     Index of sampler.
    /// </summary>
    public int Index;

    /// <summary>
    ///     Sampler state flags
    /// </summary>
    public int Flags;

    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        Texture = context.LoadResourcePointer<SlTexture>();
        Index = context.ReadInt32();
        Flags = context.ReadInt32();
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

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform.Is64Bit) return 0x40;
        return platform == SlPlatform.WiiU ? 0x30 : 0x28;
    }
}