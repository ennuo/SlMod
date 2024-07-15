using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Forest;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff;

public class ForestLibrary : IResourceSerializable
{
    public List<SuRenderForest> Forests = [];
    
    public void Load(ResourceLoadContext context)
    {
        int numForests = context.ReadInt32();
        for (int i = 0; i < numForests; ++i)
        {
            int hash = context.ReadInt32();
            string name = context.ReadStringPointer();
            int forestData = context.ReadPointer();
            int gpuDataStart = context.ReadPointer();

            // Just going to do something a tad dumb where we create new contexts for each forest, since
            // it just makes things the most convenient.
            ResourceLoadContext subcontext = context.CreateSubContext(forestData, gpuDataStart);
            var forest = subcontext.LoadObject<SuRenderForest>();
            forest.Name = name;
            Forests.Add(forest);
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Forests.Count, 0x0);
        for (int i = 0; i < Forests.Count; ++i)
        {
            int offset = 0x4 + (0x10 * i);
            SuRenderForest forest = Forests[i];
            context.WriteInt32(buffer, SlUtil.SumoHash(forest.Name), offset);

            // All pointers for each forest are based at themselves, easiest way to deal with this is just adding a subcontext,
            // plus relocations for these aren't stored.
            var subcontext = new ResourceSaveContext
            {
                UseStringPool = true
            };
            ISaveBuffer subbuffer = subcontext.Allocate(forest.GetSizeForSerialization(context.Platform, context.Version));
            subcontext.SaveObject(subbuffer, forest, 0x0);
            (byte[] cpuData, byte[] gpuData) = subcontext.Flush();
            
            context.WriteStringPointer(buffer, forest.Name, offset + 4);
            context.SaveBufferPointer(buffer, cpuData, offset + 8, align: 0x40);
            context.SaveBufferPointer(buffer, gpuData, offset + 12, align: 0x40, gpu: true);
        }
        
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x4 + (0x10 * Forests.Count);
    }
}