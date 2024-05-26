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

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x30 + platform.GetPointerSize() * 0x5;
    }
}