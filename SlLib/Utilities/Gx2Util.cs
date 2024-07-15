using System.Runtime.CompilerServices;
using SlLib.Serialization;

namespace SlLib.Utilities;

// Most of this is pulled from https://github.com/KillzXGaming/Switch-Toolbox/blob/master/Switch_Toolbox_Library/Texture%20Decoding/Wii%20U/GX2.cs#L128
// since I know absolutely nothing about Wii U formats.
public class Gx2Util
{
    public struct Gx2Surface : IResourceSerializable
    {
        public int Dim;
        public int Width;
        public int Height;
        public int Depth;
        public int MipLevels;
        public Gx2SurfaceFormat Format;
        public int AA;
        public int ResourceFlags;
        public ArraySegment<byte> Image;
        public ArraySegment<byte> Mipmaps;
        public Gx2TileMode TileMode;
        public int Swizzle;
        public int Alignment;
        public int Pitch;
        public MipLevelOffsetArray MipLevelOffset;

        public void Load(ResourceLoadContext context)
        {
            Dim = context.ReadInt32();
            Width = context.ReadInt32();
            Height = context.ReadInt32();
            Depth = context.ReadInt32();
            MipLevels = context.ReadInt32();
            Format = (Gx2SurfaceFormat)context.ReadInt32();
            AA = context.ReadInt32();
            ResourceFlags = context.ReadInt32();
            Image = context.LoadBufferPointer(context.ReadInt32(), out _);
            Mipmaps = context.LoadBufferPointer(context.ReadInt32(), out _);
            TileMode = (Gx2TileMode)context.ReadInt32();
            Swizzle = context.ReadInt32();
            Alignment = context.ReadInt32();
            Pitch = context.ReadInt32();
            for (int i = 0; i < 13; ++i) 
                MipLevelOffset[i] = context.ReadInt32();
        }

        public byte[] GetAsDDSFile()
        {
            return new byte[0x80].Concat(GetUnswizzledLevel(0)).ToArray();
        }
        
        // Splice = 0
        // surfOut = GX2.getSurfaceInfo((GX2SurfaceFormat)Format, Width, Height, 1, 1, TileMode, 0, mipLevel);
        
        // byte[] SwizzledData = GX2.swizzle(width_, height_, surfOut.depth, surfOut.height, (uint)Format, 0, 1, surfOut.tileMode, s,
        // surfOut.pitch, surfOut.bpp, Splice, 0, data_);

        
        public byte[] GetUnswizzledLevel(int level = 0)
        {
            uint bitsPerPixel = FormatHwInfo[((int)Format & 0x3f) * 4];
            uint bytesPerPixel = bitsPerPixel / 8;

            int width = Math.Max(1, Width >> level);
            int height = Math.Max(1, Height >> level);
            
            if (IsFormatBCN(Format))
            {
                width = (width + 3) / 4;
                height = (height + 3) / 4;
            }

            int pitch = width;
            
            
            byte[] result = new byte[width * height * bytesPerPixel];
            
            ArraySegment<byte> data;
            if (level == 0) data = Image;
            else data = level == 1 ? Mipmaps : Mipmaps[MipLevelOffset[level - 1]..];
            
            // if (level != 0)
            //     Console.WriteLine(MipLevelOffset[level - 1]);
            
            uint pipeSwizzle = (uint)((Swizzle >>> 8) & 1);
            uint bankSwizzle = (uint)((Swizzle >>> 9) & 3);
            
            for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
            {
                
                // MODE_DEFAULT = 0x0,
                // MODE_LINEAR_SPECIAL = 0x10,
                // MODE_LINEAR_ALIGNED = 0x1,
                // MODE_1D_TILED_THIN1 = 0x2,
                // MODE_1D_TILED_THICK = 0x3,
                // MODE_2D_TILED_THIN1 = 0x4,

                ulong pos = TileMode switch
                {
                    Gx2TileMode.MODE_DEFAULT or Gx2TileMode.MODE_LINEAR_ALIGNED => ComputeSurfaceAddrFromCoordLinear(
                        (uint)x, (uint)y, 0, 0, bytesPerPixel, (uint)pitch, (uint)height, (uint)Depth),
                    Gx2TileMode.MODE_1D_TILED_THIN1 or Gx2TileMode.MODE_1D_TILED_THICK =>
                        ComputeSurfaceAddrFromCoordMicroTiled((uint)x, (uint)y, 0, (uint)bitsPerPixel,
                            (uint)pitch, (uint)height, (AddrTileMode)TileMode, false),
                    _ => ComputeSurfaceAddrFromCoordMacroTiled((uint)x, (uint)y, 0, 0, bitsPerPixel,
                        (uint)pitch, (uint)height, (uint)(1 << AA), (AddrTileMode)TileMode, false, pipeSwizzle,
                        bankSwizzle)
                };

                uint pos_ = (uint)(y * width + x) * bytesPerPixel;
                if (pos + bytesPerPixel <= (uint)data.Count && pos + bytesPerPixel <= (uint)data.Count)
                {
                    for (int n = 0; n < bytesPerPixel; ++n)
                        result[pos_ + n] = data[(int)((uint)pos + n)];
                }
            }

            return result;
        }
        
