namespace SlLib.SumoTool;

public class SiffChunk
{
    /// <summary>
    ///     Data contained in this chunk.
    /// </summary>
    public ArraySegment<byte> Data;

    /// <summary>
    ///     Pointer relocations for this chunk.
    /// </summary>
    public List<int> Relocations = [];

    /// <summary>
    ///     Type of resource contained in this chunk.
    /// </summary>
    public SiffResourceType Type;
}