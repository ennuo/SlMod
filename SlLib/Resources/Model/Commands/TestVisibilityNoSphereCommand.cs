using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that tests the visibility of a segment, without using a cull sphere.
/// </summary>
public class TestVisibilityNoSphereCommand : IRenderCommand
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

    public int Type => 0x0a;
    public int Size => 0x18;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        LocatorIndex = context.ReadInt16(offset + 4);
        VisibilityIndex = context.ReadInt16(offset + 6);
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
        context.WriteInt16(commandBuffer, -1, 8);
        context.WriteInt32(commandBuffer, Flags, 12);
        context.WriteBoolean(commandBuffer, CalculateCullMatrix, 16, true);
        context.WriteInt32(commandBuffer, BranchOffset, 20);
    }
}