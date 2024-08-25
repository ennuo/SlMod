using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

// CallbackObject
public class CustomObject : IObjectDef
{
    public string ObjectType => "GNRC";

    public int KeyframeHash;
    public int PointerAreaHash;
    public int ScissorHash;
    public int BlendType;
    public int Layer { get; set; }
    public Vector2 Anchor { get; set; }
    public int ObjectId;
    public float u0, v0, u1, v1;
    public float u2, v2;
    public float u3, v3;
    public int extraDataSize;

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        PointerAreaHash = context.ReadInt32();
        ScissorHash = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, KeyframeHash, 0x0);
        context.WriteInt32(buffer, PointerAreaHash, 0x4);
        context.WriteInt32(buffer, ScissorHash, 0x8);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x1c + platform.GetPointerSize() * 0x3;
    }
}