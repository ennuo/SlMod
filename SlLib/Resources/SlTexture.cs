using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources;

/// <summary>
///     Texture data resource.
/// </summary>
public class SlTexture : ISumoResource
{
    public enum SlTextureType
    {
        None = 0,
        Argb32 = 1,
        L8 = 2,
        G16R16 = 3,
        Bc1 = 4,
        Bc2 = 5,
        Bc3 = 6,
        R16F = 11,
        G16FR16F = 12,
        A16FB16FG16FR16F = 13,
        R32F = 15,
        G32FR32F = 16,
        A32FB32FG32FR32F = 17,
        
        ATI2XY = 31,
    }
    
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     Texture width in pixels.
    /// </summary>
    public int Width;

    /// <summary>
    ///     Texture height in pixels.
    /// </summary>
    public int Height;

    /// <summary>
    ///     Texture format type.
    /// </summary>
    public SlTextureType Format = SlTextureType.Bc3;

    /// <summary>
    ///     Number of mips in the texture.
    /// </summary>
    public int Mips;

    /// <summary>
    ///     Texture data buffer.
    /// </summary>
    public ArraySegment<byte> Data;

    /// <summary>
    ///     Texture buffer ID for OpenGL
    /// </summary>
    public int ID;

    /// <summary>
    ///     Whether the texture data contains a cubemap.
    /// </summary>
    public bool Cubemap;

    public SlTexture(string name, Image<Rgba32> image, bool isNormalTexture = false)
    {
        Header.SetName(name);
        SetImage(image, isNormalTexture);
    }
    
    // Empty constructor for serialization
    public SlTexture()
    {
        
    }

    public bool HasData()
    {
        return Data.Count != 0;
    }
    
    public void SetImage(Image<Rgba32> image, bool isNormalTexture = false)
    {
        Data = DdsUtil.ToDds(image, DXGI_FORMAT.BC3_UNORM, generateMips: true, isNormalTexture);
        Width = image.Width;
        Height = image.Height;
        Format = SlTextureType.Bc3;
        Mips = Data[0x1c];
    }
    
    public Image<Rgba32> GetImage()
    {
        DdsUtil.ToImage(Data, out Image<Rgba32>? image);
        return image!;
    }
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        Width = context.ReadInt32();
        Height = context.ReadInt32();
        context.ReadInt32(); // Unknown, might be depth, mostly 0 because 2D textures?
        Format = (SlTextureType)context.ReadInt32();
        Mips = context.ReadInt32();
        Cubemap = context.ReadBoolean(wide: true);
        // From this point forward, it's now the platform resource
        
        // Skip the reference back to the texture.
        context.Position += context.Platform.GetPointerSize(); 
        // 0x28

        if (context.Platform == SlPlatform.Win64)
        {
            context.Position += 0x10;
            int textureData = context.ReadPointer(out bool isTextureDataFromGpu);
            context.Position += 0x10;
            int textureSize = context.ReadInt32();
            
            Data = context.LoadBuffer(textureData, textureSize, isTextureDataFromGpu);
        }
        else if (context.Platform == SlPlatform.Win32)
        {
            // Skip to texture data
            context.Position += 0x14;
            Data = context.LoadBufferPointer(context.ReadInt32(), out _);
        }
        else if (context.Platform == SlPlatform.WiiU)
        {
            bool isCompressedTexture = Format is SlTextureType.Bc1 or SlTextureType.Bc2 or SlTextureType.Bc3;
            if (!isCompressedTexture) return;

            var surface = new Gx2Util.Gx2Surface();
            surface.Load(context);

            int blockSize = Format == SlTextureType.Bc1 ? 8 : 16;

            try
            {
                Mips = 1;
                Data = surface.GetAsDDSFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

        }
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Width, 0xc);
        context.WriteInt32(buffer, Height, 0x10);
        context.WriteInt32(buffer, (int)Format, 0x18);
        context.WriteInt32(buffer, Mips, 0x1c);
        context.SavePointer(buffer, this, 0x24);
        context.WriteInt32(buffer, Data.Count, 0x3c);
        context.SaveBufferPointer(buffer, Data, 0x40, 0x80, true);
        context.SaveObject(buffer, Header, 0x0);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x44;
    }
}