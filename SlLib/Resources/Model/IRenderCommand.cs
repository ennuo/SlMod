using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     A command in the render pipeline.
/// </summary>
public interface IRenderCommand
{
    /// <summary>
    ///     The command type enum value.
    /// </summary>
    public int Type { get; }

    /// <summary>
    ///     The size of the command in the buffer.
    /// </summary>
    public int Size { get; }

    /// <summary>
    ///     Loads a render command from the buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="commandBufferOffset">The offset of the start of the command buffer</param>
    /// <param name="offset">The offset into the buffer of the render command</param>
    void Load(ResourceLoadContext context, int commandBufferOffset, int offset);

    /// <summary>
    ///     Saves a render command to a buffer.
    /// </summary>
    /// <param name="context">The current save context</param>
    /// <param name="commandDataBuffer">The command data array buffer</param>
    /// <param name="commandBuffer">This command's buffer data</param>
    /// <param name="extraBuffer">Optional extra buffer</param>
    void Save(ResourceSaveContext context, ISaveBuffer commandDataBuffer, ISaveBuffer commandBuffer,
        ISaveBuffer? extraBuffer);
}