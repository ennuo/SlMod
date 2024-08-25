using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Image = DirectXTexNet.Image;

namespace SlLib.Utilities;

public static class DdsUtil
{
    public const int DDSFOURCC = 0x00000004;  // DDPFFOURCC
    public const int DDSRGB = 0x00000040;  // DDPFRGB
    public const int DDSRGBA = 0x00000041;  // DDPFRGB | DDPFALPHAPIXELS
    public const int DDSLUMINANCE = 0x00020000;  // DDPFLUMINANCE
    public const int DDSLUMINANCEA = 0x00020001;  // DDPFLUMINANCE | DDPFALPHAPIXELS
    public const int DDSALPHAPIXELS = 0x00000001;  // DDPFALPHAPIXELS
    public const int DDSALPHA = 0x00000002;  // DDPFALPHA
    public const int DDSPAL8 = 0x00000020;  // DDPFPALETTEINDEXED8
    public const int DDSPAL8A = 0x00000021;  // DDPFPALETTEINDEXED8 | DDPFALPHAPIXELS
    public const int DDSBUMPDUDV = 0x00080000;  // DDPFBUMPDUDV
    
    [Flags]
    public enum HeaderFlags : int
    {
        TEXTURE = 0x00001007,  // DDSDCAPS | DDSDHEIGHT | DDSDWIDTH | DDSDPIXELFORMAT 
        MIPMAP = 0x00020000,  // DDSDMIPMAPCOUNT
        VOLUME = 0x00800000,  // DDSDDEPTH
        PITCH = 0x00000008,  // DDSDPITCH
        LINEARSIZE = 0x00080000,  // DDSDLINEARSIZE
    }

    /// <summary>
    /// DDS Surface Flags
    /// </summary>
    public enum SurfaceFlags : int
    {
        TEXTURE = 0x00001000, // DDSCAPSTEXTURE
        MIPMAP = 0x00400008, // DDSCAPSCOMPLEX | DDSCAPSMIPMAP
        CUBEMAP = 0x00000008, // DDSCAPSCOMPLEX
    }
    
    public const int DDSMagic = 0x20534444;
    
    public static DDS_PIXELFORMAT DXT1 = new(DDSFOURCC, MakePixelFormatFourCC('D', 'X', 'T', '1'), 0, 0, 0, 0, 0);
    public static DDS_PIXELFORMAT DXT3 = new(DDSFOURCC, MakePixelFormatFourCC('D', 'X', 'T', '3'), 0, 0, 0, 0, 0);
    public static DDS_PIXELFORMAT DXT5 = new(DDSFOURCC, MakePixelFormatFourCC('D', 'X', 'T', '5'), 0, 0, 0, 0, 0);
    public static DDS_PIXELFORMAT ATI1 = new(DDSFOURCC, MakePixelFormatFourCC('A', 'T', 'I', '1'), 0, 0, 0, 0, 0);
    public static DDS_PIXELFORMAT ATI2 = new(DDSFOURCC, MakePixelFormatFourCC('A', 'T', 'I', '2'), 0, 0, 0, 0, 0);
    
    public struct DDS_PIXELFORMAT
    {
        public int Size;
        public int Flags;
        public int FourCC;
        public int RGBBitCount;
        public int RBitMask;
        public int GBitMask;
        public int BBitMask;
        public int ABitMask;
        
        /// <summary>
        /// Creates a new DDS Pixel Format
        /// </summary>
        public DDS_PIXELFORMAT(int flags, int fourCC, int rgbBitCount, int rBitMask, int gBitMask, int bBitMask, int aBitMask)
        {
            Size = Marshal.SizeOf<DDS_PIXELFORMAT>();
            Flags = flags;
            FourCC = fourCC;
            RGBBitCount = rgbBitCount;
            RBitMask = rBitMask;
            GBitMask = gBitMask;
            BBitMask = bBitMask;
            ABitMask = aBitMask;
        }
    }

