using System.Buffers.Binary;
using System.IO.Compression;
using SlLib.Extensions;
using SlLib.Utilities;
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
                Flags = entryTableData.ReadInt32(offset + 16),
                TempHack_EntryFileOffset = offset + 24
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

    public void AddFile(string path, byte[] data)
    {
        if (DoesFileExist(path))
        {
            SetFile(path, data);
            return;
        }
        
        using FileStream fs = File.OpenWrite(_path);
        fs.Seek(0, SeekOrigin.End);
        byte[] pad = new byte[SlUtil.Align(data.Length, 0x800)];
        data.CopyTo(pad, 0);
        fs.Write(pad);
        
        var entry = new SsrPackFileEntry
        {
            FilenameHash = GetFilenameHash(path),
            CompressedSize = data.Length,
            Size = data.Length,
            Flags = 0,
            Offset = (uint)fs.Position,
            TempHack_EntryFileOffset = 24 + (_entries.Count * 20)
        };
        
        _entries.Add(entry);
        
        Span<byte> header = stackalloc byte[20];
        BinaryPrimitives.WriteInt32LittleEndian(header[0..4], entry.FilenameHash);
        BinaryPrimitives.WriteInt32LittleEndian(header[4..8], (int)entry.Offset);
        BinaryPrimitives.WriteInt32LittleEndian(header[8..12], entry.Size);
        BinaryPrimitives.WriteInt32LittleEndian(header[12..16], entry.CompressedSize);
        
        fs.Seek(entry.TempHack_EntryFileOffset, SeekOrigin.Begin);
        fs.Write(header);

        fs.Seek(12, SeekOrigin.Begin);
        Span<byte> span = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(span, _entries.Count);
        fs.Write(span);
    }
    
    public void SetFile(string path, byte[] data)
    {
        SsrPackFileEntry entry = GetFileEntry(path);

        using FileStream fs = File.OpenWrite(_path);
        
        // If the file already in the archive is bigger, we can just directly write over it,
        // instead of appending any data.
        if (entry.CompressedSize >= data.Length)
        {
            byte[] pad = new byte[entry.CompressedSize];
            data.CopyTo(pad, 0);
            
            fs.Seek(entry.Offset, SeekOrigin.Begin);
            fs.Write(pad);
        }
        else
        {
            byte[] pad = new byte[SlUtil.Align(data.Length, 0x800)];
            data.CopyTo(pad, 0);
            
            fs.Seek(0, SeekOrigin.End);
            entry.Offset = (uint)fs.Position;
            fs.Write(pad);
        }
        
        entry.CompressedSize = data.Length;
        entry.Size = data.Length;

        // dumb hack to overwrite table entry, will add something proper at some point.
        fs.Seek(entry.TempHack_EntryFileOffset + 4, SeekOrigin.Begin);
        Span<byte> span = stackalloc byte[4];
        
        BinaryPrimitives.WriteUInt32LittleEndian(span, entry.Offset);
        fs.Write(span);
        
        BinaryPrimitives.WriteInt32LittleEndian(span, entry.Size);
        fs.Write(span);
        
        BinaryPrimitives.WriteInt32LittleEndian(span, entry.CompressedSize);
        fs.Write(span);
    }

    public void Rebuild()
    {
        int size = SlUtil.Align(24 + (_entries.Count * 20), 0x800);
        foreach (SsrPackFileEntry entry in _entries)
        {
            entry.Offset = (uint)size;
            size = SlUtil.Align(size + entry.CompressedSize, 0x800);
        }
        
        
        
        // this is a bad idea as files get bigger, rework this at some point
        
        
        int offset = 0;
        
        
        
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