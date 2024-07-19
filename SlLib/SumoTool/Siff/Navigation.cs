using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.NavData;

namespace SlLib.SumoTool.Siff;

public class Navigation : IResourceSerializable
{
    public int NameHash;
    public int Version;
    
    public bool Remapped;
    public List<Vector3> Vertices = [];
    public List<Vector3> Normals = [];
    public List<NavWaypoint> Waypoints = [];
    public List<NavRacingLine> RacingLines = [];
    public List<NavTrackMarker> TrackMarkers = [];
    public List<NavSpatialGroup> SpatialGroups = [];

    public float TotalTrackDist;
    public float LowestPoint;

    public float TrackBottomLeftX, TrackBottomLeftZ;
    public float TrackTopRightX, TrackTopRightZ;
    
    public float HighestPoint;
    
    public int[] SettingsFogNameHashes = new int[4];
    public int[] SettingsBloomNameHashes = new int[4];
    public int[] SettingsExposureNameHashes = new int[4];
    
    public void Load(ResourceLoadContext context)
    {
        int link = context.Base;
        context.Base = context.Position;
        
        NameHash = context.ReadInt32();
        Version = context.ReadInt32();
        context.Version = Version;
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
        
        // these are all we need for the doomegg
        // waypoints
        // racing lines
        // track markers

        int numStarts = 0;
        if (Version >= 0x9)
        {
            numStarts = context.ReadInt32();
            context.ReadInt32();
        }
        
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
        if (Version >= 0x9)
            context.ReadPointer(); // nav start

        TotalTrackDist = context.ReadFloat();
        LowestPoint = context.ReadFloat();
        TrackBottomLeftX = context.ReadFloat();
        TrackBottomLeftZ = context.ReadFloat();
        TrackTopRightX = context.ReadFloat();
        TrackTopRightZ = context.ReadFloat();
        context.ReadPointer(); // 3d racing lines, shouldnt be saved ever
        HighestPoint = context.ReadFloat();

        for (int i = 0; i < 4; ++i) SettingsFogNameHashes[i] = context.ReadInt32();
        for (int i = 0; i < 4; ++i) SettingsBloomNameHashes[i] = context.ReadInt32();
        for (int i = 0; i < 4; ++i) SettingsExposureNameHashes[i] = context.ReadInt32();
        
        context.Base = link;
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NameHash, 0x0);
        context.WriteInt32(buffer, Version, 0x4);
        context.WriteBoolean(buffer, Remapped, 0x8, wide: true);
        
        // TODO: Clean up and complete rest of fields, also account for version
        
        
        
        
        context.WriteInt32(buffer, Waypoints.Count, 0x18);
        context.WriteInt32(buffer, RacingLines.Count, 0x1c);
        context.WriteInt32(buffer, TrackMarkers.Count, 0x20);

        context.SaveReferenceArray(buffer, Waypoints, 0x44, align: 0x10);
        context.SaveReferenceArray(buffer, RacingLines, 0x48, align: 0x10);
        context.SaveReferenceArray(buffer, TrackMarkers, 0x4c, align: 0x10);
        
        context.WriteFloat(buffer, TotalTrackDist, 0x64);
        context.WriteFloat(buffer, LowestPoint, 0x68);
        context.WriteFloat(buffer, TrackBottomLeftX, 0x6c);
        context.WriteFloat(buffer, TrackBottomLeftZ, 0x70);
        context.WriteFloat(buffer, TrackTopRightX, 0x74);
        context.WriteFloat(buffer, TrackTopRightZ, 0x78);
        
        context.WriteFloat(buffer, HighestPoint, 0x80);
        for (int i = 0; i < 4; ++i)
        {
            context.WriteInt32(buffer, SettingsFogNameHashes[i], 0x84 + (i * 4));
            context.WriteInt32(buffer, SettingsBloomNameHashes[i], 0x94 + (i * 4));
            context.WriteInt32(buffer, SettingsExposureNameHashes[i], 0xa4 + (i * 4));
        }
        
        // no relocations, just offsets from beginning of the data
        context.FlushDeferredPointers();
        context.Relocations.Clear();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return Version > 0x8 ? 0xe0 : 0xd4;
    }
}