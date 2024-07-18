using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest.GCM;

public class CellGcmTexture : IResourceSerializable
{
    public CellGcmEnumForGtf Format => (CellGcmEnumForGtf)(RawFormat & ~0x60);
    public byte RawFormat;
    public byte MipCount;
    public byte Dimension;
    public bool Cubemap;
    public int Remap;
    public short Width, Height, Depth;
    public byte Location;
    public byte Flags;
    public int Pitch, Offset;

    public static bool IsDXT(CellGcmEnumForGtf format)
    {
        return format is CellGcmEnumForGtf.DXT1 or CellGcmEnumForGtf.DXT3 or CellGcmEnumForGtf.DXT5;
    }

    public static int GetDepth(CellGcmEnumForGtf format)
    {
        switch (format)
        {
            case CellGcmEnumForGtf.B8: return 1;
            case CellGcmEnumForGtf.A1R5G5B5:
            case CellGcmEnumForGtf.A4R4G4B4:
            case CellGcmEnumForGtf.R5G6B5:
            case CellGcmEnumForGtf.G8B8:
                    return 2;
            case CellGcmEnumForGtf.DXT1:
                return 8;
            case CellGcmEnumForGtf.DXT3:
            case CellGcmEnumForGtf.DXT5:
                    return 16;
            default:
                return 4;
        }
    }
    
    public static int GetPitch(CellGcmEnumForGtf format, int width)
    {
        if (format == CellGcmEnumForGtf.DXT1)
            return ((width + 3) / 4) * 8;
        if (format is CellGcmEnumForGtf.DXT3 or CellGcmEnumForGtf.DXT5)
            return ((width + 3) / 4) * 16;
        return width * GetDepth(format);
    }

    public static int GetImageSize(CellGcmEnumForGtf format, int width, int height)
    {
        int pitch = GetPitch(format, width);
        if (IsDXT(format))
            return pitch * ((height + 3) / 4);
        return pitch * height;
    }
    
    public void Load(ResourceLoadContext context)
    {
        RawFormat = context.ReadInt8();
        MipCount = context.ReadInt8();
        Dimension = context.ReadInt8();
        Cubemap = context.ReadBoolean();
        Remap = context.ReadInt32();
        Width = context.ReadInt16();
        Height = context.ReadInt16();
        Depth = context.ReadInt16();
        Location = context.ReadInt8();
        context.ReadInt8(); // pad
        Pitch = context.ReadInt32();
        Offset = context.ReadInt32();
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version) => 0x18;
}