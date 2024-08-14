using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavTrackMarker : IResourceSerializable
{
    public Vector3 Pos;
    public Vector3 Dir = Vector3.UnitZ;
    public Vector3 Up = Vector3.UnitY;
    
    // 0x2 = checkpoint
    // 0x3 = start line?
    // 0x5 = grid position?
    // 0x6 = drift?
    // 0xd = jump start
    
    // 0x11 = extra grid?
    // 0x12 = drift cancel?
    
    // 0x19 = AUDIO_AMBIENCE
    // 0x1a = AUDIO_AMBIENCE_2
    // 0x1b = AUDIO_AMBIENCE_3
    // 0x1c = AUDIO_AMBIENCE_4
    // 0x1d = AUDIO_AMBIENCE_5
    
    // 0x2e = camera rail
    // 0x30 = racing line marker (value is hash, disables/enables racing lines using this)
    // 0x35 = portal
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
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, Pos, 0x0);
        context.WriteFloat3(buffer, Dir, 0x10);
        context.WriteFloat3(buffer, Up, 0x20);
        context.WriteInt32(buffer, Type, 0x30);
        context.WriteFloat(buffer, Radius, 0x34);
        context.SavePointer(buffer, Waypoint, 0x38, align: 0x10, deferred: true);
        context.WriteInt32(buffer, Value, 0x3c);
        context.WriteFloat(buffer, TrackDist, 0x40);
        context.SavePointer(buffer, LinkedTrackMarker, 0x44, align: 0x10, deferred: true);
        context.WriteFloat(buffer, JumpSpeedPercentage, 0x48);
        context.WriteInt32(buffer, Flags, 0x4c);
        context.WriteString(buffer, Text, 0x50);
        
        // Flags & 0x8 = PLANE
        // Flags & 0x10 = CAR
        // Flags & 0x20 = BOAT
        
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x60;
    }
}