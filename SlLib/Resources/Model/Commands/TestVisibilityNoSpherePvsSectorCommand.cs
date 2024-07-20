using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that tests the visibility of a segment by PVS set data.
/// </summary>
public class TestVisibilityNoSpherePvsSectorCommand : IBranchRenderCommand
{
    /// <summary>
    ///     Offset in command buffer to seek to if visibility test fails.
    /// </summary>
    public int BranchOffset { get; set; }

    /// <summary>
    ///     Whether to calculate the cull matrix in this command.
    /// </summary>
    public bool CalculateCullMatrix = true;
    
    /// <summary>
    ///     Render flags
    /// </summary>
    public int Flags = 0x12;

    /// <summary>
    ///     Index of the node in skeleton to use for visibility testing.
    /// </summary>
    public short LocatorIndex = -1;

    /// <summary>
    ///     Index of the PVS sector to test
    /// </summary>
    public int Sector;

    public int Type => 0xf;
    public int Size => 0x18;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        LocatorIndex = context.ReadInt16(offset + 4);
        CalculateCullMatrix = context.ReadInt16(offset + 6) != 0;
        Flags = context.ReadInt32(offset + 8);
        BranchOffset = context.ReadInt32(offset + 12);
        Sector = context.ReadInt32(offset + 20);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt16(commandBuffer, LocatorIndex, 4);
        context.WriteInt16(commandBuffer, (short)(CalculateCullMatrix ? 1 : 0), 6);
        context.WriteInt32(commandBuffer, Flags, 8);
        context.WriteInt32(commandBuffer, BranchOffset, 12);
        context.WriteInt32(commandBuffer, Sector, 20);
    }
    
    /// <inheritdoc />
    public virtual void Work(SlModel model, SlModelRenderContext context)
    {
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