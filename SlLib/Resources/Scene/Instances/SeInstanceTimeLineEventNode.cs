using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceTimeLineEventNode : SeInstanceTimeLineEventNodeBase, IResourceSerializable
{
    public SeNodeBase? StartRecipient;
    public SeNodeBase? EndRecipient;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        StartRecipient = context.LoadNode(context.ReadInt32(0xa8));
        EndRecipient = context.LoadNode(context.ReadInt32(0xac));
    }

}