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
    public List<short> Joints = [];

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
        
        // old versions have jointData at 8
        // inv data at 12
        // work data at 16 (global work buffer didnt exist yet)

        int jointDataOffset, bindDataOffset;
        if (context.Version > 0x1b)
        {
            NumBones = context.ReadInt32(offset + 4);
            
            WorkPass = context.ReadInt32(offset + 8);
            WorkResult = context.ReadInt32(offset + 12);
            
            jointDataOffset = commandBufferOffset + context.ReadInt32(offset + 16);
            bindDataOffset = commandBufferOffset + context.ReadInt32(offset + 20);
        }
        else
        {
            NumBones = context.ReadInt16(offset + 4);
            
            jointDataOffset = commandBufferOffset + context.ReadInt32(offset + 8);
            bindDataOffset = commandBufferOffset + context.ReadInt32(offset + 12);
        }
        
        for (int i = 0; i < NumBones; ++i)
        {
            InvBindMatrices.Add(context.ReadMatrix(bindDataOffset + i * 64));
            Joints.Add(context.ReadInt16(jointDataOffset + i * 2));
        }
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt32(commandBuffer, NumBones, 4);
        context.WriteInt32(commandBuffer, WorkPass, 8);
        context.WriteInt32(commandBuffer, WorkResult, 12);

        ISaveBuffer bindData = context.Allocate(NumBones * 64, 0x10);
        ISaveBuffer jointData = context.Allocate(NumBones * 2, 0x10);

        context.WriteInt32(commandBuffer, jointData.Address - commandDataBuffer.Address, 16);
        context.WriteInt32(commandBuffer, bindData.Address - commandDataBuffer.Address, 20);

        for (int i = 0; i < NumBones; ++i)
        {
            context.WriteInt16(jointData, Joints[i], i * 2);
            context.WriteMatrix(bindData, InvBindMatrices[i], i * 64);
        }
    }
}