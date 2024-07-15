﻿using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderMesh : IResourceSerializable
{
    public Vector4 BoundingSphere;
    public List<SuRenderPrimitive> Primitives = [];
    public List<int> BoneMatrixIndices = [];
    public List<Matrix4x4> BoneInverseMatrices = [];
    public string Name = string.Empty;
    
    public void Load(ResourceLoadContext context)
    {
        BoundingSphere = context.ReadFloat4();
        Primitives = context.LoadPointerArray<SuRenderPrimitive>(context.ReadInt32());

        int numBones = context.ReadInt32();
        BoneMatrixIndices = context.LoadArrayPointer(numBones, context.ReadInt32);
        BoneInverseMatrices = context.LoadArrayPointer(numBones, context.ReadMatrix);
        Name = context.ReadStringPointer();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat4(buffer, BoundingSphere, 0x0);
        context.WriteInt32(buffer, Primitives.Count, 0x10);
        context.SavePointerArray(buffer, Primitives, 0x14, align: 0x40);

        int numBones = BoneMatrixIndices.Count;
        if (numBones != BoneInverseMatrices.Count)
            throw new SerializationException("Bone matrix indices and inverse arrays count must be the same!");
        
        context.WriteInt32(buffer, numBones, 0x18);
        if (numBones != 0)
        {
            ISaveBuffer indexData = context.SaveGenericPointer(buffer, 0x1c, numBones * 4, align: 0x4);
            ISaveBuffer matrixData = context.SaveGenericPointer(buffer, 0x20, numBones * 0x40, align: 0x40);
            for (int i = 0; i < numBones; ++i)
            {
                context.WriteMatrix(matrixData, BoneInverseMatrices[i], i * 0x40);
                context.WriteInt32(indexData, BoneMatrixIndices[i], i * 0x4);
            }
        }
        
        context.WriteStringPointer(buffer, Name, 0x24);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x28;
    }
}