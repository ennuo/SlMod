using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class PointerAreaObject : IObjectDef
{
    public Vector2 Anchor;

    public int KeyframeHash;
    public int Layer;
    public string ObjectType => "PNTR";

    public void Load(ResourceLoadContext context, int offset)
    {
        KeyframeHash = context.ReadInt32(offset);
        Layer = context.ReadInt32(offset + 8);
        Anchor = context.ReadFloat2(offset + 12);
    }
}