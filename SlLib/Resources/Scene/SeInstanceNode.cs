using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Scene;

public abstract class SeInstanceNode : SeGraphNode
{
    public SeDefinitionNode? Definition { get; set; }
    public float LocalTimeScale;
    public float LocalTime;
    
    // LOCAL, GLOBAL, PARENT, PARENT_PLUS_LOCAL, GLOBAL_PLUS_LOCAL, GLOBAL_PLUS_PARENT
    public int TimeFrame;
    
    ~SeInstanceNode()
    {
        Definition?.Instances.Remove(this);
    }
    
    public static T CreateObject<T>() where T : SeInstanceNode, new()
    {
        T node = new();
        node.SetNameWithTimestamp(typeof(T).Name);
        return node;
    }
    
    /// <summary>
    ///     Loads this instance node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to load from</param>
    /// <returns>The offset of the next class base</returns>
    protected override int LoadInternal(ResourceLoadContext context, int offset)
    {
        offset = base.LoadInternal(context, offset);
        
        int address = context.ReadInt32(offset);
        if (address != 0)
            Definition = (SeDefinitionNode?) context.LoadNode(context.ReadInt32(address));
        Definition?.Instances.Add(this);
        
        LocalTimeScale = context.ReadFloat(0x70);
        LocalTime = context.ReadFloat(0x74);
        TimeFrame = context.ReadInt32(0x78);
        
        
        return offset + 0x18;
    }
    
    /// <inheritdoc />
    public override void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        base.Save(context, buffer);
        
        context.WriteNodePointer(buffer, Definition, 0x68);
        context.WriteFloat(buffer, LocalTimeScale, 0x70);
        context.WriteFloat(buffer, LocalTime, 0x74);
        context.WriteInt32(buffer, TimeFrame, 0x78); // sgFlags
    }
    
    /// <inheritdoc />
    public override int GetSizeForSerialization(SlPlatform platform, int version) => 0x80;
}