using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Logic;

public class TriggerAttribute : IResourceSerializable
{
    public int NameHash;
    public int PackedValue; // union of float, integer, and uint
    
    public void Load(ResourceLoadContext context)
    {
        NameHash = context.ReadInt32();
        PackedValue = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NameHash, 0x0);
        context.WriteInt32(buffer, PackedValue, 0x4);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x8;
    }
}