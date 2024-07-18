using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavWaypointLink : IResourceSerializable
{
    public Vector3 FromToNormal;
    public Vector3 Right;
    public Vector3 Left;
    public Vector3 RacingLineLimitLeft;
    public Vector3 RacingLineLimitRight;
    // plane
    public float RacingLineLeftScalar;
    public float RacingLineRightScalar;
    
    public NavWaypoint? From;
    public NavWaypoint? To;
    
    public void Load(ResourceLoadContext context)
    {
        FromToNormal = context.ReadAlignedFloat3();
        Right = context.ReadAlignedFloat3();
        Left = context.ReadAlignedFloat3();
        RacingLineLimitLeft = context.ReadAlignedFloat3();
        RacingLineLimitRight = context.ReadAlignedFloat3();
        context.Position += 32; // plane
        RacingLineLeftScalar = context.ReadFloat();
        RacingLineRightScalar = context.ReadFloat();
        From = context.LoadPointer<NavWaypoint>();
        To = context.LoadPointer<NavWaypoint>();
        
        // way more fields i havent added yet
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 256;
    }
}