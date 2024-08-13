using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlCollisionResourceLeafNode : IResourceSerializable
{
    public SlResourceMeshDataSingleTriangleFloat Data;
    
    public void Load(ResourceLoadContext context)
    {
        context.ReadInt32(); // Always 1?
        Data = context.LoadPointer<SlResourceMeshDataSingleTriangleFloat>() ??
               throw new SerializationException("Triangle data cannot be NULL!");
        context.ReadInt32(); // Always 0?
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, 1, 0x0);
        context.SavePointer(buffer, Data, 0x4);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}