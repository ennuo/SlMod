namespace SlLib.Resources.Buffer;

/// <summary>
///     Describes a constant buffer.
/// </summary>
public class SlConstantBufferDescChunk
{
    /// <summary>
    ///     Members in the buffer.
    /// </summary>
    public readonly List<SlConstantBufferMember> Members = [];

    /// <summary>
    ///     Name of the constant buffer.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     Size of the buffer.
    /// </summary>
    public int Size;
}