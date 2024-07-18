using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuLodBranch : IResourceSerializable
{
    public List<SuLodThreshold> Thresholds = [];
    public Matrix4x4 ObbTransform = Matrix4x4.Identity;
    public Vector4 Extents;
    
    public void Load(ResourceLoadContext context)
    {
        Thresholds = context.LoadArrayPointer<SuLodThreshold>(context.ReadInt32());
        context.Position += 4; // pad
        ObbTransform = context.ReadMatrix();
        Extents = context.ReadFloat4();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Thresholds.Count, 0x0);
        context.SaveReferenceArray(buffer, Thresholds, 0x4);
        context.WriteMatrix(buffer, ObbTransform, 0xc);
        context.WriteFloat4(buffer, Extents, 0x4c);
        
        // // I did something dumb to keep data consistent,
        // // the pointer size is already available directly after
        // context.WritePointerAtOffset(buffer, 0x4, buffer.Address + 0x5c);
        // for (int i = 0; i < Thresholds.Count; ++i)
        //     context.SaveReference(buffer, Thresholds[i], 0x5c + (i * 0xc));
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        // technically a subclass of SuBranch, so subtract 0x14 bytes
        return (0x70 - 0x14);  // + Thresholds.Count * 0xc;
    }
}