    [InlineArray(11)]
    public struct DDS_RESERVED
    {
        private int _value;
    }
    
    public struct DDS_HEADER
    {
        public int Size;
        public HeaderFlags Flags;
        public int Height;
        public int Width;
        public int PitchOrLinearSize;
        public int Depth;
        public int MipMapCount;
        public DDS_RESERVED Reserved1;
        public DDS_PIXELFORMAT PixelFormat;
        public int Caps;
        public int Caps2;
        public int Caps3;
        public int Capts4;
        public int Reserved2;
    };

    /// <summary>
    /// Generates a FourCC Integer from Pixel Format Characters
    /// </summary>
    private static int MakePixelFormatFourCC(char char1, char char2, char char3, char char4)
    {
        return Convert.ToByte(char1) | (int)Convert.ToByte(char2) << 8 | (int)Convert.ToByte(char3) << 16 | (int)Convert.ToByte(char4) << 24;
    }
    
    public static DDS_HEADER GenerateHeader(TexMetadata metadata)
    {
        var header = new DDS_HEADER
        {
            Size = Marshal.SizeOf<DDS_HEADER>(),
            Flags = HeaderFlags.TEXTURE,
            Caps = (int)SurfaceFlags.TEXTURE,
            PixelFormat = new DDS_PIXELFORMAT(0, 0, 0, 0, 0, 0, 0)
        };

        header.PixelFormat = metadata.Format switch
        {
            DXGI_FORMAT.BC1_UNORM_SRGB => DXT1,
            DXGI_FORMAT.BC1_UNORM => DXT1,
            DXGI_FORMAT.BC2_UNORM_SRGB => DXT3,
            DXGI_FORMAT.BC2_UNORM => DXT3,
            DXGI_FORMAT.BC3_UNORM_SRGB => DXT5,
            DXGI_FORMAT.BC3_UNORM => DXT5,
            DXGI_FORMAT.BC4_UNORM => ATI1,
            DXGI_FORMAT.BC5_UNORM => ATI2,
            _ => throw new Exception("Invalid pixel format!")
        };

        if (metadata.MipLevels > 0)
        {
            header.Flags |= HeaderFlags.MIPMAP;
            header.MipMapCount = metadata.MipLevels;
            if (header.MipMapCount > 1)
                header.Caps |= (int)SurfaceFlags.MIPMAP;
        }

        switch (metadata.Dimension)
        {
            case TEX_DIMENSION.TEXTURE2D:
            {
                header.Width = metadata.Width;
                header.Height = metadata.Height;
                header.Depth = 1;
                break;
            }
            case TEX_DIMENSION.TEXTURE3D:
            {
                header.Flags |= HeaderFlags.VOLUME;
                header.Caps2 |= 0x00200000;
                header.Width = metadata.Width;
                header.Height = metadata.Height;
                header.Depth = metadata.Depth;
                break;
            }
        }
        
        TexHelper.Instance.ComputePitch(metadata.Format, metadata.Width, metadata.Height, out long rowPitch, out long slicePitch, CP_FLAGS.NONE);
        if (TexHelper.Instance.IsCompressed(metadata.Format))
        {
            header.Flags |= HeaderFlags.LINEARSIZE;
            header.PitchOrLinearSize = (int)slicePitch;
        }
        else
        {
            header.Flags |= HeaderFlags.PITCH;
            header.PitchOrLinearSize = (int)rowPitch;
        }

        return header;
    }
    
    /// <summary>
    ///     Parses texture metadata from a DDS header.
    /// </summary>
    /// <param name="dds">DDS file buffer</param>
    /// <returns>Parsed texture metadata</returns>
    public static unsafe TexMetadata GetTextureInformation(ReadOnlySpan<byte> dds)
    {
        fixed (byte* buf = dds)
        {
            return TexHelper.Instance.GetMetadataFromDDSMemory((IntPtr)buf, dds.Length, DDS_FLAGS.NONE);
        }
    }

