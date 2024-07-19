using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Collision;

namespace SlLib.SumoTool.Siff;

public class CollisionMesh : IResourceSerializable
{
    public int NameHash;
    public int Version;
    
    public Vector3 Min;
    public Vector3 Max;
    
    public List<Vector4> Vertices = [];
    public List<Triangle> Triangles = [];
    public List<Vector4> TriangleNormals = [];
    public List<OctreeNode> OctreeNodes = [];
    public List<short> OctreeTriangleIndices = [];
    public string Name = string.Empty;
    
    public void Load(ResourceLoadContext context)
    {
        NameHash = context.ReadInt32();
        Version = context.ReadInt32();

        int numVertices = context.ReadInt32();
        int numTriangles = context.ReadInt32();
        int numOctreeNodes = context.ReadInt32();
        int numOctreeTriangleIndices = context.ReadInt32();

        Min = context.ReadFloat3();
        Max = context.ReadFloat3();

        Vertices = context.LoadArrayPointer(numVertices, context.ReadFloat4);
        Triangles = context.LoadArrayPointer<Triangle>(numTriangles);
        TriangleNormals = context.LoadArrayPointer(numTriangles, context.ReadFloat4);
        OctreeNodes = context.LoadArrayPointer<OctreeNode>(numOctreeNodes);
        OctreeTriangleIndices = context.LoadArrayPointer(numOctreeTriangleIndices, context.ReadInt16);
        Name = context.ReadStringPointer();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NameHash, 0x0);
        context.WriteInt32(buffer, Version, 0x4);
        
        context.WriteInt32(buffer, Vertices.Count, 0x8);
        context.WriteInt32(buffer, Triangles.Count, 0xc);
        context.WriteInt32(buffer, OctreeNodes.Count, 0x10);
        context.WriteInt32(buffer, OctreeTriangleIndices.Count, 0x14);
        
        context.WriteFloat3(buffer, Min, 0x18);
        context.WriteFloat3(buffer, Max, 0x24);

        ISaveBuffer vertexData = context.SaveGenericPointer(buffer, 0x30, Vertices.Count * 0x10, align: 0x10);
        for (int i = 0; i < Vertices.Count; ++i)
            context.WriteFloat4(vertexData, Vertices[i], i * 0x10);
        
        context.SaveReferenceArray(buffer, Triangles, 0x34);
        
        ISaveBuffer normalData = context.SaveGenericPointer(buffer, 0x38, TriangleNormals.Count * 0x10, align: 0x10);
        for (int i = 0; i < TriangleNormals.Count; ++i)
            context.WriteFloat4(normalData, TriangleNormals[i], i * 0x10);
        
        context.SaveReferenceArray(buffer, OctreeNodes, 0x3c);
        
        ISaveBuffer indexData = context.SaveGenericPointer(buffer, 0x40, OctreeTriangleIndices.Count * 0x2, align: 0x2);
        for (int i = 0; i < OctreeTriangleIndices.Count; ++i)
            context.WriteInt16(indexData, OctreeTriangleIndices[i], i * 0x2);
        
        context.WriteStringPointer(buffer, Name, 0x44);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x48;
    }
}