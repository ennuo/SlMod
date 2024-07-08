using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavWaypoint : IResourceSerializable
{
    public Vector3 Pos;
    public Vector3 Dir;
    public Vector3 Up;
    public string Name = string.Empty;
    public float TrackDist;
    public int Flags;
    
    public void Load(ResourceLoadContext context)
    {
        Pos = context.ReadPaddedFloat3();
        Dir = context.ReadPaddedFloat3();
        Up = context.ReadPaddedFloat3();
        Name = context.ReadFixedString(0x40);
        TrackDist = context.ReadFloat();
        Flags = context.ReadInt32();

        int numToLinks = context.ReadInt32();
        int numFromLinks = context.ReadInt32();
        
        
        
        



        // fill rest of the data later
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xb0;
    }
}