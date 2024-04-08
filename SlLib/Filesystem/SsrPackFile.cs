using System.Buffers.Binary;
using System.IO.Compression;
using SlLib.Extensions;
using FileStream = System.IO.FileStream;

namespace SlLib.Filesystem;

/// <summary>
///     Packed data archive used in Sonic & Sega All-Stars Racing.
/// </summary>
public sealed class SsrPackFile : IFileSystem
{
    /// <summary>
    ///     Collection of entries stored in this archive.
    /// </summary>
    private readonly List<SsrPackFileEntry> _entries = [];

    /// <summary>
    ///     Path to archive.
    /// </summary>
    private readonly string _path;

    /// <summary>
    ///     Constructs a pack file from a path on disk.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if the file is not found</exception>
    /// <param name="path">Path to pack file</param>
    public SsrPackFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{path} does not exist!");

        _path = path;
        using FileStream fs = File.OpenRead(_path);

        Span<byte> header = stackalloc byte[24];
        fs.ReadExactly(header);

        int numEntries = BinaryPrimitives.ReadInt32LittleEndian(header[12..16]);

        byte[] entryTableData = new byte[numEntries * 20];
        fs.ReadExactly(entryTableData);

        for (int i = 0; i < numEntries; ++i)
        {
            int offset = i * 20;
            _entries.Add(new SsrPackFileEntry
            {
                FilenameHash = entryTableData.ReadInt32(offset),
                Offset = (uint)entryTableData.ReadInt32(offset + 4),
                Size = entryTableData.ReadInt32(offset + 8),
                CompressedSize = entryTableData.ReadInt32(offset + 12),
                Flags = entryTableData.ReadInt32(offset + 16)
            });
        }
    }

    /// <inheritdoc />
    public bool DoesFileExist(string path)
    {
        int hash = GetFilenameHash(path);
        return _entries.Exists(entry => entry.FilenameHash == hash);
    }

    /// <summary>
    ///     Gets a file entry in the pack file by path.
    /// </summary>
    /// <param name="path">Path of entry to find</param>
    /// <returns>File entry</returns>
    private SsrPackFileEntry GetFileEntry(string path)
    {
        int hash = GetFilenameHash(path);
        SsrPackFileEntry? entry = _entries.Find(entry => entry.FilenameHash == hash);
        ArgumentNullException.ThrowIfNull(entry);
        return entry;
    }

    /// <inheritdoc />
    public byte[] GetFile(string path)
    {
        SsrPackFileEntry entry = GetFileEntry(path);
        using FileStream fs = File.OpenRead(_path);
        fs.Seek(entry.Offset, SeekOrigin.Begin);

        byte[] buffer = new byte[entry.Size];
        if (entry.CompressedSize != entry.Size)
        {
            using var decompressor = new ZLibStream(fs, CompressionMode.Decompress, true);
            decompressor.ReadExactly(buffer);
        }
        else
        {
            fs.ReadExactly(buffer);
        }

        return buffer;
    }

    /// <inheritdoc />
    public Stream GetFileStream(string path, out int fileSize)
    {
        SsrPackFileEntry entry = GetFileEntry(path);
        fileSize = entry.Size;

        FileStream fs = File.OpenRead(_path);
        fs.Seek(entry.Offset, SeekOrigin.Begin);

        // If the data is compressed, decompress into a memory stream
        if (entry.CompressedSize != entry.Size)
        {
            var stream = new MemoryStream(entry.Size);
            using var decompressor = new ZLibStream(fs, CompressionMode.Decompress, false);
            decompressor.CopyTo(stream);
            return stream;
        }

        return fs;
    }

    public void Dispose()
    {
    }

    /// <summary>
    ///     Gets the filename hash for a path.
    /// </summary>
    /// <param name="path">Path to hash</param>
    /// <returns>Hash of filename path</returns>
    private static int GetFilenameHash(string path)
    {
        path = $".\\{path.Replace("/", "\\").ToUpper()}";
        uint hash = 0;
        for (int i = path.Length - 1; i >= 0; --i)
            hash = hash * 0x83 + path[i];
        return (int)hash;
    }
}