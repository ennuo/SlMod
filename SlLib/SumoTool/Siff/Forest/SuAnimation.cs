using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuAnimation : IResourceSerializable
{
    // 0 = SuSimpleAnimation
    // 1 = SuCubicAnimation16
    // 2 = SuCubicAnimation8
    // 3 = SuCubicAnimation16Reduced
    // 4 = SuCubicAnimation16Reduced2
    // 5 = ???
    // 6 = SuCubicAnimationReduced2Quantized<ushort, ushort>
    // 7 = SuCubicAnimationReduced2Quantized<byte, ushort>
    // 8 = SuCubicAnimationReduced2Quantized<ushort, float>
    // 9 = SuCubicAnimationReduced2Quantized<ushort, byte>
    // 10 = SuCubicAnimationReduced2Quantized<byte, byte>
    
    // most common type seems to be 6
    public int Type;
    public int NumFrames;
    public int NumBones;
    public int NumUvBones;
    public int NumFloatStreams;
    public List<Vector4> ParamData;
    
    public void Load(ResourceLoadContext context)
    {
        Type = context.ReadInt32();
        NumFrames = context.ReadInt32();
        NumBones = context.ReadInt32();
        NumUvBones = context.ReadInt32();
        NumFloatStreams = context.ReadInt32();
        //ParamData = context.ReadInt32();
        
        //Console.WriteLine($"0x{(context._data.Offset + context.Position):x8} ({Type}) (ParamData = 0x{(context._data.Offset + context.ReadPointer()):x8}");
        
        
        
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0;
    }
}