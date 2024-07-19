using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuCurve : IResourceSerializable
{
    public int BranchId;
    public int Degree;
    public List<Vector4> ControlPoints = [];
    public List<float> Knots = [];
    
    public void Load(ResourceLoadContext context)
    {
        BranchId = context.ReadInt32();
        Degree = context.ReadInt32();

        int numControlPoints = context.ReadInt32();
        int numKnots = context.ReadInt32();
        
        for (int i = 0; i < numControlPoints; ++i)
            ControlPoints.Add(context.ReadFloat4());
        for (int i = 0; i < numKnots; ++i)
            Knots.Add(context.ReadFloat());
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, BranchId, 0x0);
        context.WriteInt32(buffer, Degree, 0x4);
        context.WriteInt32(buffer, ControlPoints.Count, 0x8);
        context.WriteInt32(buffer, Knots.Count, 0xc);

        int offset = 0x10;
        foreach (Vector4 point in ControlPoints)
        {
            context.WriteFloat4(buffer, point, offset);
            offset += 0x10;
        }
        
        foreach (float knot in Knots)
        {
            context.WriteFloat(buffer, knot, offset);
            offset += 0x4;
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x10 + (ControlPoints.Count * 0x10) + (Knots.Count * 0x4);
    }
}