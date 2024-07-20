using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Sh;

public class NavWaypointShSample : IResourceSerializable
{
    public SuShSample Sample;    
    public float ParticleBrightness;
    public float PlantBrightness;
    public Vector3 Pos;
    
    public void Load(ResourceLoadContext context)
    {
        for (int i = 0; i < 27; ++i)
            Sample[i] = context.ReadFloat();
        ParticleBrightness = context.ReadFloat();
        PlantBrightness = context.ReadFloat();
        context.Align(0x10);
        Pos = context.ReadAlignedFloat3();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        for (int i = 0; i < 27; ++i)
            context.WriteFloat(buffer, Sample[i], i * 4);
        context.WriteFloat(buffer, ParticleBrightness, 0x6c);
        context.WriteFloat(buffer, PlantBrightness, 0x70);
        context.WriteFloat3(buffer, Pos, 0x80);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x90;
    }
}