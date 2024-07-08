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

    /// <summary>
    ///     LOD bias for mip maps
    /// </summary>
    public int MipMapLodBias;
    
    
    // Texture Filter
    // 0x0 = GL_NEAREST
    // 0x1 = GL_LINEAR
    // 0x2 = GL_NEAREST_MIPMAP_NEAREST
    // 0x3 = GL_LINEAR_MIPMAP_NEAREST
    // 0x4 = GL_NEAREST_MIPMAP_LINEAR
    // 0x5 = GL_LINEAR_MIPMAP_LINEAR
    
    // Texture Address
    // 0x0 = GL_REPEAT
    // 0x1 = GL_MIRRORED_REPEAT
    // 0x2 = GL_CLAMP_TO_EDGE
    
    // Aniso
    // 0x0 = 1.0
    // 0x1 = 2.0
    
    // Flags
    // (flags & 7) -> (flags & 7) -> D3DSAMP_ADDRESSU
    // (flags & 0x38) -> (flags >> 3 & 7) = D3DSAMP_ADDRESSV
    // (flags & 0x1c0) -> (flags >> 6 & 7) = D3DSAMP_ADDRESSW
    // (flags * 0x600) -> (flags >> 9 & 3) = D3DSAMP_MINFILTER
    // (flags & 0x1800) -> (flags >> 0xb & 3) = D3DSAMP_MAGFILTER
    // (flags & 0x6000) -> (flags >> 0xd & 3) = D3DSAMP_MIPFILTER
    // (flags & 0x38000) -> (flags >> 0xf & 7) = D3DSAMP_MAXANISOTROPY
    // (flags & 0x40000) -> (flags >> 0x12 & 1) = D3DSAMP_SRGBTEXTURE
    
    public bool HasTextureData()
    {
        return Texture.Instance?.HasData() ?? false;
    }
    
    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        Texture = context.LoadResourcePointer<SlTexture>();
        Index = context.ReadInt32();
        Flags = context.ReadInt32();
        MipMapLodBias = context.ReadInt32();
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SaveObject(buffer, Header, 0x0);
        context.SaveResource(buffer, Texture, 0xc);
        context.WriteInt32(buffer, Index, 0x10);
        context.WriteInt32(buffer, Flags, 0x14);
        context.WriteInt32(buffer, MipMapLodBias, 0x18);
        context.SavePointer(buffer, this, 0x1c);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform.Is64Bit) return 0x40;
        return platform == SlPlatform.WiiU ? 0x30 : 0x28;
    }
}