using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Fonts;

public class CharacterInfo : IResourceSerializable
{
    public int TextureHash;
    public short CharCode;
    public short X, Y, W, H;
    public short PreShift, PostShift;
    public short YAdjust;
    public short HasGraphic;
    
    public void Load(ResourceLoadContext context)
    {
        TextureHash = context.ReadInt32();
        CharCode = context.ReadInt16();
        X = context.ReadInt16();
        Y = context.ReadInt16();
        W = context.ReadInt16();
        H = context.ReadInt16();
        PreShift = context.ReadInt16();
        PostShift = context.ReadInt16();
        YAdjust = context.ReadInt16();
        HasGraphic = context.ReadInt16();
        context.ReadInt16(); // Pad
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, TextureHash, 0x0);
        context.WriteInt16(buffer, CharCode, 0x4);
        context.WriteInt16(buffer, X, 0x6);
        context.WriteInt16(buffer, Y, 0x8);
        context.WriteInt16(buffer, W, 0xa);
        context.WriteInt16(buffer, H, 0xc);
        context.WriteInt16(buffer, PreShift, 0xe);
        context.WriteInt16(buffer, PostShift, 0x10);
        context.WriteInt16(buffer, YAdjust, 0x12);
        context.WriteInt16(buffer, HasGraphic, 0x14);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        // early transformed versions still use 0x18
        return version == -1 ? 0x18 : 0x24;
    }
}