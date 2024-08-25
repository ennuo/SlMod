using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Enums;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.NavData;

public class NavWaypoint : IResourceSerializable
{
    public Vector3 Pos;
    public Vector3 Dir = Vector3.UnitZ;
    public Vector3 Up = Vector3.UnitY;
    public string Name = string.Empty;
    public float TrackDist;
    
    public int Flags = 17891328;
    
    public List<NavWaypointLink> ToLinks = [];
    public List<NavWaypointLink> FromLinks = [];
    
    public float TargetSpeed = 1.0f;
    public List<NavTrackMarker> TrackMarkers = [];
    
    // old ssr stuff
    public float SnowLevel;
    public readonly byte[] FogBlend = [0xFF, 0, 0, 0];
    public readonly byte[] BloomBlend = [0xFF, 0, 0, 0];
    public readonly byte[] ExposureBlend = [0xFF, 0, 0, 0];
    
    // transformed stuff
    
    // same as waypoint flags and racing line permissions
    public int Permissions;
    public NavWaypoint? UnknownWaypoint; // links to end of the spatial group?
    
    public void Load(ResourceLoadContext context)
    {
        Pos = context.ReadAlignedFloat3();
        Dir = context.ReadAlignedFloat3();
        Up = context.ReadAlignedFloat3();
        Name = context.ReadFixedString(0x40);
        
        TrackDist = context.ReadFloat();
        Flags = context.ReadInt32();

        int numToLinks = context.ReadInt32();
        int numFromLinks = context.ReadInt32();
        
        ToLinks = context.LoadPointerArray<NavWaypointLink>(numToLinks);
        FromLinks = context.LoadArrayPointer<NavWaypointLink>(numFromLinks);

        foreach (var link in ToLinks) link.From = this;
        foreach (var link in FromLinks) link.To = this;
        
        // This field is generally 0.0f when serialized in Transformed,
        // but get resets to 1.0f when loaded, so editing it is pointless.
        TargetSpeed = context.ReadFloat();
        
        if (context.Version < 0x9)
        {
            if (context.ReadPointer() != 0) 
                throw new SerializationException("NavWaypointSHSampleSet not supported!");
            
            TrackMarkers = context.LoadPointerArray<NavTrackMarker>(context.ReadPointer(), context.ReadInt32());
            SnowLevel = context.ReadFloat();
        
            for (int i = 0; i < 4; ++i) FogBlend[i] = context.ReadInt8();
            for (int i = 0; i < 4; ++i) BloomBlend[i] = context.ReadInt8();
            for (int i = 0; i < 4; ++i) ExposureBlend[i] = context.ReadInt8();   
        }
        else
        {
            int trackData = context.ReadPointer();
            int numMarkers = context.ReadInt32();
            //TrackMarkers = context.LoadPointerArray<NavTrackMarker>(trackData, numMarkers);
            
            // seems to always be 0?
            context.Position += 8;
            Permissions = context.ReadInt32();
            UnknownWaypoint = context.LoadPointer<NavWaypoint>();
            
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, Pos, 0x0);
        context.WriteFloat3(buffer, Dir, 0x10);
        context.WriteFloat3(buffer, Up, 0x20);
        context.WriteString(buffer, Name, 0x30);
        context.WriteFloat(buffer, TrackDist, 0x70);
        context.WriteInt32(buffer, Flags, 0x74);
        
        context.WriteInt32(buffer, ToLinks.Count, 0x78);
        context.WriteInt32(buffer, FromLinks.Count, 0x7c);
        
        context.SaveReferenceArray(buffer, FromLinks, 0x84, align: 0x10);
        context.SavePointerArray(buffer, ToLinks, 0x80, elementAlignment: 0x10, deferred: true);
        
        context.WriteFloat(buffer, TargetSpeed, 0x88);
        
        if (context.Version >= 0x9)
        {
            context.SavePointerArray(buffer, TrackMarkers, 0x8c, elementAlignment: 0x10, deferred: true);
            context.WriteInt32(buffer, TrackMarkers.Count, 0x90);
            
            context.WriteInt32(buffer, Permissions, 0x9c);
            context.SavePointer(buffer, UnknownWaypoint, 0xa0, align: 0x10, deferred: true);
        }
        else
        {
            context.SavePointerArray(buffer, TrackMarkers, 0x90, elementAlignment: 0x10, deferred: true);
            context.WriteInt32(buffer, TrackMarkers.Count, 0x94);
            context.WriteFloat(buffer, SnowLevel, 0x98);

            for (int i = 0; i < 4; ++i)
            {
                context.WriteInt8(buffer, FogBlend[i], 0x9c + (0x1 * i));
                context.WriteInt8(buffer, BloomBlend[i], 0xa0 + (0x1 * i));
                context.WriteInt8(buffer, ExposureBlend[i], 0xa4 + (0x1 * i));
            }   
        }
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xb0;
    }
}