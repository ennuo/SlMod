using SlLib.Serialization;

namespace SlLib.Resources.Model.Commands;

/// <summary>
///     Represents a command that renders a segment of a model.
/// </summary>
public class RenderSegmentCommand : IRenderCommand
{
    /// <summary>
    ///     Material animation data for this segment.
    /// </summary>
    public byte[] MaterialAnimation = Array.Empty<byte>();

    /// <summary>
    ///     The index of the material to use in rendering.
    /// </summary>
    public short MaterialIndex;

    /// <summary>
    ///     The joint this segment is attached to.
    /// </summary>
    public short PivotJoint = -1;

    /// <summary>
    ///     The index of the segment in the model to render.
    /// </summary>
    public short SegmentIndex;

    /// <summary>
    ///     The offset in the buffer to store animation work status.
    /// </summary>
    public int WorkPass;

    public int Type => 0x01;
    public int Size => 0x14;

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int commandBufferOffset, int offset)
    {
        SegmentIndex = context.ReadInt16(offset + 0x04);
        
        // I don't even think this is actually used, but whatever
        PivotJoint = context.ReadInt16(offset + 0x06);
        
        MaterialIndex = context.ReadInt16(offset + 0x08);
        WorkPass = context.ReadInt32(offset + 0xc);
        
        
        // earlier versions, either 0xb or 0x13 from what we have
        // don't have material animations yet, so make sure to account for that at some point
        // for now it doesn't matter since i dont even serialize them
        
        // i think they use a bone for it? short at 0xa
        
        // if (data.ReadInt32(offset + 0x10) != 0)
        // throw new NotImplementedException("Material animation data is unsupported!");    
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer)
    {
        context.WriteInt16(commandBuffer, SegmentIndex, 4);
        context.WriteInt16(commandBuffer, PivotJoint, 6);
        context.WriteInt16(commandBuffer, MaterialIndex, 8);
        context.WriteInt32(commandBuffer, WorkPass, 12);
    }
}