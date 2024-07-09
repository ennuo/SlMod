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

    public float ClampedWidth;
    public float ClampedHeight;
    public float ClampedDepth;
    public float ClampedGridSpacing;
    
    public ArraySegment<byte> SampleData = ArraySegment<byte>.Empty;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        Width = context.ReadFloat(0x190);
        Height = context.ReadFloat(0x194);
        Depth = context.ReadFloat(0x198);
        GridSpacing = context.ReadFloat(0x19c);

        ClampedWidth = context.ReadFloat(0x1a0);
        ClampedHeight = context.ReadFloat(0x1a4);
        ClampedDepth = context.ReadFloat(0x1a8);
        ClampedGridSpacing = context.ReadFloat(0x1ac);

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
        context.WriteFloat(buffer, GridSpacing, 0x19c);
        
        context.WriteFloat(buffer, ClampedWidth, 0x1a0);
        context.WriteFloat(buffer, ClampedHeight, 0x1a4);
        context.WriteFloat(buffer, ClampedDepth, 0x1a8);
        context.WriteFloat(buffer, ClampedGridSpacing, 0x1ac);

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