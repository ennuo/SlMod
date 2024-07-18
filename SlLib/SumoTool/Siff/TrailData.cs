using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff;

public class TrailData : IResourceSerializable
{
    public int NameHash;
    public List<Vector4> Points = [];
    
    public void Load(ResourceLoadContext context)
    {
        NameHash = context.ReadInt32();
        Points = context.LoadArrayPointer(context.ReadInt32(), context.ReadFloat4);
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NameHash, 0x0);
        context.WriteInt32(buffer, Points.Count, 0x4);
        ISaveBuffer pointData = context.SaveGenericPointer(buffer, 0x8, Points.Count * 0x10, align: 0x10);
        for (int i = 0; i < Points.Count; ++i)
            context.WriteFloat4(pointData, Points[i], i * 0x10);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}