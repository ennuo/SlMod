using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class PointerAreaObject : IObjectDef
{
    public string ObjectType => "PNTR";

    public int KeyframeHash;
    public int Layer;
    public Vector2 Anchor;

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        context.Position += context.Platform.GetPointerSize();
        Layer = context.ReadInt32();
        Anchor = context.ReadFloat2();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10 + platform.GetPointerSize();
    }
}