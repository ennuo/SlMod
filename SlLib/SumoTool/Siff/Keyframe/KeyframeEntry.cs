using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff.Keyframe;

public class KeyframeEntry : IResourceSerializable
{
    public int Hash;
    public List<KeyframeData> Data = [];
    public short Type;
    public float Width = 1.0f;
    public float Height = 1.0f;

    public KeyframeEntry Duplicate(int hash)
    {
        var saveContext = new ResourceSaveContext();
        ISaveBuffer buffer =
            saveContext.Allocate(GetSizeForSerialization(SlPlatform.Win32, SlPlatform.Win32.DefaultVersion));
        saveContext.SaveObject(buffer, this, 0);
        (byte[] cpuData, byte[] _) = saveContext.Flush();

        var loadContext = new ResourceLoadContext(cpuData);
        var entry = loadContext.LoadObject<KeyframeEntry>();
        entry.Hash = hash;

        return entry;
    }
    
    public KeyframeData? GetKeyFrame(string name)
    {
        int hash = SlUtil.SumoHash(name);
        return Data.Find(kfrm => kfrm.Hash == hash);
    }
    
    public void Load(ResourceLoadContext context)
    {
        Hash = context.ReadInt32();

        // I assume this got moved above hash due to type
        // alignment rules in 64-bit versions, but have to verify.
        int keyframeData = context.ReadPointer();
        
        int frames = context.ReadInt16();
        Type = context.ReadInt16();
        Data = Type switch
        {
            0 => context.LoadArray<KeyframeData>(keyframeData, frames),
            1 => context.LoadArray<ParticleKeyFrameData>(keyframeData, frames).Cast<KeyframeData>().ToList(),
            2 => context.LoadArray<GouraudKeyFrameData>(keyframeData, frames).Cast<KeyframeData>().ToList(),
            _ => throw new SerializationException($"Unsupported keyframe type: {Type}")
        };
        
        Width = context.ReadFloat();
        Height = context.ReadFloat();
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Hash, 0x0);
        context.WriteInt16(buffer, (short)Data.Count, 0x8);
        context.SaveObjectArray(buffer, Data, 0x4);
        context.WriteInt16(buffer, Type, 0xa);
        context.WriteFloat(buffer, Width, 0xc);
        context.WriteFloat(buffer, Height, 0x10);
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x18 : 0x14;
    }
}