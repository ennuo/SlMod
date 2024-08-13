using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Keyframe;

public class GouraudKeyFrameData : KeyframeData
{
    public KeyframeRgbaData RgbaData;
    
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        // should probably switch operations like these to a copy
        for (int i = 0; i < 16; ++i)
            RgbaData[i] = context.ReadInt8();
    }
    
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        for (int i = 0; i < 16; ++i)
            context.WriteInt8(buffer, RgbaData[i], 0x28 + i);
    }
    
    public override int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x38;
    }
}