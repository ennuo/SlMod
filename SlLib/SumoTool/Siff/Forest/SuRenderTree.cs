using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderTree : IResourceSerializable
{
    public SuBlindData? BlindData;
    public int Hash;
    public List<SuBranch> Branches = [];
    public List<Vector4> Translations = [];
    public List<Vector4> Rotations = [];
    public List<Vector4> Scales = [];
    public List<SuCollisionMesh> CollisionMeshes = [];
    public List<SuLightData> Lights = [];
    public List<SuCameraData> Cameras = [];
    public List<SuEmitterData> Emitters = [];
    public List<SuCurve> Curves = [];
    public List<SuTextureTransform> DefaultTextureTransforms = [];
    public List<SuAnimationEntry> AnimationEntries = [];
    public List<float> DefaultAnimationFloats = [];
    
    public void Load(ResourceLoadContext context)
    {
        BlindData = context.LoadPointer<SuBlindData>();
        Hash = context.ReadInt32();

        int numBranches = context.ReadInt32();
        
        Branches = context.LoadPointerArray<SuBranch>(numBranches);
        Translations = context.LoadArrayPointer(numBranches, context.ReadFloat4);
        Rotations = context.LoadArrayPointer(numBranches, context.ReadFloat4);
        Scales = context.LoadArrayPointer(numBranches, context.ReadFloat4);

        int numTextureMatrices = context.ReadInt32();
        
        CollisionMeshes = context.LoadPointerArray<SuCollisionMesh>(context.ReadInt32());
        Lights = context.LoadPointerArray<SuLightData>(context.ReadInt32());
        Cameras = context.LoadPointerArray<SuCameraData>(context.ReadInt32());
        Emitters = context.LoadPointerArray<SuEmitterData>(context.ReadInt32());
        Curves = context.LoadPointerArray<SuCurve>(context.ReadInt32());
        
        DefaultTextureTransforms = context.LoadArrayPointer<SuTextureTransform>(numTextureMatrices);
        AnimationEntries = context.LoadArrayPointer<SuAnimationEntry>(context.ReadInt32());
        DefaultAnimationFloats = context.LoadArrayPointer(context.ReadInt32(), context.ReadFloat);
        
        if (context.ReadInt32() != 0) throw new SerializationException("Stream overrides are unsupported!");
        context.ReadPointer();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SavePointer(buffer, BlindData, 0x0);
        context.WriteInt32(buffer, Hash, 0x4);
        context.WriteInt32(buffer, Branches.Count, 0x8);
        
        ISaveBuffer treeBranchPointerData = context.SaveGenericPointer(buffer, 0xc, Branches.Count * 0x4);
        for (int i = 0; i < Branches.Count; ++i)
        {
            SuBranch branch = Branches[i];
            int align = branch.Lod != null ? 0x10 : 0x4;
            context.SavePointer(treeBranchPointerData, branch, i * 4, align);
        }
        
        
        ISaveBuffer translationData = context.SaveGenericPointer(buffer, 0x10, Branches.Count * 0x10, align: 0x10);
        ISaveBuffer rotationData = context.SaveGenericPointer(buffer, 0x14, Branches.Count * 0x10, align: 0x10);
        ISaveBuffer scaleData = context.SaveGenericPointer(buffer, 0x18, Branches.Count * 0x10, align: 0x10);
        context.WriteInt32(buffer, DefaultTextureTransforms.Count, 0x1c);
        context.SaveReferenceArray(buffer, DefaultTextureTransforms, 0x48);
        for (int i = 0; i < Branches.Count; ++i)
        {
            context.WriteFloat4(translationData, Translations[i], i * 0x10);
            context.WriteFloat4(rotationData, Rotations[i], i * 0x10);
            context.WriteFloat4(scaleData, Scales[i], i * 0x10);
        }
        
        
        context.FlushDeferredPointersOfType<SuRenderMesh>();
        
        context.WriteInt32(buffer, CollisionMeshes.Count, 0x20);
        context.SavePointerArray(buffer, CollisionMeshes, 0x24, elementAlignment: 0x10);
        context.WriteInt32(buffer, Lights.Count, 0x28);
        context.SavePointerArray(buffer, Lights, 0x2c, elementAlignment: 0x10);
        context.WriteInt32(buffer, Cameras.Count, 0x30);
        context.SavePointerArray(buffer, Cameras, 0x34, elementAlignment: 0x10);
        context.WriteInt32(buffer, Emitters.Count, 0x38);
        context.SavePointerArray(buffer, Emitters, 0x3c, elementAlignment: 0x10);
        context.WriteInt32(buffer, Curves.Count, 0x40);
        context.SavePointerArray(buffer, Curves, 0x44);
        context.WriteInt32(buffer, AnimationEntries.Count, 0x4c);
        context.SaveReferenceArray(buffer, AnimationEntries, 0x50);

        context.WriteInt32(buffer, DefaultAnimationFloats.Count, 0x54);
        ISaveBuffer floatData = context.SaveGenericPointer(buffer, 0x58, DefaultAnimationFloats.Count * 4);
        for (int i = 0; i < DefaultAnimationFloats.Count; ++i)
            context.WriteFloat(floatData, DefaultAnimationFloats[i], i * 4);
        
        context.FlushDeferredPointersOfType<SuRenderVertexStream.VertexStreamHashes>();
        
        return;
        
        // context.SavePointer(buffer, BlindData, 0x0);
        // context.WriteInt32(buffer, Hash, 0x4);
        // context.WriteInt32(buffer, Branches.Count, 0x8);
        // context.SavePointerArray(buffer, Branches, 0xc);
        //
        //
        // ISaveBuffer translationData = context.SaveGenericPointer(buffer, 0x10, Branches.Count * 0x10, align: 0x10);
        // ISaveBuffer rotationData = context.SaveGenericPointer(buffer, 0x14, Branches.Count * 0x10, align: 0x10);
        // ISaveBuffer scaleData = context.SaveGenericPointer(buffer, 0x18, Branches.Count * 0x10, align: 0x10);
        //
        // for (int i = 0; i < Branches.Count; ++i)
        // {
        //     context.WriteFloat4(translationData, Translations[i], i * 0x10);
        //     context.WriteFloat4(rotationData, Rotations[i], i * 0x10);
        //     context.WriteFloat4(scaleData, Scales[i], i * 0x10);
        // }
        //
        // context.WriteInt32(buffer, DefaultTextureTransforms.Count, 0x1c);
        // context.WriteInt32(buffer, CollisionMeshes.Count, 0x20);
        // context.SavePointerArray(buffer, CollisionMeshes, 0x24, align: 0x10);
        // context.WriteInt32(buffer, Lights.Count, 0x28);
        // context.SavePointerArray(buffer, Lights, 0x2c, align: 0x10);
        // context.WriteInt32(buffer, Cameras.Count, 0x30);
        // context.SavePointerArray(buffer, Cameras, 0x34);
        // context.WriteInt32(buffer, Emitters.Count, 0x38);
        // context.SavePointerArray(buffer, Emitters, 0x3c, align: 0x10);
        // context.WriteInt32(buffer, Curves.Count, 0x40);
        // context.SavePointerArray(buffer, Curves, 0x44);
        // context.SaveReferenceArray(buffer, DefaultTextureTransforms, 0x48);
        //
        // // anim stuff goes here, scary...
        //
        // context.WriteInt32(buffer, DefaultAnimationFloats.Count, 0x54);
        // ISaveBuffer treeHashData = context.SaveGenericPointer(buffer, 0x58, DefaultAnimationFloats.Count * 4);
        // for (int i = 0; i < DefaultAnimationFloats.Count; ++i)
        //     context.WriteFloat(treeHashData, DefaultAnimationFloats[i], i * 0x4);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x64;
    }
}