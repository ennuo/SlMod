using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class ScissorObject : IObjectDef
{
    public string ObjectType => "SCIS";

    public int KeyframeHash;
    public Vector2 Anchor;

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        context.Position += context.Platform.GetPointerSize();
        Anchor = context.ReadFloat2();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x14 : 0x10;
    }
}