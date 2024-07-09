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
    
    
    private List<Matrix4x4> Samples = [];
    

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        Width = context.ReadFloat(0x190);
        Height = context.ReadFloat(0x194);
        Depth = context.ReadFloat(0x198);
        GridSpacing = context.ReadFloat(0x19c);
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteFloat(buffer, Width, 0x190);
        context.WriteFloat(buffer, Height, 0x194);
        context.WriteFloat(buffer, Depth, 0x198);
        context.WriteFloat(buffer, GridSpacing, 0x19c);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x1d0;
}