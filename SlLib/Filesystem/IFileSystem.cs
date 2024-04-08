namespace SlLib.Filesystem;

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
    public byte[] GetFile(string path);

    /// <summary>
    ///     Gets a stream handle for a file by path.
    /// </summary>
    /// <param name="path">Path of file to extract</param>
    /// <param name="fileSize">Output parameter, the size of the file.</param>
    /// <returns>File stream</returns>
    public Stream GetFileStream(string path, out int fileSize);
}