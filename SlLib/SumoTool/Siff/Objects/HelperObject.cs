using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class HelperObject : IObjectDef
{
    public string ObjectType => "HELP";

    public int KeyframeHash;
    public int PointerAreaHash;
    public int Layer;
    public Vector2 Anchor;

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        PointerAreaHash = context.ReadInt32();
        context.Position += context.Platform.GetPointerSize() * 0x2;
        Layer = context.ReadInt32();
        Anchor = context.ReadFloat2();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, KeyframeHash, 0x0);
        context.WriteInt32(buffer, PointerAreaHash, 0x4);
        context.WriteInt32(buffer, Layer, 0x10);
        context.WriteFloat2(buffer, Anchor, 0x14);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x14 + platform.GetPointerSize() * 0x2;
    }
}