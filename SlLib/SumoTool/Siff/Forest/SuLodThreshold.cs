using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuLodThreshold : IResourceSerializable
{
    public float ThresholdDistance;
    public SuRenderMesh? Mesh;
    public short ChildBranch = -1;
    
    public void Load(ResourceLoadContext context)
    {
        ThresholdDistance = context.ReadFloat();
        Mesh = context.LoadPointer<SuRenderMesh>();
        ChildBranch = context.ReadInt16();
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteFloat(buffer, ThresholdDistance, 0x0);
        context.SavePointer(buffer, Mesh, 0x4, align: 0x10, deferred: true);
        context.WriteInt16(buffer, ChildBranch, 0x8);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0xc;
    }
}