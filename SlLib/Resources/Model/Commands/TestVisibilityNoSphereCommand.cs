using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that tests the visibility of a segment, without using a cull sphere.
/// </summary>
public class TestVisibilityNoSphereCommand : IRenderCommand
{
    public int Type => 0x0a;
    public int Size => 0x18;
    
    /// <summary>
    ///     Offset in command buffer to seek to if visibility test fails.
    /// </summary>
    public int BranchOffset;

    /// <summary>
    ///     Whether or not to calculate the cull matrix in this command.
    /// </summary>
    public bool CalculateCullMatrix;

    /// <summary>
    ///     Render flags
    /// </summary>
    public int Flags = 0x11;

    /// <summary>
    ///     Index of the node in skeleton to use for visibility testing.
    /// </summary>
    public short LocatorIndex = -1;

    /// <summary>
    ///     Index of the visibility attribute in the skeleton.
    /// </summary>
    public short VisibilityIndex = -1;
    
    /// <summary>
    ///     LOD visibility to test.
    /// </summary>
    public short LodIndex;
    
    /// <summary>
    ///     LOD group this command is part of.
    /// </summary>
    public short LodGroup = -1;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        LocatorIndex = context.ReadInt16(offset + 4); // used to attach joint to locator if visible and not -1?
        VisibilityIndex = context.ReadInt16(offset + 6);
        LodGroup = context.ReadInt16(offset + 8);
        LodIndex = context.ReadInt16(offset + 10);
        Flags = context.ReadInt32(offset + 12);
        CalculateCullMatrix = context.ReadBoolean(offset + 16, true);
        BranchOffset = context.ReadInt32(offset + 20);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt16(commandBuffer, LocatorIndex, 4);
        context.WriteInt16(commandBuffer, VisibilityIndex, 6);
        context.WriteInt16(commandBuffer, LodGroup, 8);
        context.WriteInt16(commandBuffer, LodIndex, 10);
        context.WriteInt32(commandBuffer, Flags, 12);
        context.WriteBoolean(commandBuffer, CalculateCullMatrix, 16, true);
        context.WriteInt32(commandBuffer, BranchOffset, 20);
    }
    
    /// <inheritdoc />
    public virtual void Work(SlModel model, SlModelRenderContext context)
    {
        // for now we're not handling LODs at all
        if (LodGroup != -1)
        {
            context.NextSegmentIsVisible = false;
            return;
        }
        
        SlSkeleton? skeleton = model.Resource.Skeleton;
        
        
        foreach (SlModelInstanceData instance in context.Instances)
        {
            if (LocatorIndex == -1)
                instance.WorldMatrix = instance.InstanceWorldMatrix;
            else
                instance.WorldMatrix = skeleton!.Joints[LocatorIndex].BindPose * instance.InstanceBindMatrix;
        }
    }
}