    /// <summary>
    ///     Decodes DDS image data to RGBA32 pixel data.
    /// </summary>
    /// <param name="data">Data to decode</param>
    /// <param name="format">Input image format</param>
    /// <param name="width">Width of image in pixels</param>
    /// <param name="height">Height of image in pixels</param>
    /// <returns>RGBA32 pixel data</returns>
    private static unsafe byte[]? ConvertToRgba32(ReadOnlySpan<byte> data, DXGI_FORMAT format, int width, int height)
    {
        // Compute the pitch of the first mip map, so we can make sure there's enough data in the mipmap.
        TexHelper.Instance.ComputePitch(format, width, height, out long rowPitch, out long slicePitch, CP_FLAGS.NONE);
        if (data.Length < slicePitch) return null;

        // R8G8B8A8 is 32 bits per pixel
        byte[] result = new byte[width * height * 4];

        // If it's already in the target format, just return the data.
        if (format == DXGI_FORMAT.R8G8B8A8_UNORM)
        {
            data.CopyTo(result);
            return result;
        }

        var targetFormat = DXGI_FORMAT.R8G8B8A8_UNORM;
        bool isCompressed = TexHelper.Instance.IsCompressed(format);
        bool isSrgb = TexHelper.Instance.IsSRGB(format);

        fixed (byte* buf = data)
        {
            var image = new Image(width, height, format, rowPitch, slicePitch, (IntPtr)buf, null);
            var metadata = new TexMetadata(width, height, 1, 1, 1, 0, 0, format, TEX_DIMENSION.TEXTURE2D);
            ScratchImage? scratchImage = TexHelper.Instance.InitializeTemporary([image], metadata, null);

            if (isCompressed)
            {
                if (isSrgb) targetFormat = DXGI_FORMAT.R8G8B8A8_UNORM_SRGB;

                using ScratchImage? decompressed = scratchImage.Decompress(0, targetFormat);
                Marshal.Copy(decompressed.GetImage(0).Pixels, result, 0, result.Length);

                return result;
            }

            var filterFlags = TEX_FILTER_FLAGS.DEFAULT;
            if (isSrgb) filterFlags |= TEX_FILTER_FLAGS.SRGB;

            using ScratchImage? converted = scratchImage.Convert(0, targetFormat, filterFlags, 0.5f);
            Marshal.Copy(converted.GetImage(0).Pixels, result, 0, result.Length);
            return result;
        }
    }
    
    /// <summary>
    ///     Computes the Z direction for a normal map given the X and Y.
    /// </summary>
    /// <param name="x">X direction</param>
    /// <param name="y">Y direction</param>
    /// <returns>Z direction</returns>
    public static byte ComputeNormalZ(byte x, byte y)
    {
        float nx  = 2.0f * (x / 255.0f) - 1.0f;
        float ny  = 2.0f * (y / 255.0f) - 1.0f;
        float nz  = 0.0f;
        float d = 1.0f - nx * nx + ny * ny;
        
        if (d > 0) nz = (float)Math.Sqrt(d);
        
        int z = (int)(255.0f * (nz + 1) / 2.0f);
        z = Math.Max(0, Math.Min(255, z));
        
        return (byte)z;
    }

    /// <summary>
    ///     Decodes DDS pixel data to an RGBA32 image.
    /// </summary>
    /// <param name="data">Data to decode</param>
    /// <param name="format">Image format</param>
    /// <param name="isNormalMap">Whether or not the image is a tangent-space normal map, only applies for BC3 images</param>
    /// <param name="width">Width of image in pixels</param>
    /// <param name="height">Height of image in pixels</param>
    /// <param name="image">Resulting image</param>
    /// <returns>Whether the DDS file converted successfully</returns>
    public static bool ToImage(ReadOnlySpan<byte> data, DXGI_FORMAT format, bool isNormalMap, int width, int height,
        out Image<Rgba32>? image)
    {
        image = default;

        byte[]? pixelData = ConvertToRgba32(data, format, width, height);
        if (pixelData == null) return false;

        if (isNormalMap && format == DXGI_FORMAT.BC3_UNORM)
        {
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                byte x = pixelData[i + 3];
                byte y = pixelData[i + 1];
                pixelData[i + 0] = x;
                pixelData[i + 1] = y;
                pixelData[i + 2] = ComputeNormalZ(x, y);
                pixelData[i + 3] = 255;
            }  
        }
        
