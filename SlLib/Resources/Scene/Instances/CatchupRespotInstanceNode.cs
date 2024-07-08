using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class CatchupRespotInstanceNode : SeInstanceTransformNode, IResourceSerializable
{
    public int Lap;
    public int DriveMode;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        Lap = context.ReadInt32(0x160);
        DriveMode = context.ReadInt32(0x164);
    }
}