using System.Buffers.Binary;
using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavRacingLineSeg : IResourceSerializable
{
    public Vector3 RacingLine;
    public Vector3 SafeRacingLine;
    public float RacingLineScalar;
    public float SafeRacingLineScalar;
    public NavWaypointLink? Link;
    public float RacingLineLength = 1.0f;
    public int TurnType;
    
    public void Load(ResourceLoadContext context)
    {
        RacingLine = context.ReadPaddedFloat3();
        SafeRacingLine = context.ReadPaddedFloat3();
        RacingLineScalar = context.ReadFloat();
        SafeRacingLineScalar = context.ReadFloat();
        
        Link = context.LoadPointer<NavWaypointLink>();
        
        RacingLineLength = context.ReadFloat();
        TurnType = context.ReadInt32();
        
        // todo: missing fields
        
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x40;
    }
}