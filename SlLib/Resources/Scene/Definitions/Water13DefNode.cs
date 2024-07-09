using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class Water13DefNode : SeDefinitionTransformNode
{
    public int WaterSimulationHash;
    public int WaterRenderableHash;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        WaterSimulationHash = context.ReadInt32(0xd0);
        WaterRenderableHash = context.ReadInt32(0xd4);
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        context.WriteInt32(buffer, WaterSimulationHash, 0xd0);
        context.WriteInt32(buffer, WaterRenderableHash, 0xd4);
        
        context.WriteInt32(buffer, -0x4FF4E1AB, 0xe8);
        context.WriteInt32(buffer, -0x4FF4E1AB, 0xec);
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xf0;
}