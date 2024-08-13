using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Collision;

public class SlResourceMeshSection : IResourceSerializable
{
    public int Type = 2;
    public int Roots;
    public List<SlCollisionResourceBranchNode> Branches = [];
    public List<SlCollisionResourceLeafNode> Leafs = [];
    
    public void Load(ResourceLoadContext context)
    {
        Type = context.ReadInt32();
        if (Type != 2)
            throw new SerializationException("Only single triangle collision mesh data is supported!");
        
        int numBranches = context.ReadInt32();
        Roots = context.ReadInt32();
        Branches = context.LoadArrayPointer<SlCollisionResourceBranchNode>(numBranches);
        Leafs = context.LoadArrayPointer<SlCollisionResourceLeafNode>(context.ReadInt32());
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // Sneak in some buffer, not sure if it's actually used,
        // but it's there, so. This code is probably prone to break
        // if the serializer settings are changed because it's not directly
        // referenced by anything.
        ISaveBuffer sneakyData = context.Allocate(8, align: 1);
        context.WriteInt32(sneakyData, Branches.Count, 0x0);
        context.WriteInt32(sneakyData, Roots, 0x4);
        
        context.WriteInt32(buffer, Type, 0x0);
        context.WriteInt32(buffer, Branches.Count, 0x4);
        context.WriteInt32(buffer, Roots, 0x8);
        context.WriteInt32(buffer, Leafs.Count, 0x10);
        
        context.SaveReferenceArray(buffer, Branches, 0xc, align: 0x10);
        context.SaveReferenceArray(buffer, Leafs, 0x14);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x18;
    }
}