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

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, KeyframeHash, 0x0);
        context.WriteInt32(buffer, PointerAreaHash, 0x4);
        context.WriteInt32(buffer, ScissorHash, 0x8);
        context.WriteInt32(buffer, BlendType, 0x18);
        context.WriteInt32(buffer, Layer, 0x1c);
        context.WriteFloat2(buffer, Anchor, 0x20);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x1c + platform.GetPointerSize() * 0x3;
    }
}