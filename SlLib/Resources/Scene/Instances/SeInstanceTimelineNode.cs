using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceTimelineNode : SeInstanceNode
{
    public float EndTime;
    public float PauseAt;
    public bool DisableAtEnd = true;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);

        EndTime = context.ReadFloat();
        PauseAt = context.ReadFloat();
        DisableAtEnd = context.ReadBoolean(0x88, wide: true);
        // int ???
    }
}