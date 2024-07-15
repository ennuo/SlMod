using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuCollisionMesh : IResourceSerializable
{
    public Matrix4x4 ObbTransform;
    public Vector4 Extents;
    public Vector4 Sphere;
    public int Hash;
    public short Type;
    public short BranchIndex = -1;
    public SuBlindData? BlindData;
    public List<SuCollisionTriangle> Triangles = [];
    
    public void Load(ResourceLoadContext context)
    {
        ObbTransform = context.ReadMatrix();
        Extents = context.ReadFloat4();
        Sphere = context.ReadFloat4();
        Hash = context.ReadInt32();
        Type = context.ReadInt16();
        BranchIndex = context.ReadInt16();
        int numTriangles = context.ReadInt32();
        BlindData = context.LoadPointer<SuBlindData>();
        Triangles = context.LoadArrayPointer<SuCollisionTriangle>(numTriangles);
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteMatrix(buffer, ObbTransform, 0x0);
        context.WriteFloat4(buffer, Extents, 0x40);
        context.WriteFloat4(buffer, Sphere, 0x50);
        context.WriteInt32(buffer, Hash, 0x60);
        context.WriteInt16(buffer, Type, 0x64);
        context.WriteInt16(buffer, BranchIndex, 0x68);
        context.WriteInt32(buffer, Triangles.Count, 0x6c);
        context.SavePointer(buffer, BlindData, 0x70);;
        context.SaveReferenceArray(buffer, Triangles, 0x74, align: 0x10);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x78;
    }
}