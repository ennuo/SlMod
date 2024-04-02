﻿using SlLib.Extensions;
using SlLib.Utilities;

namespace SlLib.Archives;

/// <summary>
///     Packed data archive used in Sonic & All-Stars Racing Transformed.
/// </summary>
public sealed class SlPackFile : IFileSystem
{
    /// <summary>
    ///     Collection of entries stored in this archive.
    /// </summary>
    private readonly List<SlPackFileTocEntry> _entries = [];

    /// <summary>
    ///     Collection of paths for each data archive needed by this pack file.
    /// </summary>
    private readonly List<FileStream> _handles = [];

    /// <summary>
    ///     Reads a pack file from disk.
    /// </summary>
    /// <param name="path">The path to the pack file (either the TOC or any MXX file)</param>
    /// <exception cref="FileNotFoundException">Thrown if the TOC or MXX files are not found</exception>
    public SlPackFile(string path)
    {
        path = Path.ChangeExtension(path, null);
        string tocFilePath = $"{path}.toc";
        if (!File.Exists(tocFilePath))
            throw new FileNotFoundException($"{tocFilePath} doesn't exist!");
        
        byte[] toc = File.ReadAllBytes(tocFilePath);
        CryptUtil.PackFileUnmunge(toc);

        int numBins = toc.ReadInt32(0x10);
        int tableOffset = toc.ReadInt32(0x14);
        int stringsOffset = toc.ReadInt32(0x18);

        for (int i = 0; i < numBins; ++i)
        {
            string packFilePath = $"{path}.M{i:00}";
            if (!File.Exists(packFilePath))
                throw new FileNotFoundException($"Need data file {packFilePath} doesn't exist!");
            _handles.Add(File.OpenRead(packFilePath));
        }

        int offset = tableOffset;
        while (offset < stringsOffset)
        {
            string name = toc.ReadString(stringsOffset + toc.ReadInt32(offset + 20));
            _entries.Add(new SlPackFileTocEntry
            {
                Hash = toc.ReadInt32(offset + 0),
                IsDirectory = toc.ReadBoolean(offset + 4),
                Parent = toc.ReadInt32(offset + 8),
                Offset = toc.ReadInt32(offset + 12),
                Size = toc.ReadInt32(offset + 16),
                Name = name,
                Path = name
            });

            offset += 24;
        }

        ResolveEntryPaths(_entries[0]);
    }

    /// <inheritdoc />
    public bool DoesFileExist(string path)
    {
        return _entries.Exists(entry => entry.Path == path);
    }

    /// <summary>
    ///     Extracts a file by path.
    /// </summary>
    /// <param name="path">Path of the file to extract</param>
    /// <returns>File data extracted from pack file</returns>
    /// <exception cref="FileNotFoundException">Thrown if data archive needed by this pack is missing</exception>
    public byte[]? GetFile(string path)
    {
        SlPackFileTocEntry? entry = _entries.Find(entry => entry.Path == path);
        if (entry == null || entry.IsDirectory) return null;

        FileStream fs = _handles[entry.Parent];
        fs.Seek(entry.Offset, SeekOrigin.Begin);
        byte[] buffer = new byte[entry.Size];
        fs.ReadExactly(buffer, 0, buffer.Length);

        return buffer;
    }

    public void Dispose()
    {
        foreach (FileStream stream in _handles)
            stream.Dispose();
    }

    /// <summary>
    ///     Recursively caches paths for all entries in the pack.
    /// </summary>
    /// <param name="entry">Entry to recurse</param>
    private void ResolveEntryPaths(SlPackFileTocEntry entry)
    {
        if (!entry.IsDirectory) return;
        bool isRoot = entry.Parent == -1;
        for (int i = entry.Offset; i < entry.Offset + entry.Size; ++i)
        {
            SlPackFileTocEntry child = _entries[i];
            if (!isRoot)
                child.Path = $"{entry.Path}/{child.Path}";
            if (child.IsDirectory)
                ResolveEntryPaths(child);
        }
    }
}