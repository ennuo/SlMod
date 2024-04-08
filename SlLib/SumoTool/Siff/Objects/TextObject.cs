using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class TextObject : IObjectDef
{
    public Vector2 Anchor;
    public int BlendType;
    public float ConstrainValue;

    public int FontHash;
    public bool IsBilinear;
    public bool IsConstrained;
    public short Justification;
    public int KeyframeHash;
    public int Layer;
    public int PointerAreaHash;
    public int ScissorHash;
    public int StringHash;
    public byte WordWrap;
    public string ObjectType => "TEXT";

    public void Load(ResourceLoadContext context, int offset)
    {
        FontHash = context.ReadInt32(offset);
        StringHash = context.ReadInt32(offset + 4);
        KeyframeHash = context.ReadInt32(offset + 8);
        PointerAreaHash = context.ReadInt32(offset + 12);
        ScissorHash = context.ReadInt32(offset + 16);
        Justification = context.ReadInt16(offset + 40);
        WordWrap = context.ReadInt8(offset + 42);
        IsConstrained = context.ReadBoolean(offset + 43);
        ConstrainValue = context.ReadFloat(offset + 44);
        IsBilinear = context.ReadBoolean(offset + 48, true);
        BlendType = context.ReadInt32(offset + 52);
        Layer = context.ReadInt32(offset + 56);
        Anchor = context.ReadFloat2(offset + 60);
    }
}