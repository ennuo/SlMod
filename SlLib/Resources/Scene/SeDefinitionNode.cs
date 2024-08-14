using SlLib.Resources.Database;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeDefinitionNode : SeGraphNode
{
    /// <summary>
    ///     The instances of this definition.
    /// </summary>
    public readonly List<SeInstanceNode> Instances = [];
    
    /// <summary>
    ///     Creates instances of this node's hierarchy parented to a given node.
    /// </summary>
    /// <param name="parent">Node to parent to</param>
    public SeInstanceNode Instance(SeGraphNode parent)
    {
        SeInstanceNode instance = this switch
        {
            SeDefinitionEntityNode => SeInstanceNode.CreateObject<SeInstanceEntityNode>(this),
            SeDefinitionAnimatorNode => SeInstanceNode.CreateObject<SeInstanceAnimatorNode>(this),
            SeDefinitionAnimationStreamNode => SeInstanceNode.CreateObject<SeInstanceAnimationStreamNode>(this),
            SeDefinitionCollisionNode => SeInstanceNode.CreateObject<SeInstanceCollisionNode>(this),
            _ => throw new ArgumentException("invalid!")
        };

        if (instance is SeInstanceTransformNode transformInstance &&
            this is SeDefinitionTransformNode transformDefinition)
        {
            transformInstance.Translation = transformDefinition.Translation;
            transformInstance.Rotation = transformDefinition.Rotation;
            transformInstance.Scale = transformDefinition.Scale;
            transformInstance.InheritTransforms = transformDefinition.InheritTransforms;
            transformInstance.TransformFlags = transformDefinition.TransformFlags;
        }
        
        instance.Parent = parent;
        
        SeGraphNode? child = FirstChild;
        while (child != null)
        {
            if (child is SeDefinitionNode definition)
                definition.Instance(instance);
            child = child.NextSibling;
        }
        
        instance.RecomputePositions();
        return instance;
    }
    
    public static T CreateObject<T>() where T : SeDefinitionNode, new()
    {
        T node = new();
        node.SetNameWithTimestamp(typeof(T).Name);
        return node;
    }
    
    /// <summary>
    ///     Loads this definition node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to laod from</param>
    /// <returns>The offset of the next class base</returns>
    protected override int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        // instance count @ 0x68

        return offset + 0x18;
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);

        // Should just always be 1?
        context.WriteInt32(buffer, 1, 0x70); 
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x80;
}