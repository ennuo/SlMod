namespace SlLib.Resources.Model;

public class SlVertexAttribute
{
    /// <summary>
    ///     The number of elements in the stream.
    /// </summary>
    public int Count;

    /// <summary>
    ///     The usage index.
    /// </summary>
    public int Index;

    /// <summary>
    ///     The byte offset of the attribute.
    /// </summary>
    public int Offset;

    /// <summary>
    ///     The size of this attribute in the stream.
    /// </summary>
    public int Size;

    /// <summary>
    ///     The index of the stream.
    /// </summary>
    public int Stream;

    /// <summary>
    ///     The type of element in the stream.
    /// </summary>
    public SlVertexElementType Type = SlVertexElementType.Float;

    /// <summary>
    ///     The usage of this attribute.
    /// </summary>
    public int Usage = SlVertexUsage.Position;
}