using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class HelperObject : IObjectDef
{
    public Vector2 Anchor;

    public int KeyframeHash;
    public int Layer;
    public int PointerAreaHash;
    public string ObjectType => "HELP";

    public void Load(ResourceLoadContext context, int offset)
    {
        KeyframeHash = context.ReadInt32(offset);
        PointerAreaHash = context.ReadInt32(offset + 4);
        Layer = context.ReadInt32(offset + 16);
        Anchor = context.ReadFloat2(offset + 20);
    }
}