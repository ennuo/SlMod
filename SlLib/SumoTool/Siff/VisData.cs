using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Visibility;

namespace SlLib.SumoTool.Siff;

public class VisData : IResourceSerializable
{
    public int NumStaticTrees;
    public int NumSkydomeTrees;
    public int NumAnimatedTrees;
    public int NumTrees;
    
    public List<int> TreeHashes = [];
    public List<Volume> CameraVolumes = [];
    public List<Volume> ItemVolumes = [];
    
    public List<int> VisDataOffsets = [];
    public List<short> Data = [];
    
    public void Load(ResourceLoadContext context)
    {
        NumStaticTrees = context.ReadInt32();
        NumSkydomeTrees = context.ReadInt32();
        NumAnimatedTrees = context.ReadInt32();

        NumTrees = context.ReadInt32();
        
        // now the fuck does this work
        int volumeVisDataPointers = context.ReadPointer();
        
        int treeHashData = context.ReadPointer();
        TreeHashes = context.LoadArray(treeHashData, NumTrees, context.ReadInt32);
        CameraVolumes = context.LoadArrayPointer<Volume>(context.ReadInt32());
        ItemVolumes = context.LoadArrayPointer<Volume>(context.ReadInt32());

        // dont feel like figuring out how this data works, so we'll be doing a cheap hack
        int numVisDataPointers = (CameraVolumes.Count * 4) + 4;
        VisDataOffsets = context.LoadArray(volumeVisDataPointers, numVisDataPointers, context.ReadPointer);
        
        
        // should always be consistent on base game files
        int start = volumeVisDataPointers + (numVisDataPointers * 4);
        for (int i = 0; i < VisDataOffsets.Count; ++i)
            VisDataOffsets[i] -= start;
        
        Data = context.LoadArray(start, (treeHashData - start) / 0x2, context.ReadInt16);
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NumStaticTrees, 0x0);
        context.WriteInt32(buffer, NumSkydomeTrees, 0x4);
        context.WriteInt32(buffer, NumAnimatedTrees, 0x8);
        context.WriteInt32(buffer, NumTrees, 0xc);
        
        context.WriteInt32(buffer, CameraVolumes.Count, 0x18);
        context.SaveReferenceArray(buffer, CameraVolumes, 0x1c, align: 0x10);
        context.WriteInt32(buffer, ItemVolumes.Count, 0x20);
        context.SaveReferenceArray(buffer, ItemVolumes, 0x24, align: 0x10);

        ISaveBuffer volumeVisDataPointers = context.SaveGenericPointer(buffer, 0x10, VisDataOffsets.Count * 4);
        ISaveBuffer volumeVisData = context.Allocate(Data.Count * 0x2);
        
        for (int i = 0; i < VisDataOffsets.Count; ++i)
            context.WritePointerAtOffset(volumeVisDataPointers, i * 4, volumeVisData.Address + VisDataOffsets[i]);
        
        for (int i = 0; i < Data.Count; ++i)
            context.WriteInt16(volumeVisData, Data[i], i * 0x2);

        ISaveBuffer treeHashes = context.SaveGenericPointer(buffer, 0x14, NumTrees * 0x4);
        for (int i = 0; i < NumTrees; ++i)
            context.WriteInt32(treeHashes, TreeHashes[i], i * 4);
        
        // None of the data here is actually considered pointers,
        // just offsets relative to the start of the data.
        context.Relocations.Clear();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x28;
    }
}