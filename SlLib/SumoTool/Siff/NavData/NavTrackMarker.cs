using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavTrackMarker : IResourceSerializable
{
    public Vector3 Pos;
    public Vector3 Dir = Vector3.UnitZ;
    public Vector3 Up = Vector3.UnitY;
    public int Type;
    public float Radius = 20.0f;
    public NavWaypoint? Waypoint;
    public int Value;
    public float TrackDist;
    public NavTrackMarker? LinkedTrackMarker;
    public float JumpSpeedPercentage = 0.65f;
    public int Flags;
    public string Text = string.Empty;
    
    public void Load(ResourceLoadContext context)
    {
        Pos = context.ReadAlignedFloat3();
        Dir = context.ReadAlignedFloat3();
        Up = context.ReadAlignedFloat3();
        Type = context.ReadInt32();
        Radius = context.ReadFloat();
        Waypoint = context.LoadPointer<NavWaypoint>();
        Value = context.ReadInt32();
        TrackDist = context.ReadFloat();
        LinkedTrackMarker = context.LoadPointer<NavTrackMarker>();
        JumpSpeedPercentage = context.ReadFloat();
        Flags = context.ReadInt32();
        Text = context.ReadFixedString(0x10);
        
        Console.WriteLine($"NavTrackMarker (type={Type}, waypoint_attachment={Waypoint?.Name ?? "-Empty-"}, name={Text}, value={Value})");
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x60;
    }
}