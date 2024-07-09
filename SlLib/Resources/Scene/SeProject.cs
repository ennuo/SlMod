using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public class SeProject : SeDefinitionNode, IResourceSerializable
{
    /// <summary>
    ///     Whether this is the master project of the workspace.
    /// </summary>
    public bool MasterProject;
    
    /// <summary>
    ///     Definition nodes owned by this project.
    /// </summary>
    public readonly List<SeDefinitionNode> Definitions = [];
    
    /// <summary>
    ///     Instance nodes owned by this project.
    /// </summary>
    public readonly List<SeInstanceNode> Instances = [];
    
    /// <summary>
    ///     The flags representing this project's current edit state.
    /// </summary>
    public int EditState;
    
    /// <inheritdoc />
    public override void Load(ResourceLoadContext context)
    {
        context.Position = LoadInternal(context, context.Position);
        
        // These don't get serialized to the actual nodes
        // 0x80 = Files(?) - SeFile RefCountPtrArray (assuming this type size is 16 bytes)
        // 0x90 = Definitions(?) - SeNodeBase RefCountPtrArray (assuming this type size is 16 bytes)
        // 0xa0 = Instances(?) - SeNodeBase RefCountPtrArray (assuming this type size is 16 bytes)
        context.Position += 0x30;
        
        MasterProject = context.ReadBoolean(wide: true);
        EditState = context.ReadInt32();  // ( & 0xc2 | 10)
    }
}