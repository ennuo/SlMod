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

    /// <summary>
    ///     Decodes DDS pixel data to an RGBA32 image.
    /// </summary>
    /// <param name="data">Data to decode</param>
    /// <param name="format">Image format</param>
    /// <param name="width">Width of image in pixels</param>
    /// <param name="height">Height of image in pixels</param>
    /// <param name="image">Resulting image</param>
    /// <returns>Whether or not the DDS file converted successfully</returns>
    public static bool ToImage(ReadOnlySpan<byte> data, DXGI_FORMAT format, int width, int height,
        out Image<Rgba32>? image)
    {
        image = default;

        byte[]? pixelData = ConvertToRgba32(data, format, width, height);
        if (pixelData == null) return false;

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

        // Make sure we're reading a valid DDS file format.
        if (!TexHelper.Instance.IsValid(format)) return false;

        int width = metadata.Width;
        int height = metadata.Height;

        return ToImage(dds[0x80..], format, width, height, out image);
    }

    /// <summary>
    ///     Converts an image to a BC DDS file.
    /// </summary>
    /// <param name="image">Image to encode</param>
    /// <param name="format">Target compressed format</param>
    /// <returns>DDS file buffer</returns>
    /// <exception cref="ArgumentException">If a non-compressed DXGI format is specified</exception>
    public static byte[] ToDds(Image<Rgba32> image, DXGI_FORMAT format)
    {
        if (!TexHelper.Instance.IsCompressed(format))
            throw new ArgumentException("Only BC formats are supported!");

        byte[] pixelData = new byte[image.Width * image.Height * Unsafe.SizeOf<Rgba32>()];
        image.CopyPixelDataTo(pixelData);
        return CompressBlock(pixelData, image.Width, image.Height, format);
    }

    private static unsafe byte[] CompressBlock(byte[] data, int width, int height, DXGI_FORMAT format)
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

            using ScratchImage? compressed = scratchImage.Compress(format, flags, 0.5f);
            using UnmanagedMemoryStream? file = compressed.SaveToDDSMemory(DDS_FLAGS.NONE);
            byte[] result = new byte[file.Length];
            file.ReadExactly(result);
            return result;
        }
    }
}