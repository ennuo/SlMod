using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuField : IResourceSerializable
{
    public int BranchIndex;
    public int Type;
    public float Magnitude;
    public float Attenuation;
    public Vector4 Direction;
    
    public void Load(ResourceLoadContext context)
    {
        BranchIndex = context.ReadInt32();
        Type = context.ReadInt32();
        Magnitude = context.ReadFloat();
        Attenuation = context.ReadFloat();
        Direction = context.ReadFloat4();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, BranchIndex, 0x0);
        context.WriteInt32(buffer, Type, 0x4);
        context.WriteFloat(buffer, Magnitude, 0x8);
        context.WriteFloat(buffer, Attenuation, 0xc);
        context.WriteFloat4(buffer, Direction, 0x10);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
    }
}