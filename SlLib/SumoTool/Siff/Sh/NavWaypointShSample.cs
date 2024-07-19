using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Sh;

public class NavWaypointShSample : IResourceSerializable
{
    public SuShSampleCompressed Sample;    
    public short ParticleBrightness;
    public short PlantBrightness;
    public Vector3 Pos;
    
    public void Load(ResourceLoadContext context)
    {
        for (int i = 0; i < 27; ++i)
            Sample[i] = context.ReadInt16();
        ParticleBrightness = context.ReadInt16();
        PlantBrightness = context.ReadInt16();
        context.Align(0x10);
        Pos = context.ReadAlignedFloat3();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        for (int i = 0; i < 27; ++i)
            context.WriteInt16(buffer, Sample[i], i * 2);
        context.WriteInt16(buffer, ParticleBrightness, 0x36);
        context.WriteInt16(buffer, PlantBrightness, 0x38);
        context.WriteFloat3(buffer, Pos, 0x40);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x50;
    }
}