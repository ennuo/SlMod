using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleReferenceNode : SeDefinitionTransformNode
{
    // SeInstanceParticleSystemNode
    public string ReferenceSystemName = string.Empty;
    public SeNodeBase? ReferenceSystem;

    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        ReferenceSystemName = context.ReadStringPointer(0xd0);
        ReferenceSystem = context.LoadNode(context.ReadInt32(0xf8));
    }

    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        context.WriteStringPointer(buffer, ReferenceSystemName, 0xd0);
        context.WriteInt32(buffer, ReferenceSystem?.Uid ?? 0, 0xf8);
    }

    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x100;
}