        [InlineArray(13)]
        public struct MipLevelOffsetArray
        {
            private int _element0;
        }
    }
    
    public static bool IsFormatBCN(Gx2SurfaceFormat format)
    {
        switch (format)
        {
            case Gx2SurfaceFormat.T_BC1_UNORM:
            case Gx2SurfaceFormat.T_BC1_SRGB:
            case Gx2SurfaceFormat.T_BC2_UNORM:
            case Gx2SurfaceFormat.T_BC2_SRGB:
            case Gx2SurfaceFormat.T_BC3_UNORM:
            case Gx2SurfaceFormat.T_BC3_SRGB:
            case Gx2SurfaceFormat.T_BC4_UNORM:
            case Gx2SurfaceFormat.T_BC4_SNORM:
            case Gx2SurfaceFormat.T_BC5_SNORM:
            case Gx2SurfaceFormat.T_BC5_UNORM:
                return true;
            default:
                return false;
        }
    }
    
    private static ulong ComputeSurfaceAddrFromCoordLinear(uint x, uint y, uint slice, uint sample, uint bpp, uint pitch, uint height, uint numSlices)
    {
        uint sliceOffset = pitch * height * (slice + sample * numSlices);
        return (y * pitch + x + sliceOffset) * bpp;
    }
    
    private static ulong ComputeSurfaceAddrFromCoordMicroTiled(uint x, uint y, uint slice, uint bpp, uint pitch, uint height, AddrTileMode tileMode, bool IsDepth)
    {
        int microTileThickness = 1;
        if (tileMode == AddrTileMode.ADDR_TM_1D_TILED_THICK)
            microTileThickness = 4;

        uint microTileBytes = (uint)(64 * microTileThickness * bpp + 7) / 8;
        uint microTilesPerRow = pitch >> 3;
        uint microTileIndexX = x >> 3;
        uint microTileIndexY = y >> 3;
        uint microTileIndexZ = slice / (uint)microTileThickness;

        ulong microTileOffset = microTileBytes * (microTileIndexX + microTileIndexY * microTilesPerRow);
        ulong sliceBytes = (ulong)(pitch * height * microTileThickness * bpp + 7) / 8;
        ulong sliceOffset = microTileIndexZ * sliceBytes;

        uint pixelIndex = ComputePixelIndexWithinMicroTile(x, y, slice, bpp, tileMode, IsDepth);
        ulong pixelOffset = (bpp * pixelIndex) >> 3;

        return pixelOffset + microTileOffset + sliceOffset;
    }
    
