using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class GouraudObject : IObjectDef
{
    public Vector2 Anchor;
    public int BlendType;

    public int KeyframeHash;
    public int Layer;
    public int PointerAreahash;
    public int ScissorHash;
    public string ObjectType => "GORD";

    public void Load(ResourceLoadContext context, int offset)
    {
        KeyframeHash = context.ReadInt32(offset);
        PointerAreahash = context.ReadInt32(offset + 4);
        ScissorHash = context.ReadInt32(offset + 8);
        BlendType = context.ReadInt32(offset + 24);
        Layer = context.ReadInt32(offset + 28);
        Anchor = context.ReadFloat2(offset + 32);
    }
}