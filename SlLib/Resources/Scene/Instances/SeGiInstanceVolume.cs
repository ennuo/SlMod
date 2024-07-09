using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeGiInstanceVolume : SeInstanceTransformNode
{
    public float Width;
    public float Height;
    public float Depth;
    public float GridSpacing;
    
    public ArraySegment<byte> SampleData = ArraySegment<byte>.Empty;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Width = context.ReadFloat(0x190);
        Height = context.ReadFloat(0x194);
        Depth = context.ReadFloat(0x198);
        GridSpacing = context.ReadFloat(0x19c);

        int numSamples = context.ReadInt32(0x1b0);
        SampleData = context.LoadBuffer(context.ReadInt32(0x1c0), numSamples * 0x30, false);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Width, 0x190);
        context.WriteFloat(buffer, Height, 0x194);
        context.WriteFloat(buffer, Depth, 0x198);
        context.WriteFloat(buffer, GridSpacing, 0x19c); ;
        
        if (SampleData.Count != 0)
        {
            int numSamples = SampleData.Count / 0x30;
            context.WriteInt32(buffer, numSamples, 0x1b0);
            context.WriteInt32(buffer, numSamples, 0x1b4);
            context.SaveBufferPointer(buffer, SampleData, 0x1c0);
            context.WriteInt32(buffer, 0x20000, 0x1b8);
        }
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1d0;
}