using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class TextObject : IObjectDef
{
    public string ObjectType => "TEXT";

    public int FontHash;
    public int StringHash;
    public int KeyframeHash;
    public int PointerAreaHash;
    public int ScissorHash;

    public short Justification;
    public byte WordWrap;
    public bool IsConstrained;
    public float ConstrainValue;
    public bool IsBilinear;
    public int BlendType;
    public int Layer;
    public Vector2 Anchor;

    public void Load(ResourceLoadContext context)
    {
        FontHash = context.ReadInt32();
        StringHash = context.ReadInt32();
        KeyframeHash = context.ReadInt32();
        PointerAreaHash = context.ReadInt32();
        ScissorHash = context.ReadInt32();

        context.Position += context.Platform.GetPointerSize() * 0x5;

        Justification = context.ReadInt16();
        WordWrap = context.ReadInt8();
        IsConstrained = context.ReadBoolean();
        ConstrainValue = context.ReadFloat();
        IsBilinear = context.ReadBoolean(true);
        BlendType = context.ReadInt32();
        Layer = context.ReadInt32();
        Anchor = context.ReadFloat2();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, FontHash, 0x0);
        context.WriteInt32(buffer, StringHash, 0x4);
        context.WriteInt32(buffer, KeyframeHash, 0x8);
        context.WriteInt32(buffer, PointerAreaHash, 0xc);
        context.WriteInt32(buffer, ScissorHash, 0x10);
        
        context.WriteInt16(buffer, Justification, 0x28);
        context.WriteInt8(buffer, WordWrap, 0x2a);
        context.WriteBoolean(buffer, IsConstrained, 0x2b);
        context.WriteFloat(buffer, ConstrainValue, 0x2c);
        context.WriteBoolean(buffer, IsBilinear, 0x30, wide: true);
        context.WriteInt32(buffer, BlendType, 0x34);
        context.WriteInt32(buffer, Layer, 0x38);
        context.WriteFloat2(buffer, Anchor, 0x3c);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x30 + platform.GetPointerSize() * 0x5;
    }
}