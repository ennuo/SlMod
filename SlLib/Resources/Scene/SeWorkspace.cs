using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public class SeWorkspace : SeDefinitionNode
{
    /// <summary>
    ///     Projects owned by this workspace.
    /// </summary>
    public List<SeProject> Projects = [];
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        
        // + 0x80 = Projects = SeProject RefCountPtrArray (assuming this type size is 16 bytes)
    }
}