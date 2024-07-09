using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionTimeLineEventNode : SeDefinitionTimeLineEventBaseNode
{
    public int StartMessageDestination;
    public int StartMessage;
    public SeNodeBase? StartRecipient;
    public int EndMessageDestination;
    public int EndMessage;
    public SeNodeBase? EndRecipient;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);

        StartMessageDestination = context.ReadInt32(0x90);
        StartMessage = context.ReadInt32(0x94);
        StartRecipient = context.LoadNode(context.ReadInt32(0x9c));
        EndMessageDestination = context.ReadInt32(0xb0);
        EndMessage = context.ReadInt32(0xb4);
        EndRecipient = context.LoadNode(context.ReadInt32(0xbc));
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteInt32(buffer, StartMessageDestination, 0x90);
        context.WriteInt32(buffer, StartMessage, 0x94);
        context.WriteInt32(buffer, StartRecipient?.Uid ?? 0, 0x9c);
        context.WriteInt32(buffer, EndMessageDestination, 0xb0);
        context.WriteInt32(buffer, EndMessage, 0xb4);
        context.WriteInt32(buffer, EndRecipient?.Uid ?? 0, 0xbc);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0xd0;
}
