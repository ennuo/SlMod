using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class GouraudObject : IObjectDef
{
    public string ObjectType => "GORD";

    public int KeyframeHash;
    public int PointerAreaHash;
    public int ScissorHash;
    public int BlendType;
    public int Layer;
    public Vector2 Anchor;

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        PointerAreaHash = context.ReadInt32();
        ScissorHash = context.ReadInt32();
        context.Position += context.Platform.GetPointerSize() * 0x3;
        BlendType = context.ReadInt32();
        Layer = context.ReadInt32();
        Anchor = context.ReadFloat2();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x1c + platform.GetPointerSize() * 0x3;
    }
}