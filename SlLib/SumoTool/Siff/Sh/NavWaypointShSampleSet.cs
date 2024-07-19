using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Sh;

public class NavWaypointShSampleSet : IResourceSerializable
{
    public List<NavWaypointShSample> Samples = [];
    
    public void Load(ResourceLoadContext context)
    {
        int sampleData = context.ReadPointer();
        Samples = context.LoadArray<NavWaypointShSample>(sampleData, context.ReadInt32());
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SaveReferenceArray(buffer, Samples, 0x0, align: 0x10);
        context.WriteInt32(buffer, Samples.Count, 0x4);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x8;
    }
}