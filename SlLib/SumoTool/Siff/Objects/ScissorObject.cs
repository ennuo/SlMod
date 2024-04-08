using System.Numerics;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class ScissorObject : IObjectDef
{
    public Vector2 Anchor;

    public int KeyframeHash;
    public string ObjectType => "SCIS";

    public void Load(ResourceLoadContext context, int offset)
    {
        KeyframeHash = context.ReadInt32(offset);
        Anchor = context.ReadFloat2(offset + 8);
    }
}