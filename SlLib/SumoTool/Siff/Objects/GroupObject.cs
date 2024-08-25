using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class GroupObject : IObjectDef
{
    public string ObjectType => "GROP";

    public int KeyframeHash;
    public int PointerAreaHash;
    public int Layer { get; set; } = 100;
    public Vector2 Anchor { get; set; }
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

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // Sort object hashes
        ObjectHashes.Sort((a, z) => ((uint)a).CompareTo((uint)z));
        
        context.WriteInt32(buffer, KeyframeHash, 0x0);
        context.WriteInt32(buffer, PointerAreaHash, 0x4);
        context.WriteInt32(buffer, Layer, 0x10);
        context.WriteFloat2(buffer, Anchor, 0x14);
        context.WriteInt32(buffer, ObjectHashes.Count, 0x1c);
        context.WritePointerAtOffset(buffer, 0x20, buffer.Address + 0x24);
        for (int i = 0; i < ObjectHashes.Count; ++i)
            context.WriteInt32(buffer, ObjectHashes[i], 0x24 + i * 4);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x18 + platform.GetPointerSize() * 0x3 + ObjectHashes.Count * 0x4;
    }
}