    private static ulong ComputeSurfaceAddrFromCoordMacroTiled(uint x, uint y, uint slice, uint sample, uint bpp, uint pitch, uint height,
                                                      uint numSamples, AddrTileMode tileMode, bool IsDepth, uint pipeSwizzle, uint bankSwizzle)
    {
        uint microTileThickness = ComputeSurfaceThickness(tileMode);

        uint microTileBits = numSamples * bpp * (microTileThickness * 64);
        uint microTileBytes = (microTileBits + 7) / 8;

        uint pixelIndex = ComputePixelIndexWithinMicroTile(x, y, slice, bpp, tileMode, IsDepth);
        uint bytesPerSample = microTileBytes / numSamples;
        uint sampleOffset = 0;
        uint pixelOffset = 0;
        uint samplesPerSlice = 0;
        uint numSampleSplits = 0;
        uint sampleSlice = 0;

        if (IsDepth)
        {
            sampleOffset = bpp * sample;
            pixelOffset = numSamples * bpp * pixelIndex;
        }
        else
        {
            sampleOffset = sample * (microTileBits / numSamples);
            pixelOffset = bpp * pixelIndex;
        }

        uint elemOffset = pixelOffset + sampleOffset;

        if (numSamples <= 1 || microTileBytes <= 2048)
        {
            samplesPerSlice = numSamples;
            numSampleSplits = 1;
            sampleSlice = 0;
        }
        else
        {
            samplesPerSlice = 2048 / bytesPerSample;
            numSampleSplits = numSamples / samplesPerSlice;
            numSamples = samplesPerSlice;

            uint tileSliceBits = microTileBits / numSampleSplits;
            sampleSlice = elemOffset / tileSliceBits;
            elemOffset %= tileSliceBits;
        }

        elemOffset = (elemOffset + 7) / 8;

        uint pipe = ComputePipeFromCoordWoRotation(x, y);
        uint bank = ComputeBankFromCoordWoRotation(x, y);

        uint swizzle_ = pipeSwizzle + 2 * bankSwizzle;
        uint bankPipe = pipe + 2 * bank;
        uint rotation = ComputeSurfaceRotationFromTileMode(tileMode);
        uint sliceIn = slice;

        if (IsThickMacroTiled(tileMode) != 0)
            sliceIn >>= 2;

        bankPipe ^= 2 * sampleSlice * 3 ^ (swizzle_ + sliceIn * rotation);
        bankPipe %= 8;

        pipe = bankPipe % 2;
        bank = bankPipe / 2;

        uint sliceBytes = (height * pitch * microTileThickness * bpp * numSamples + 7) / 8;
        uint sliceOffset = sliceBytes * (sampleSlice + numSampleSplits * slice) / microTileThickness;

        uint macroTilePitch = 32;
        uint macroTileHeight = 16;

        switch (tileMode)
        {
            case AddrTileMode.ADDR_TM_2D_TILED_THIN2:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN2:
                {
                    macroTilePitch = 16;
                    macroTileHeight = 32;
                    break;
                }

            case AddrTileMode.ADDR_TM_2D_TILED_THIN4:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN4:
                {
                    macroTilePitch = 8;
                    macroTileHeight = 64;
                    break;
                }
        }

        uint macroTilesPerRow = pitch / macroTilePitch;
        uint macroTileBytes = (numSamples * microTileThickness * bpp * macroTileHeight
                              * macroTilePitch + 7) / 8;
        uint macroTileIndexX = x / macroTilePitch;
        uint macroTileIndexY = y / macroTileHeight;
        ulong macroTileOffset = (macroTileIndexX + macroTilesPerRow * macroTileIndexY) * macroTileBytes;

        if (IsBankSwappedTileMode(tileMode) != 0)
        {
            uint bankSwapWidth = ComputeSurfaceBankSwappedWidth(tileMode, bpp, 1, pitch);
            uint swapIndex = macroTilePitch * macroTileIndexX / bankSwapWidth;
            bank ^= BankSwapOrder[swapIndex & 3];
        }

        ulong totalOffset = elemOffset + ((macroTileOffset + sliceOffset) >> 3);
        return bank << 9 | pipe << 8 | totalOffset & 255 | (ulong)((int)totalOffset & -256) << 3;
    }
    
    private static uint ComputePipeFromCoordWoRotation(uint x, uint y)
    {
        return ((y >> 3) ^ (x >> 3)) & 1;
    }
    
    private static uint ComputeBankFromCoordWoRotation(uint x, uint y)
    {
        return ((y >> 5) ^ (x >> 3)) & 1 | 2 * (((y >> 4) ^ (x >> 4)) & 1);
    }

