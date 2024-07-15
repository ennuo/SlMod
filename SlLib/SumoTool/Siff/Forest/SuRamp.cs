using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public struct SuRamp : IResourceSerializable
{
    public int Shift;
    
    public void Load(ResourceLoadContext context)
    {
        Shift = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Shift, 0);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x4;
    }
}