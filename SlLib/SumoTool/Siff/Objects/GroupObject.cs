using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class GroupObject : IObjectDef
{
    public string ObjectType => "GROP";

    public int KeyframeHash;
    public int PointerAreaHash;
    public int Layer;
    public Vector2 Anchor;
    public List<int> ObjectHashes = [];

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        PointerAreaHash = context.ReadInt32();
        context.Position += context.Platform.GetPointerSize() * 0x2;
        Layer = context.ReadInt32();
        Anchor = context.ReadFloat2();
        ObjectHashes = context.LoadArrayPointer(context.ReadInt32(), context.ReadInt32);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x18 + platform.GetPointerSize() * 0x3 + ObjectHashes.Count * 0x4;
    }
}