using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Sh;

namespace SlLib.SumoTool.Siff;

public class ShSamplerData : IResourceSerializable
{
    public List<NavWaypointShSampleSet> SampleSets = [];
    
    public void Load(ResourceLoadContext context)
    {
        SampleSets = context.LoadArrayPointer<NavWaypointShSampleSet>(context.ReadInt32());
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SaveReferenceArray(buffer, SampleSets, 0x4);
        context.WriteInt32(buffer, SampleSets.Count, 0x0);
        
        // Like many other siff resources, doesn't technically use relocations
        context.Relocations.Clear();
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x8;
    }
}