        if (format == DXGI_FORMAT.BC5_UNORM)
        {
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                byte x = pixelData[i + 0];
                byte y = pixelData[i + 1];
                pixelData[i + 2] = ComputeNormalZ(x, y);
                pixelData[i + 3] = 255;
            }   
        }

        image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(pixelData, width, height);
        return true;
    }

    /// <summary>
    ///     Decodes a DDS file in-memory to an RGBA32 image.
    /// </summary>
    /// <param name="dds">DDS file buffer</param>
    /// <param name="image">Resulting image</param>
    /// <returns>Whether the DDS file converted successfully</returns>
    public static bool ToImage(ReadOnlySpan<byte> dds, out Image<Rgba32>? image)
    {
        image = default;

        // A DDS file header is 0x80 bytes, make sure we can at least read that much.
        if (dds.Length < 0x80) return false;
        
        
        TexMetadata metadata = GetTextureInformation(dds);
        DXGI_FORMAT format = metadata.Format;
        
        bool isNormalMap = (format == DXGI_FORMAT.BC3_UNORM &&
                            (BinaryPrimitives.ReadInt32LittleEndian(dds[0x50..0x54]) & 0x80000000) != 0);
        
        // Make sure we're reading a valid DDS file format.
        if (!TexHelper.Instance.IsValid(format)) return false;

        int width = metadata.Width;
        int height = metadata.Height;

        return ToImage(dds[0x80..], format, isNormalMap, width, height, out image);
    }

    /// <summary>
    ///     Converts an image to a BC DDS file.
    /// </summary>
    /// <param name="image">Image to encode</param>
    /// <param name="format">Target compressed format</param>
    /// <param name="generateMips">Whether or not to generate mipmaps</param>
    /// <param name="isNormalTexture">Whether or not the image data represents a normal texture</param>
    /// <returns>DDS file buffer</returns>
    /// <exception cref="ArgumentException">If a non-compressed DXGI format is specified</exception>
    public static byte[] ToDds(Image<Rgba32> image, DXGI_FORMAT format, bool generateMips = false, bool isNormalTexture = false)
    {
        if (!TexHelper.Instance.IsCompressed(format))
            throw new ArgumentException("Only BC formats are supported!");

        byte[] pixelData = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
        image.CopyPixelDataTo(pixelData);
        
        if (isNormalTexture && format == DXGI_FORMAT.BC3_UNORM)
        {
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                pixelData[i + 3] = pixelData[i + 0];
                //pixelData[i + 1] = (byte)(0xff - pixelData[i + 1]);
                pixelData[i + 0] = 0xFF;
                pixelData[i + 2] = 0;
            }
        }
        
        byte[] imageData = CompressBlock(pixelData, image.Width, image.Height, format, generateMips);
        if (isNormalTexture) imageData[0x53] = 0x80;
        
        return imageData;
    }
    
    public static unsafe byte[] CompleteFileHeader(ReadOnlySpan<byte> data, DXGI_FORMAT format, int width, int height, int mips)
    {
        fixed (byte* buffer = data)
        {
            TexHelper.Instance.ComputePitch(format, width, height, out long rowPitch, out long slicePitch, CP_FLAGS.NONE);
            var image = new Image(width, height, format, rowPitch, slicePitch, (IntPtr)buffer,
                null);
            var metadata = new TexMetadata(width, height, 1, 1, mips, 0, 0, format, TEX_DIMENSION.TEXTURE2D);
            
            ScratchImage scratchImage = TexHelper.Instance.InitializeTemporary([image], metadata, null);
            using UnmanagedMemoryStream? file = scratchImage.SaveToDDSMemory(0, DDS_FLAGS.NONE);
            byte[] result = new byte[file.Length];
            file.ReadExactly(result);
            return result;
        }
    }
    
    private static unsafe byte[] CompressBlock(byte[] data, int width, int height, DXGI_FORMAT format, bool generateMips)
    {
        long rowPitch = width * 4;
        long slicePitch = width * height * 4;
        fixed (byte* buffer = data)
        {
            var image = new Image(width, height, DXGI_FORMAT.R8G8B8A8_UNORM, rowPitch, slicePitch, (IntPtr)buffer,
                null);
            var metadata = new TexMetadata(width, height, 1, 1, 1, 0, 0, DXGI_FORMAT.R8G8B8A8_UNORM,
                TEX_DIMENSION.TEXTURE2D);
            ScratchImage scratchImage = TexHelper.Instance.InitializeTemporary([image], metadata, null);
            
            var flags = TEX_COMPRESS_FLAGS.DEFAULT;
            if (TexHelper.Instance.IsSRGB(format))
                flags |= TEX_COMPRESS_FLAGS.SRGB;
            
            byte[] result;
            if (generateMips)
            {
                using ScratchImage? mipImage = scratchImage.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, 0);
                using ScratchImage? compressedMipImage = mipImage.Compress(format, flags, 0.5f);
                using UnmanagedMemoryStream? mipFile = compressedMipImage.SaveToDDSMemory(DDS_FLAGS.NONE);
                result = new byte[mipFile.Length];
                mipFile.ReadExactly(result);
                return result;
            }
            
            using ScratchImage? compressed = scratchImage.Compress(format, flags, 0.5f);
            using UnmanagedMemoryStream? file = compressed.SaveToDDSMemory(DDS_FLAGS.NONE);
            result = new byte[file.Length];
            file.ReadExactly(result);
            return result;
        }
    }
    
    public static unsafe byte[] DoShittyConvertTexture(DXGI_FORMAT target, DXGI_FORMAT format, byte[] data, int width, int height, bool isNormalTexture)
    {
        TexHelper.Instance.ComputePitch(format, width, height, out long rowPitch, out long slicePitch, CP_FLAGS.NONE);
        fixed (byte* buffer = data)
        {
            var image = new Image(width, height, format, rowPitch, slicePitch, (IntPtr)buffer,
                null);
            var metadata = new TexMetadata(width, height, 1, 1, 1, 0, 0, format,
                TEX_DIMENSION.TEXTURE2D);
            ScratchImage scratchImage = TexHelper.Instance.InitializeTemporary([image], metadata, null);

            var flags = TEX_COMPRESS_FLAGS.DEFAULT;
            if (TexHelper.Instance.IsSRGB(format))
                flags |= TEX_COMPRESS_FLAGS.SRGB;

            scratchImage = scratchImage.Decompress(0, DXGI_FORMAT.R8G8B8A8_UNORM);
            if (isNormalTexture && format == DXGI_FORMAT.BC5_SNORM)
            {
                byte* pixels = (byte*)scratchImage.GetImage(0).Pixels;
                for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)
                {
                    int offset = ((y * width) + x) * 4;
                    pixels[offset + 3] = pixels[offset + 0];
                    pixels[offset + 0] = 0xFF;
                    pixels[offset + 2] = 0x0;
                }
            }
            
            using ScratchImage? mipImage = scratchImage.GenerateMipMaps(TEX_FILTER_FLAGS.DEFAULT, 0);
            using ScratchImage? compressedMipImage = mipImage.Compress(target, flags, 0.5f);
            using UnmanagedMemoryStream? mipFile = compressedMipImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            byte[] result = new byte[mipFile.Length];
            mipFile.ReadExactly(result);
            if (isNormalTexture) result[0x53] = 0x80;
            
            return result;
        }
    }
}