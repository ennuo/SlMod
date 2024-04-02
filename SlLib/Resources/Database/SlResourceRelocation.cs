namespace SlLib.Resources.Database;

public class SlResourceRelocation(int offset, int value)
{
    /// <summary>
    ///     The offset of the pointer in the buffer relative to the data start.
    /// </summary>
    public int Offset = offset;

    /// <summary>
    ///     The packed relocation value.
    ///     <remarks>
    ///         Contains resource type, as well as relocation type.
    ///     </remarks>
    /// </summary>
    public int Value = value;

    /// <summary>
    ///     The type of relocation to be performed.
    /// </summary>
    public int RelocationType => Value & 3;

    /// <summary>
    ///     The type of resource if this is a resource relocation.
    /// </summary>
    public int ResourceType => Value >>> 4;

    /// <summary>
    ///     Whether or not this pointer relocates into the GPU data buffer.
    /// </summary>
    public bool IsGpuPointer => RelocationType == SlRelocationType.Pointer && Value >>> 4 != 0;

    /// <summary>
    ///     Whether or not this pointer references another resource.
    /// </summary>
    public bool IsResourcePointer =>
        RelocationType is SlRelocationType.Resource or SlRelocationType.ResourcePlusDataPointer;
}