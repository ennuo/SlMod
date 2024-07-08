using OpenTK.Graphics.OpenGL;
using PixelType = OpenTK.Graphics.OpenGL.PixelType;

namespace SeEditor.OpenGL;

public static class SlTexturePlatformInfo
{
    // 32 types, each entry is 0x14 bytes
    public class TextureInfo
    {
        public PixelInternalFormat InternalFormat;
        public PixelFormat Format;
        public PixelType Type;
        public int Stride;

        public bool IsCompressedType()
        {
            return InternalFormat
                is PixelInternalFormat.CompressedRgbaS3tcDxt1Ext or
                PixelInternalFormat.CompressedRgbaS3tcDxt3Ext
                or PixelInternalFormat.CompressedRgbaS3tcDxt5Ext;
        }

        public bool IsValid()
        {
            return InternalFormat != 0;
        }
    }

    public static readonly TextureInfo[] Info =
    [
        new TextureInfo(), // None
        new TextureInfo // Argb32
        {
            InternalFormat = PixelInternalFormat.Rgba,
            Format = PixelFormat.Bgra,
            Type = PixelType.UnsignedByte,
            Stride = 32
        },
        new TextureInfo // L8
        {
            InternalFormat = PixelInternalFormat.Luminance,
            Format = PixelFormat.Luminance,
            Type = PixelType.UnsignedByte,
            Stride = 8
        },
        new TextureInfo(), // G16R16
        new TextureInfo // BC1
        {
            InternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt1Ext,
            Format = PixelFormat.Rgb,
            Type = PixelType.UnsignedByte,
            Stride = 4
        },
        new TextureInfo // BC2
        {
            InternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt3Ext,
            Format = PixelFormat.Rgba,
            Type = PixelType.UnsignedByte,
            Stride = 8
        },
        new TextureInfo // BC3
        {
            InternalFormat = PixelInternalFormat.CompressedRgbaS3tcDxt5Ext,
            Format = PixelFormat.Rgba,
            Type = PixelType.UnsignedByte,
            Stride = 8
        },
        new TextureInfo // 7
        {
            InternalFormat = PixelInternalFormat.DepthComponent16Sgix,
            Format = PixelFormat.DepthComponent,
            Type = PixelType.UnsignedShort,
            Stride = 16
        },
        new TextureInfo(), // 8
        new TextureInfo(), // 9
        new TextureInfo(), // 10
        new TextureInfo(), // 11
        new TextureInfo(), // 12
        new TextureInfo // A16FB16FG16FR16F
        {
            InternalFormat = PixelInternalFormat.Rgba,
            Format = PixelFormat.Bgra,
            Type = PixelType.HalfFloat,
            Stride = 64
        },
        new TextureInfo // 14
        {
            InternalFormat = PixelInternalFormat.Rgba,
            Format = PixelFormat.Bgra,
            Type = PixelType.HalfFloat,
            Stride = 64
        },
        new TextureInfo(), // R32F
        new TextureInfo(), // G32FR32F
        new TextureInfo // A32FB32FG32FR32F
        {
            InternalFormat = PixelInternalFormat.Rgba,
            Format = PixelFormat.Bgra,
            Type = PixelType.Float,
            Stride = 128
        },
    ];


}