    private static uint ComputeSurfaceRotationFromTileMode(AddrTileMode tileMode)
    {
        switch (tileMode)
        {
            case AddrTileMode.ADDR_TM_2D_TILED_THIN1:
            case AddrTileMode.ADDR_TM_2D_TILED_THIN2:
            case AddrTileMode.ADDR_TM_2D_TILED_THIN4:
            case AddrTileMode.ADDR_TM_2D_TILED_THICK:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN1:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN2:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN4:
            case AddrTileMode.ADDR_TM_2B_TILED_THICK:
                return 2;
            case AddrTileMode.ADDR_TM_3D_TILED_THIN1:
            case AddrTileMode.ADDR_TM_3D_TILED_THICK:
            case AddrTileMode.ADDR_TM_3B_TILED_THIN1:
            case AddrTileMode.ADDR_TM_3B_TILED_THICK:
                return 1;
            default:
                return 0;
        }
    }
    
    private static uint ComputeSurfaceBankSwappedWidth(AddrTileMode tileMode, uint bpp, uint numSamples, uint pitch)
    {
        if (IsBankSwappedTileMode(tileMode) == 0)
            return 0;

        uint bytesPerSample = 8 * bpp;
        uint samplesPerTile, slicesPerTile;

        if (bytesPerSample != 0)
        {
            samplesPerTile = 2048 / bytesPerSample;
            slicesPerTile = Math.Max(1, numSamples / samplesPerTile);
        }

        else
            slicesPerTile = 1;

        if (IsThickMacroTiled(tileMode) != 0)
            numSamples = 4;

        uint bytesPerTileSlice = numSamples * bytesPerSample / slicesPerTile;

        uint factor = ComputeMacroTileAspectRatio(tileMode);
        uint swapTiles = Math.Max(1, 128 / bpp);

        uint swapWidth = swapTiles * 32;
        uint heightBytes = numSamples * factor * bpp * 2 / slicesPerTile;
        uint swapMax = 0x4000 / heightBytes;
        uint swapMin = 256 / bytesPerTileSlice;

        uint bankSwapWidth = Math.Min(swapMax, Math.Max(swapMin, swapWidth));

        while (bankSwapWidth >= 2 * pitch)
            bankSwapWidth >>= 1;

        return bankSwapWidth;
    }
    
    private static uint ComputeMacroTileAspectRatio(AddrTileMode tileMode)
    {
        switch (tileMode)
        {
            case AddrTileMode.ADDR_TM_2D_TILED_THIN2:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN2:
                return 2;

            case AddrTileMode.ADDR_TM_2D_TILED_THIN4:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN4:
                return 4;

            default:
                return 1;
        }
    }
    
