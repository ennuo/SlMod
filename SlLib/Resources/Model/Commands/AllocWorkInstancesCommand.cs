using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that calculates the bind matrices for a skin.
/// </summary>
public class AllocWorkInstancesCommand : IRenderCommand
{
    /// <summary>
    ///     Number of instances to allocate.
    /// </summary>
    public int NumInstances;
    
    public int Type => 0x07;
    public int Size => 0x08;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        NumInstances = context.ReadInt32(offset + 4);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt32(commandBuffer, NumInstances, 4);
    }
}