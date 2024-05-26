namespace SlLib.Filesystem;

public sealed class MappedFileSystem : IFileSystem
{
    /// <summary>
    ///     Root folder.
    /// </summary>
    private readonly string _root;

    /// <summary>
    ///     Creates a new mapped filesystem at a specified root.
    /// </summary>
    /// <param name="root">The root folder for this filesystem on disk</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if root directory doesn't exist</exception>
    public MappedFileSystem(string root)
    {
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"{root} doesn't exist!");
        _root = root;
    }

    /// <inheritdoc />
    public bool DoesFileExist(string path)
    {
        return File.Exists(Path.Join(_root, path));
    }

    /// <inheritdoc />
    public byte[] GetFile(string path)
    {
        return File.ReadAllBytes(Path.Join(_root, path));
    }

    /// <inheritdoc />
    public Stream GetFileStream(string path, out int fileSize)
    {
        FileStream stream = File.OpenRead(Path.Join(_root, path));
        fileSize = (int)stream.Length;
        return stream;
    }

    public void Dispose()
    {
    }
}