    private static uint ComputePixelIndexWithinMicroTile(uint x, uint y, uint z, uint bpp, AddrTileMode tileMode, bool IsDepth)
    {
        uint pixelBit0 = 0;
        uint pixelBit1 = 0;
        uint pixelBit2 = 0;
        uint pixelBit3 = 0;
        uint pixelBit4 = 0;
        uint pixelBit5 = 0;
        uint pixelBit6 = 0;
        uint pixelBit7 = 0;
        uint pixelBit8 = 0;

        uint thickness = ComputeSurfaceThickness(tileMode);

        if (IsDepth)    
        {
            pixelBit0 = x & 1;
            pixelBit1 = y & 1;
            pixelBit2 = (x & 2) >> 1;
            pixelBit3 = (y & 2) >> 1;
            pixelBit4 = (x & 4) >> 2;
            pixelBit5 = (y & 4) >> 2;
        }
        else
        {
            switch (bpp)
            {
                case 8:
                    pixelBit0 = x & 1;
                    pixelBit1 = (x & 2) >> 1;
                    pixelBit2 = (x & 4) >> 2;
                    pixelBit3 = (y & 2) >> 1;
                    pixelBit4 = y & 1;
                    pixelBit5 = (y & 4) >> 2;
                    break;
                case 0x10:
                    pixelBit0 = x & 1;
                    pixelBit1 = (x & 2) >> 1;
                    pixelBit2 = (x & 4) >> 2;
                    pixelBit3 = y & 1;
                    pixelBit4 = (y & 2) >> 1;
                    pixelBit5 = (y & 4) >> 2;
                    break;
                case 0x20:
                case 0x60:
                    pixelBit0 = x & 1;
                    pixelBit1 = (x & 2) >> 1;
                    pixelBit2 = y & 1;
                    pixelBit3 = (x & 4) >> 2;
                    pixelBit4 = (y & 2) >> 1;
                    pixelBit5 = (y & 4) >> 2;
                    break;
                case 0x40:
                    pixelBit0 = x & 1;
                    pixelBit1 = y & 1;
                    pixelBit2 = (x & 2) >> 1;
                    pixelBit3 = (x & 4) >> 2;
                    pixelBit4 = (y & 2) >> 1;
                    pixelBit5 = (y & 4) >> 2;
                    break;
                case 0x80:
                    pixelBit0 = y & 1;
                    pixelBit1 = x & 1;
                    pixelBit2 = (x & 2) >> 1;
                    pixelBit3 = (x & 4) >> 2;
                    pixelBit4 = (y & 2) >> 1;
                    pixelBit5 = (y & 4) >> 2;
                    break;
                default:
                    pixelBit0 = x & 1;
                    pixelBit1 = (x & 2) >> 1;
                    pixelBit2 = y & 1;
                    pixelBit3 = (x & 4) >> 2;
                    pixelBit4 = (y & 2) >> 1;
                    pixelBit5 = (y & 4) >> 2;
                    break;
            }
        }

        if (thickness > 1)
        {
            pixelBit6 = z & 1;
            pixelBit7 = (z & 2) >> 1;
        }

        if (thickness == 8)
            pixelBit8 = (z & 4) >> 2;

        return (pixelBit8 << 8) | (pixelBit7 << 7) | (pixelBit6 << 6) | 32 * pixelBit5 | 16 * pixelBit4 | 8 * pixelBit3 | 4 * pixelBit2 | pixelBit0 | 2 * pixelBit1;
    }
    
    private static uint ComputeSurfaceThickness(AddrTileMode tileMode)
    {
        switch (tileMode)
        {
            case AddrTileMode.ADDR_TM_1D_TILED_THICK:
            case AddrTileMode.ADDR_TM_2D_TILED_THICK:
            case AddrTileMode.ADDR_TM_2B_TILED_THICK:
            case AddrTileMode.ADDR_TM_3D_TILED_THICK:
            case AddrTileMode.ADDR_TM_3B_TILED_THICK:
                return 4;

            case AddrTileMode.ADDR_TM_2D_TILED_XTHICK:
            case AddrTileMode.ADDR_TM_3D_TILED_XTHICK:
                return 8;

            default:
                return 1;
        }
    }
    
    private static uint IsBankSwappedTileMode(AddrTileMode tileMode)
    {
        switch (tileMode)
        {
            case AddrTileMode.ADDR_TM_2B_TILED_THIN1:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN2:
            case AddrTileMode.ADDR_TM_2B_TILED_THIN4:
            case AddrTileMode.ADDR_TM_2B_TILED_THICK:
            case AddrTileMode.ADDR_TM_3B_TILED_THIN1:
            case AddrTileMode.ADDR_TM_3B_TILED_THICK:
                return 1;

            default:
                return 0;
        }
    }
        
    private static uint IsThickMacroTiled(AddrTileMode tileMode)
    {
        switch (tileMode)
        {
            case AddrTileMode.ADDR_TM_2D_TILED_THICK:
            case AddrTileMode.ADDR_TM_2B_TILED_THICK:
            case AddrTileMode.ADDR_TM_3D_TILED_THICK:
            case AddrTileMode.ADDR_TM_3B_TILED_THICK:
                return 1;

            default:
                return 0;
        }
    }
    
    private static byte[] BankSwapOrder = [ 0, 1, 3, 2, 6, 7, 5, 4, 0, 0 ];
    
