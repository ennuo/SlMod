namespace SlLib.Archives;

/// <summary>
///     An entry in a pack file from S&SAR.
/// </summary>
public class SsrPackFileEntry
{
    /// <summary>
    ///     The compressed size of this entry's data.
    /// </summary>
    public int CompressedSize;

    /// <summary>
    ///     The hash of this entry's file path.
    /// </summary>
    public int FilenameHash;

    /// <summary>
    ///     Entry flags.
    /// </summary>
    public int Flags;

    /// <summary>
    ///     The offset of this entry's data in the archive.
    /// </summary>
    public int Offset;

    /// <summary>
    ///     The uncompressed size of this entry's data.
    /// </summary>
    public int Size;
}