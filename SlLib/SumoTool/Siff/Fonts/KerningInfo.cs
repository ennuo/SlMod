using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Fonts;

public class KerningInfo : IResourceSerializable
{
    public int KerningHash;
    public int KerningValue;
    
    public void Load(ResourceLoadContext context)
    {
        KerningHash = context.ReadInt32();
        KerningValue = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, KerningHash, 0x0);
        context.WriteInt32(buffer, KerningValue, 0x4);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x8;
    }
}