    private static byte[] FormatHwInfo = 
    [
        0x00, 0x00, 0x00, 0x01, 0x08, 0x03, 0x00, 0x01, 0x08, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x01, 0x10, 0x07, 0x00, 0x00, 0x10, 0x03, 0x00, 0x01, 0x10, 0x03, 0x00, 0x01,
        0x10, 0x0B, 0x00, 0x01, 0x10, 0x01, 0x00, 0x01, 0x10, 0x03, 0x00, 0x01, 0x10, 0x03, 0x00, 0x01,
        0x10, 0x03, 0x00, 0x01, 0x20, 0x03, 0x00, 0x00, 0x20, 0x07, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00,
        0x20, 0x03, 0x00, 0x01, 0x20, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x20, 0x03, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x20, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
        0x00, 0x00, 0x00, 0x01, 0x20, 0x0B, 0x00, 0x01, 0x20, 0x0B, 0x00, 0x01, 0x20, 0x0B, 0x00, 0x01,
        0x40, 0x05, 0x00, 0x00, 0x40, 0x03, 0x00, 0x00, 0x40, 0x03, 0x00, 0x00, 0x40, 0x03, 0x00, 0x00,
        0x40, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x80, 0x03, 0x00, 0x00, 0x80, 0x03, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x10, 0x01, 0x00, 0x00,
        0x10, 0x01, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00, 0x20, 0x01, 0x00, 0x00,
        0x00, 0x01, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x60, 0x01, 0x00, 0x00,
        0x60, 0x01, 0x00, 0x00, 0x40, 0x01, 0x00, 0x01, 0x80, 0x01, 0x00, 0x01, 0x80, 0x01, 0x00, 0x01,
        0x40, 0x01, 0x00, 0x01, 0x80, 0x01, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
    ];
    
    public enum Gx2TileMode
    {
        MODE_DEFAULT = 0x0,
        MODE_LINEAR_SPECIAL = 0x10,
        MODE_LINEAR_ALIGNED = 0x1,
        MODE_1D_TILED_THIN1 = 0x2,
        MODE_1D_TILED_THICK = 0x3,
        MODE_2D_TILED_THIN1 = 0x4,
        MODE_2D_TILED_THIN2 = 0x5,
        MODE_2D_TILED_THIN4 = 0x6,
        MODE_2D_TILED_THICK = 0x7,
        MODE_2B_TILED_THIN1 = 0x8,
        MODE_2B_TILED_THIN2 = 0x9,
        MODE_2B_TILED_THIN4 = 0xA,
        MODE_2B_TILED_THICK = 0xB,
        MODE_3D_TILED_THIN1 = 0xC,
        MODE_3D_TILED_THICK = 0xD,
        MODE_3B_TILED_THIN1 = 0xE,
        MODE_3B_TILED_THICK = 0xF,
        MODE_LAST = 0x20,
    };
    
    public enum AddrTileMode
    {
        ADDR_TM_LINEAR_GENERAL = 0x0,
        ADDR_TM_LINEAR_ALIGNED = 0x1,
        ADDR_TM_1D_TILED_THIN1 = 0x2,
        ADDR_TM_1D_TILED_THICK = 0x3,
        ADDR_TM_2D_TILED_THIN1 = 0x4,
        ADDR_TM_2D_TILED_THIN2 = 0x5,
        ADDR_TM_2D_TILED_THIN4 = 0x6,
        ADDR_TM_2D_TILED_THICK = 0x7,
        ADDR_TM_2B_TILED_THIN1 = 0x8,
        ADDR_TM_2B_TILED_THIN2 = 0x9,
        ADDR_TM_2B_TILED_THIN4 = 0x0A,
        ADDR_TM_2B_TILED_THICK = 0x0B,
        ADDR_TM_3D_TILED_THIN1 = 0x0C,
        ADDR_TM_3D_TILED_THICK = 0x0D,
        ADDR_TM_3B_TILED_THIN1 = 0x0E,
        ADDR_TM_3B_TILED_THICK = 0x0F,
        ADDR_TM_2D_TILED_XTHICK = 0x10,
        ADDR_TM_3D_TILED_XTHICK = 0x11,
        ADDR_TM_POWER_SAVE = 0x12,
        ADDR_TM_COUNT = 0x13,
    }
    
