using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlResourceMeshDataSingleTriangleFloat : IResourceSerializable
{
    public Vector3 Center;
    public Vector3 Max;
    public Vector3 Min;
    
    public Vector3 A;
    public Vector3 B;
    public Vector3 C;
    public int CollisionMaterialIndex;
    
    public void Load(ResourceLoadContext context)
    {
        A = context.ReadFloat3();
        B = context.ReadFloat3();
        C = context.ReadFloat3();

        Center = (A + B + C) / 3.0f;
        Max = Vector3.Max(A, Vector3.Max(B, C));
        Min = Vector3.Min(A, Vector3.Min(B, C));
        
        CollisionMaterialIndex = context.ReadInt32();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat3(buffer, A, 0x0);
        context.WriteFloat3(buffer, B, 0xC);
        context.WriteFloat3(buffer, C, 0x18);
        context.WriteInt32(buffer, CollisionMaterialIndex, 0x24);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x28;
    }
}