using System.Buffers.Binary;
using System.IO.Compression;
using SlLib.Extensions;

namespace SlLib.Archives;

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
    ///     File read handle for this archive.
    /// </summary>
    private readonly FileStream _handle;

    /// <summary>
    ///     Constructs a pack file from a path on disk.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if the file is not found</exception>
    /// <param name="path">Path to pack file</param>
    public SsrPackFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{path} does not exist!");

        _handle = File.OpenRead(path);
        Span<byte> header = stackalloc byte[24];
        _handle.ReadExactly(header);

        int numEntries = BinaryPrimitives.ReadInt32LittleEndian(header[12..16]);

        byte[] entryTableData = new byte[numEntries * 20];
        _handle.ReadExactly(entryTableData);

        for (int i = 0; i < numEntries; ++i)
        {
            int offset = i * 20;
            _entries.Add(new SsrPackFileEntry
            {
                FilenameHash = entryTableData.ReadInt32(offset),
                Offset = entryTableData.ReadInt32(offset + 4),
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

    /// <inheritdoc />
    public byte[]? GetFile(string path)
    {
        int hash = GetFilenameHash(path);
        SsrPackFileEntry? entry = _entries.Find(entry => entry.FilenameHash == hash);
        if (entry == null) return null;

        _handle.Seek(entry.Offset, SeekOrigin.Begin);
        byte[] buffer = new byte[entry.Size];

        if (entry.CompressedSize != entry.Size)
        {
            using var decompressor = new DeflateStream(_handle, CompressionMode.Decompress);
            decompressor.ReadExactly(buffer, 0, buffer.Length);
        }
        else
        {
            _handle.ReadExactly(buffer, 0, buffer.Length);
        }

        return buffer;
    }

    public void Dispose()
    {
        _handle.Dispose();
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