using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuCurve : IResourceSerializable
{
    public int BranchId;
    public int Degree;
    public int NumControlPoints;
    public int NumKnots;
    
    public void Load(ResourceLoadContext context)
    {
        BranchId = context.ReadInt32();
        Degree = context.ReadInt32();
        NumControlPoints = context.ReadInt32();
        NumKnots = context.ReadInt32();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, BranchId, 0x0);
        context.WriteInt32(buffer, Degree, 0x4);
        context.WriteInt32(buffer, NumControlPoints, 0x8);
        context.WriteInt32(buffer, NumKnots, 0xc);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10;
    }
}