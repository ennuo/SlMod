using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class TextureObject : IObjectDef
{
    public Vector2 Anchor;
    public int BlendType;
    public bool IsBilinear;
    public int KeyframeHash;
    public int Layer;
    public int PointerAreaHash;
    public int ScissorHash;
    public int TextureEffectHash;

    public int TextureHash;
    public string ObjectType => "TXTR";

    public void Load(ResourceLoadContext context, int offset)
    {
        TextureHash = context.ReadInt32(offset);
        TextureEffectHash = context.ReadInt32(offset + 4);
        KeyframeHash = context.ReadInt32(offset + 8);
        PointerAreaHash = context.ReadInt32(offset + 12);
        ScissorHash = context.ReadInt32(offset + 16);
        IsBilinear = context.ReadBoolean(offset + 40, true);
        BlendType = context.ReadInt32(offset + 44);
        Layer = context.ReadInt32(offset + 48);
        Anchor = context.ReadFloat2(offset + 52);
    }
}