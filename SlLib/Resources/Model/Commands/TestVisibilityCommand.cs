using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that tests the visibility of a segment.
/// </summary>
public class TestVisibilityCommand : IRenderCommand
{
    /// <summary>
    ///     Offset in command buffer to seek to if visibility test fails.
    /// </summary>
    public int BranchOffset;

    /// <summary>
    ///     Whether or not to calculate the cull matrix in this command.
    /// </summary>
    public bool CalculateCullMatrix = true;

    /// <summary>
    ///     Index of the cull sphere to use for visibility testing.
    /// </summary>
    public short CullSphereIndex = -1;

    /// <summary>
    ///     LOD group this visibility command is part of.
    /// </summary>
    public short LodGroup = -1;

    /// <summary>
    ///     LOD index this visibility command tests.
    /// </summary>
    public short LodIndex;

    /// <summary>
    ///     Render flags
    /// </summary>
    public int Flags = 0x12;

    /// <summary>
    ///     Index of the node in skeleton to use for visibility testing.
    /// </summary>
    public short LocatorIndex = -1;

    /// <summary>
    ///     Index of the visibility attribute in the skeleton.
    /// </summary>
    public int VisibilityIndex = -1;

    public int Type => 0x0;
    public int Size => 0x1c;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        CullSphereIndex = context.ReadInt16(offset + 4);
        LocatorIndex = context.ReadInt16(offset + 6);
        VisibilityIndex = context.ReadInt32(offset + 8);
        LodGroup = context.ReadInt16(offset + 12);
        LodIndex = context.ReadInt16(offset + 14);
        Flags = context.ReadInt32(offset + 16);
        // earlier versions seem to not serialize this calculate cull matrix boolean?
        CalculateCullMatrix = context.ReadBoolean(offset + 20, true);
        BranchOffset = context.ReadInt32(offset + 24);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt16(commandBuffer, CullSphereIndex, 4);
        context.WriteInt16(commandBuffer, LocatorIndex, 6);
        context.WriteInt32(commandBuffer, VisibilityIndex, 8);
        context.WriteInt16(commandBuffer, LodGroup, 12);
        context.WriteInt16(commandBuffer, LodIndex, 14);
        context.WriteInt32(commandBuffer, Flags, 16);
        context.WriteBoolean(commandBuffer, CalculateCullMatrix, 20, true);
        context.WriteInt32(commandBuffer, BranchOffset, 24);
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