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
    public int CullSphereIndex = -1;

    /// <summary>
    ///     Render flags
    /// </summary>
    public int Flags = 0x12;

    /// <summary>
    ///     Index of the node in skeleton to use for visibility testing.
    /// </summary>
    public int LocatorIndex = -1;

    /// <summary>
    ///     Index of the visibility attribute in the skeleton.
    /// </summary>
    public int VisibilityIndex = -1;

    public int Type => 0x0d;
    public int Size => 0x1c;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        CullSphereIndex = context.ReadInt16(offset + 4);
        LocatorIndex = context.ReadInt16(offset + 6);
        VisibilityIndex = context.ReadInt32(offset + 8);
        Flags = context.ReadInt32(offset + 16);
        CalculateCullMatrix = context.ReadBoolean(offset + 20);
        BranchOffset = context.ReadInt32(offset + 24);
    }
}