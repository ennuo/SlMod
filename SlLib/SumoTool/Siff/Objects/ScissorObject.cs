using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Objects;

public class ScissorObject : IObjectDef
{
    public string ObjectType => "SCIS";

    public int KeyframeHash;
    public Vector2 Anchor { get; set; }
    
    // doesnt actually exist
    public int Layer { get; set; }

    public void Load(ResourceLoadContext context)
    {
        KeyframeHash = context.ReadInt32();
        context.Position += context.Platform.GetPointerSize();
        Anchor = context.ReadFloat2();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, KeyframeHash, 0x0);
        context.WriteFloat2(buffer, Anchor, 0x8);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x14 : 0x10;
    }
}