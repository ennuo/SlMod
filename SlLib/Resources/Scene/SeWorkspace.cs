using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public class SeWorkspace : SeDefinitionNode, IResourceSerializable
{
    /// <summary>
    ///     Projects owned by this workspace.
    /// </summary>
    public List<SeProject> Projects = [];
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        // + 0x80 = Projects = SeProject RefCountPtrArray (assuming this type size is 16 bytes)
    }
}