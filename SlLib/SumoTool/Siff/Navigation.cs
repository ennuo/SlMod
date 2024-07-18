using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.NavData;

namespace SlLib.SumoTool.Siff;

public class Navigation : IResourceSerializable
{
    public int NameHash;
    public bool Remapped;
    public List<Vector3> Vertices = [];
    public List<Vector3> Normals = [];
    public List<NavWaypoint> Waypoints = [];
    public List<NavRacingLine> RacingLines = [];
    public List<NavTrackMarker> TrackMarkers = [];
    public List<NavSpatialGroup> SpatialGroups = [];

    public float TotalTrackDist;
    public float LowestPoint;
    public float HighestPoint;
    
    public void Load(ResourceLoadContext context)
    {
        int link = context.Base;
        context.Base = context.Position;
        
        NameHash = context.ReadInt32();
        context.Version = context.ReadInt32();
        Remapped = context.ReadBoolean(wide: true);

        int numVerts = context.ReadInt32();
        int numNormals = context.ReadInt32();
        int numTris = context.ReadInt32();
        int numWaypoints = context.ReadInt32();
        int numRacingLines = context.ReadInt32();
        int numTrackMarkers = context.ReadInt32();
        int numTrafficRoutes = context.ReadInt32();
        int numTrafficSpawn = context.ReadInt32();
        int numBlockers = context.ReadInt32();
        int numErrors = context.ReadInt32();
        int numSpatialGroups = context.ReadInt32();

        int numStarts = 0;
        if (context.Version >= 0x9) numStarts = context.ReadInt32();
        else context.ReadInt32();
        
        Console.WriteLine($"Num Waypoints: {numWaypoints}");
        Console.WriteLine($"Num Racing Lines: {numRacingLines}");
        Console.WriteLine($"Num Track Markers: {numTrackMarkers}");
        Console.WriteLine($"Num Spatial Groups:  {numSpatialGroups}");
        
        context.ReadInt32(); // ???
        
        Vertices = context.LoadArrayPointer(numVerts, context.ReadAlignedFloat3);
        Normals = context.LoadArrayPointer(numNormals, context.ReadAlignedFloat3);

        context.ReadPointer(); // tris
        Waypoints = context.LoadArrayPointer<NavWaypoint>(numWaypoints);
        RacingLines = context.LoadArrayPointer<NavRacingLine>(numRacingLines);
        TrackMarkers = context.LoadArrayPointer<NavTrackMarker>(numTrackMarkers);
        context.ReadPointer(); // traffic routes
        context.ReadPointer(); // traffic spawn
        context.ReadPointer(); // blockers
        context.ReadPointer(); // errors
        SpatialGroups = context.LoadArrayPointer<NavSpatialGroup>(numSpatialGroups);
        context.ReadPointer(); // nav start

        TotalTrackDist = context.ReadFloat();
        LowestPoint = context.ReadFloat();
        context.Position += 0x14; // Basically the track bounding box, seems to always just be 0 when serialized, so point in storing it.
        HighestPoint = context.ReadFloat();
        
        // fog/bloom/exposure hashes after this, are those used?
        
        context.Base = link;
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xe0;
    }
}