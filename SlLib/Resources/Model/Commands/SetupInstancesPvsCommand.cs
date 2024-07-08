using System.Numerics;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

public class SetupInstancesPvsCommand : IRenderCommand
{
    public int Type => 0x10;

    public int Size => 0x18;

    /// <summary>
    ///     World position matrices for each instance to setup
    /// </summary>
    public List<Matrix4x4> InstanceWorldMatrices = [];
    
    /// <summary>
    ///     Index of the cull sphere to use for visibility testing.
    /// </summary>
    public short CullSphereIndex = -1;

    /// <summary>
    ///     Render mask flags
    /// </summary>
    public int RenderMask;

    /// <summary>
    ///     Branch offset if no instances pass visibility test.
    /// </summary>
    public int BranchOffset;

    /// <summary>
    ///     Pvs data offset(?) No idea, deal with it later
    /// </summary>
    public int PvsData;
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        short numInstances = context.ReadInt16(offset + 4);
        CullSphereIndex = context.ReadInt16(offset + 6);
        RenderMask = context.ReadInt32(offset + 8);
        
        int matrixData = commandBufferOffset + context.ReadInt32(offset + 12);
        InstanceWorldMatrices = context.LoadArray(matrixData, numInstances, context.ReadMatrix);
        
        BranchOffset = context.ReadInt32(offset + 16);
        
        // might also be a data pointer? except using an offset from the matrix data pointer
        PvsData = context.ReadInt32(offset + 20);
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt16(commandBuffer, (short)InstanceWorldMatrices.Count, 4);
        context.WriteInt16(commandBuffer, CullSphereIndex, 6);
        context.WriteInt32(commandBuffer, RenderMask, 8);
        
        ISaveBuffer matrixData = context.Allocate(InstanceWorldMatrices.Count * 64, 0x10);
        context.WriteInt32(commandBuffer, matrixData.Address - commandDataBuffer.Address, 12);
        for (int i = 0; i < InstanceWorldMatrices.Count; ++i)
            context.WriteMatrix(matrixData, InstanceWorldMatrices[i], i * 64);
        
        context.WriteInt32(commandBuffer, BranchOffset, 16);
        context.WriteInt32(commandBuffer, PvsData, 20);
    }
    
    /// <inheritdoc />
    public virtual void Work(SlModel model, SlModelRenderContext context)
    {
        context.RenderContextInstances.Clear();
        if (InstanceWorldMatrices.Count != 0)
        {
            foreach (Matrix4x4 world in InstanceWorldMatrices)
            {
                context.RenderContextInstances.Add(new SlModelInstanceData
                {
                    InstanceBindMatrix = world * context.EntityWorldMatrix,
                    InstanceWorldMatrix = world * context.EntityWorldMatrix
                });
            }

            context.Instances = context.RenderContextInstances;
        }
        else
        {
            context.Instances = context.SceneGraphInstances;
        }
    }
}