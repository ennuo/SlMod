using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleStyleNode : SeDefinitionNode, IResourceSerializable
{
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        // starts @ 0x80?

        
    }
}