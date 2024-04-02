namespace SlLib.Archives;

public interface IFileSystem : IDisposable
{
    /// <summary>
    ///     Checks if a file exists in the filesystem.
    /// </summary>
    /// <param name="path">Path of file</param>
    /// <returns>Whether or not the file exists in the filesystem</returns>
    public bool DoesFileExist(string path);

    /// <summary>
    ///     Extracts file by path.
    /// </summary>
    /// <param name="path">Path of file to extract</param>
    /// <returns>File data extracted from filesystem</returns>
    public byte[]? GetFile(string path);
}