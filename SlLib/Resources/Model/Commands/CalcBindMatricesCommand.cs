using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that calculates the bind matrices for a skin.
/// </summary>
public class CalcBindMatricesCommand : IRenderCommand
{
    /// <summary>
    ///     Inverse bind pose matrices for each joint used by this skin.
    /// </summary>
    public List<Matrix4x4> InvBindMatrices = [];

    /// <summary>
    ///     Indices of nodes in the skeleton that are used by this skin.
    /// </summary>
    public List<int> Joints = [];

    /// <summary>
    ///     The number of bones in the skin.
    /// </summary>
    public int NumBones;

    /// <summary>
    ///     The offset in the work buffer to store bind matrix status.
    /// </summary>
    public int WorkPass;

    /// <summary>
    ///     The offset in the work buffer to store the resulting bind matrices.
    /// </summary>
    public int WorkResult;

    public int Type => 0x0b;
    public int Size => 0x18;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        NumBones = context.ReadInt32(offset + 4);
        WorkPass = context.ReadInt32(offset + 8);
        WorkResult = context.ReadInt32(offset + 12);

        int jointDataOffset = commandBufferOffset + context.ReadInt32(offset + 16);
        int bindDataOffset = commandBufferOffset + context.ReadInt32(offset + 20);

        for (int i = 0; i < NumBones; ++i)
        {
            InvBindMatrices.Add(context.ReadMatrix(bindDataOffset + i * 64));
            Joints.Add(context.ReadInt32(jointDataOffset + i * 4));
        }
    }
}