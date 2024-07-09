using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Instances;

public class SeInstanceTimeLineEventNode : SeInstanceTimeLineEventNodeBase
{
    public SeNodeBase? StartRecipient;
    public SeNodeBase? EndRecipient;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        StartRecipient = context.LoadNode(context.ReadInt32(0xa8));
        EndRecipient = context.LoadNode(context.ReadInt32(0xac));
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteInt32(buffer, StartRecipient?.Uid ?? 0, 0xa8);
        context.WriteInt32(buffer, EndRecipient?.Uid ?? 0, 0xac);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xc0;

}