    public enum AddrTileType
    {
        ADDR_DISPLAYABLE = 0,
        ADDR_NON_DISPLAYABLE = 1,
        ADDR_DEPTH_SAMPLE_ORDER = 2,
        ADDR_THICK_TILING = 3,
    }
    
    public enum Gx2SurfaceFormat
    {
        INVALID = 0x0,
        TC_R8_UNORM = 0x1,
        TC_R8_UINT = 0x101,
        TC_R8_SNORM = 0x201,
        TC_R8_SINT = 0x301,
        T_R4_G4_UNORM = 0x2,
        TCD_R16_UNORM = 0x5,
        TC_R16_UINT = 0x105,
        TC_R16_SNORM = 0x205,
        TC_R16_SINT = 0x305,
        TC_R16_FLOAT = 0x806,
        TC_R8_G8_UNORM = 0x7,
        TC_R8_G8_UINT = 0x107,
        TC_R8_G8_SNORM = 0x207,
        TC_R8_G8_SINT = 0x307,
        TCS_R5_G6_B5_UNORM = 0x8,
        TC_R5_G5_B5_A1_UNORM = 0xA,
        TC_R4_G4_B4_A4_UNORM = 0xB,
        TC_A1_B5_G5_R5_UNORM = 0xC,
        TC_R32_UINT = 0x10D,
        TC_R32_SINT = 0x30D,
        TCD_R32_FLOAT = 0x80E,
        TC_R16_G16_UNORM = 0xF,
        TC_R16_G16_UINT = 0x10F,
        TC_R16_G16_SNORM = 0x20F,
        TC_R16_G16_SINT = 0x30F,
        TC_R16_G16_FLOAT = 0x810,
        D_D24_S8_UNORM = 0x11,
        T_R24_UNORM_X8 = 0x11,
        T_X24_G8_UINT = 0x111,
        D_D24_S8_FLOAT = 0x811,
        TC_R11_G11_B10_FLOAT = 0x816,
        TCS_R10_G10_B10_A2_UNORM = 0x19,
        TC_R10_G10_B10_A2_UINT = 0x119,
        T_R10_G10_B10_A2_SNORM = 0x219,
        TC_R10_G10_B10_A2_SNORM = 0x219,
        TC_R10_G10_B10_A2_SINT = 0x319,
        TCS_R8_G8_B8_A8_UNORM = 0x1A,
        TC_R8_G8_B8_A8_UINT = 0x11A,
        TC_R8_G8_B8_A8_SNORM = 0x21A,
        TC_R8_G8_B8_A8_SINT = 0x31A,
        TCS_R8_G8_B8_A8_SRGB = 0x41A,
        TCS_A2_B10_G10_R10_UNORM = 0x1B,
        TC_A2_B10_G10_R10_UINT = 0x11B,
        D_D32_FLOAT_S8_UINT_X24 = 0x81C,
        T_R32_FLOAT_X8_X24 = 0x81C,
        T_X32_G8_UINT_X24 = 0x11C,
        TC_R32_G32_UINT = 0x11D,
        TC_R32_G32_SINT = 0x31D,
        TC_R32_G32_FLOAT = 0x81E,
        TC_R16_G16_B16_A16_UNORM = 0x1F,
        TC_R16_G16_B16_A16_UINT = 0x11F,
        TC_R16_G16_B16_A16_SNORM = 0x21F,
        TC_R16_G16_B16_A16_SINT = 0x31F,
        TC_R16_G16_B16_A16_FLOAT = 0x820,
        TC_R32_G32_B32_A32_UINT = 0x122,
        TC_R32_G32_B32_A32_SINT = 0x322,
        TC_R32_G32_B32_A32_FLOAT = 0x823,
        T_BC1_UNORM = 0x31,
        T_BC1_SRGB = 0x431,
        T_BC2_UNORM = 0x32,
        T_BC2_SRGB = 0x432,
        T_BC3_UNORM = 0x33,
        T_BC3_SRGB = 0x433,
        T_BC4_UNORM = 0x34,
        T_BC4_SNORM = 0x234,
        T_BC5_UNORM = 0x35,
        T_BC5_SNORM = 0x235,
        T_NV12_UNORM = 0x81,
        FIRST = 0x1,
        LAST = 0x83F,
    };
}