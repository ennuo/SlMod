using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Entry;

namespace SlLib.SumoTool.Siff;

public class SceneLibrary : IResourceSerializable
{
    public List<SceneTableEntry> Scenes = [];
    
    public void Load(ResourceLoadContext context)
    {
        int numScenes = context.ReadInt32();
        for (int i = 0; i < numScenes; ++i)
        {
            var scene = new SceneTableEntry(context.ReadInt32());
            int numObjects = context.ReadInt32();
            // scene.WidescreenFlag = context.ReadInt32();
            scene.Index = context.ReadInt32();
            int objectData = context.ReadPointer();
            scene.Width = context.ReadFloat();
            scene.Height = context.ReadFloat();
            
            for (int j = 0; j < numObjects; ++j)
                scene.Objects.Add(context.ReadInt32(objectData + (j * 4)));
            
            Scenes.Add(scene);
        }
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Scenes.Count, 0x0);
        for (int i = 0; i < Scenes.Count; ++i)
        {
            SceneTableEntry scene = Scenes[i];
            int address = 0x4 + (i * 0x18);
            context.WriteInt32(buffer, scene.Hash, address);
            context.WriteInt32(buffer, scene.Objects.Count, address + 0x4);
            // context.WriteInt32(buffer, scene.WidescreenFlag, address + 0x8);
            context.WriteInt32(buffer, scene.Index, address + 0x8);
            ISaveBuffer objectData = context.SaveGenericPointer(buffer, address + 0xc, scene.Objects.Count * 0x4);
            for (int j = 0; j < scene.Objects.Count; ++j)
                context.WriteInt32(objectData, scene.Objects[j], j * 0x4);
            context.WriteFloat(buffer, scene.Width, address + 0x10);
            context.WriteFloat(buffer, scene.Height, address + 0x14);
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        if (platform.Is64Bit) return 0x4 + (0x20 * Scenes.Count);
        return 0x4 + (0x18 * Scenes.Count);
    }
}