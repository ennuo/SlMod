using System.Numerics;
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
        int numStarts = context.Version >= 0x9 ? context.ReadInt32() : 0;
        context.ReadInt32(); // ???
        
        Vertices = context.LoadArrayPointer(numVerts, context.ReadPaddedFloat3);
        Normals = context.LoadArrayPointer(numNormals, context.ReadPaddedFloat3);

        context.ReadPointer(); // tris
        Waypoints = context.LoadArrayPointer<NavWaypoint>(numWaypoints);
        
        
        context.Base = link;
    }
}