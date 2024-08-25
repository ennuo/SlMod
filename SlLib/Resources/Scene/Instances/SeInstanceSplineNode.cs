using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceSplineNode : SeInstanceTransformNode
{
    public int SplineFlags;
    public ArraySegment<byte> Data = ArraySegment<byte>.Empty;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        SplineFlags = context.ReadInt32(0x180); // looped @  (1 << 0)
        //Looped = context.ReadBoolean(0x180);

        int numSplinePoints = context.ReadInt32(0x160);
        int splinePointData = context.ReadInt32(0x170);
        Data = context.LoadBuffer(splinePointData, numSplinePoints * 0x40, false);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, SplineFlags, 0x180);
        if (Data.Count != 0)
        {
            int numSplinePoints = Data.Count / 0x40;
            context.SaveBufferPointer(buffer, Data, 0x170, align: 16);
            context.WriteInt32(buffer, numSplinePoints, 0x160);
            context.WriteInt32(buffer, numSplinePoints, 0x164);
        }
        
        context.WriteInt16(buffer, -1, 0x186);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x190;
}