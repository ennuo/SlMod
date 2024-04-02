namespace SlLib.Archives;

/// <summary>
///     An entry in a pack file.
/// </summary>
public class SlPackFileTocEntry
{
    /// <summary>
    ///     The hash of this entry's name.
    /// </summary>
    public int Hash;

    /// <summary>
    ///     Whether or not this entry is a directory node.
    /// </summary>
    public bool IsDirectory;

    /// <summary>
    ///     The name of this entry.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    ///     The offset of this entry's data.
    ///     For files, this refers to the offset in the pack file.
    ///     For directories, this refers to the index of first child.
    /// </summary>
    public int Offset;

    /// <summary>
    ///     The parent of this entry.
    ///     For files, this refers to the pack file index.
    ///     For directories, this refers to its parent node.
    /// </summary>
    public int Parent = -1;

    /// <summary>
    ///     The cached path of this entry.
    /// </summary>
    public string Path = string.Empty;

    /// <summary>
    ///     The size of this entry's data.
    ///     For files, this refers to the size of the data in the pack file.
    ///     For directories, this refers to the number of child entries.
    /// </summary>
    public int Size;
}