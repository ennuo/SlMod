using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources;

/// <summary>
///     Texture data resource.
/// </summary>
public class SlTexture : ISumoResource, IWritable
{
    /// <summary>
    ///     Texture data buffer.
    /// </summary>
    public ArraySegment<byte> Data;

    /// <summary>
    ///     Texture format type.
    /// </summary>
    public int Format;

    /// <summary>
    ///     Texture height in pixels.
    /// </summary>
    public int Height;

    /// <summary>
    ///     Number of mips in the texture.
    /// </summary>
    public int Mips;

    /// <summary>
    ///     Texture width in pixels.
    /// </summary>
    public int Width;

    /// <inheritdoc />
    public SlResourceHeader Header { get; set; }

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        Header = context.LoadObject<SlResourceHeader>(offset);

        Width = context.ReadInt32(offset + 16);
        Height = context.ReadInt32(offset + 20);
        Format = context.ReadInt32(offset + 28);
        Mips = context.ReadInt32(offset + 32);

        int textureDataSize = context.ReadInt32(offset + 88);
        Data = context.LoadBufferPointer(offset + 64, textureDataSize, out _);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Width, 0xc);
        context.WriteInt32(buffer, Height, 0x10);
        context.WriteInt32(buffer, Format, 0x18);
        context.WriteInt32(buffer, Mips, 0x1c);
        context.SavePointer(buffer, this, 0x24);
        context.WriteInt32(buffer, Data.Count, 0x3c);
        context.SaveBufferPointer(buffer, Data, 0x40, 0x80, true);
        context.SaveObject(buffer, Header, 0x0);
    }

    /// <inheritdoc />
    public int GetAllocatedSize()
    {
        return 0x44;
    }
}