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

    private static byte ComputeNormalZ(byte x, byte y)
    {
        float nx  = 2 * (x / 255.0f) - 1;
        float ny  = 2 * (y / 255.0f) - 1;
        float nz  = 0.0f;
        float nz2 = 1 - nx * nx - ny * ny;
        if (nz2 > 0) {
            nz = (float)Math.Sqrt(nz2);
        }
        int z = (int)(255.0f * (nz + 1) / 2.0f);
        if (z < 0)
            z = 0;
        if (z > 255)
            z = 255;
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
    /// <returns>Whether or not the DDS file converted successfully</returns>
    public static bool ToImage(ReadOnlySpan<byte> data, DXGI_FORMAT format, bool isNormalMap, int width, int height,
        out Image<Rgba32>? image)
    {
        image = default;

        byte[]? pixelData = ConvertToRgba32(data, format, width, height);
        if (pixelData == null) return false;

        if (isNormalMap)
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

        image = SixLabors.ImageSharp.Image.LoadPixelData<Rgba32>(pixelData, width, height);
        return true;
    }

    /// <summary>
    ///     Decodes a DDS file in-memory to an RGBA32 image.
    /// </summary>
    /// <param name="dds">DDS file buffer</param>
    /// <param name="image">Resulting image</param>
    /// <returns>Whether or not the DDS file converted successfully</returns>
    public static bool ToImage(ReadOnlySpan<byte> dds, out Image<Rgba32>? image)
    {
        image = default;

        // A DDS file header is 0x80 bytes, make sure we can at least read that much.
        if (dds.Length < 0x80) return false;
        
        
        TexMetadata metadata = GetTextureInformation(dds);
        DXGI_FORMAT format = metadata.Format;

        bool isNormalMap = format == DXGI_FORMAT.BC3_UNORM && (BinaryPrimitives.ReadInt32LittleEndian(dds[0x50..0x54]) & 0x80000000) != 0;
        
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
}