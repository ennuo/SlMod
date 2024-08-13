using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Keyframe;
using SlLib.Utilities;

namespace SlLib.SumoTool.Siff;

public class KeyframeLibrary : IResourceSerializable
{
    public List<KeyframeEntry> Keyframes = [];

    public KeyframeEntry? GetKeyframe(string path)
    {
        path = path.Replace("/", "\\").ToUpper();
        int hash = SlUtil.SumoHash(path);
        return Keyframes.Find(kfrm => kfrm.Hash == hash);
    }
    
    public KeyframeEntry? GetKeyframe(int hash)
    {
        return Keyframes.Find(kfrm => kfrm.Hash == hash);
    }
    
    public void Load(ResourceLoadContext context)
    {
        int numFrames = context.ReadInt32();
        for (int i = 0; i < numFrames; ++i)
            Keyframes.Add(context.LoadObject<KeyframeEntry>());
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // Fairly sure the function that grabs keyframes from this list assumes the list
        // is sorted in ascending order by unsigned UID, or else it'll fail to find the entries.
        Keyframes.Sort((a, z) =>
        {
            uint aHash = (uint)a.Hash;
            uint bHash = (uint)z.Hash;

            return aHash.CompareTo(bHash);
        });
        
        context.WriteInt32(buffer, Keyframes.Count, 0x0);
        int offset = 4;
        foreach (KeyframeEntry keyframe in Keyframes)
        {
            context.SaveObject(buffer, keyframe, offset);
            offset += 0x14;
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        int stride = platform.Is64Bit ? 0x18 : 0x14;
        return 0x4 + stride * Keyframes.Count;
    }
}