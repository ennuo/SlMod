namespace SlLib.Resources.Buffer;

/// <summary>
///     A member descriptor for a constant buffer.
/// </summary>
public class SlConstantBufferMember
{
    /// <summary>
    ///     The byte stride for each element in the array.
    /// </summary>
    public int ArrayDataStride;

    /// <summary>
    ///     The element stride for each element in the array.
    /// </summary>
    public int ArrayElementStride;

    /// <summary>
    ///     Number of columns, if this is a matrix.
    /// </summary>
    public int Columns = 1;

    /// <summary>
    ///     The number of vector components used by this member.
    ///     <remarks>
    ///         Basically is this a float1, float2, float3, or float4.
    ///     </remarks>
    /// </summary>
    public int Components = 1;

    /// <summary>
    ///     The dimensions of this array
    ///     <remarks>
    ///         In general, 0 = Scalar, 1 = Vector, 2 = Matrix
    ///     </remarks>
    /// </summary>
    public int Dimensions;

    /// <summary>
    ///     The number of vector components in this member.
    ///     <remarks>
    ///         Should generally always be 4 if using vectors.
    ///     </remarks>
    /// </summary>
    public int MaxComponents = 4;

    /// <summary>
    ///     The name of this member.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The offset of this member.
    /// </summary>
    public int Offset;

    /// <summary>
    ///     Number of rows, if this is a matrix.
    /// </summary>
    public int Rows = 1;

    /// <summary>
    ///     The size of the member.
    /// </summary>
    public int Size = 16;
}