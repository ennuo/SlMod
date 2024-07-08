using SlLib.Serialization;

namespace SlLib.Resources.Scene.Definitions;

public class SeDefinitionParticleSystemNode : SeDefinitionTransformNode, IResourceSerializable
{
    public int SystemFlags;
    public float MaxClipSize = 1.0f;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        SystemFlags = context.ReadInt32();
        context.Position += 8;
        MaxClipSize = context.ReadFloat();
    }
}