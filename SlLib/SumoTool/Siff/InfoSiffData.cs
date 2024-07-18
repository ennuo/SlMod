using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff;

public class InfoSiffData : IResourceSerializable
{
    public int Type;
    
    public void Load(ResourceLoadContext context)
    {
        Type = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Type, 0);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 4;
    }
}