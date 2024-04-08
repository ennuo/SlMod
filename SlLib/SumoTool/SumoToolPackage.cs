using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.Serialization;
using SlLib.Utilities;

namespace SlLib.SumoTool;

public class SumoToolPackage
{
    private readonly byte[]?[] _files = new byte[SumoToolPackageFile.Count][];

    /// <summary>
    /// Sets locale data in sumo tool package.
    /// </summary>
    /// <param name="dat">Dat chunk file</param>
    /// <param name="rel">Rel chunk file</param>
    /// <param name="gpu">Gpu chunk file</param>
    public void SetLocaleChunks(byte[] dat, byte[] rel, byte[]? gpu)
    {
        _files[SumoToolPackageFile.Lang] = dat;
        _files[SumoToolPackageFile.LangRel] = rel;
        _files[SumoToolPackageFile.LangGpu] = gpu;
    }
    
    /// <summary>
    /// Sets common data in sumo tool package.
    /// </summary>
    /// <param name="dat">Dat chunk file</param>
    /// <param name="rel">Rel chunk file</param>
    /// <param name="gpu">Gpu chunk file</param>
    public void SetCommonChunks(byte[] dat, byte[] rel, byte[]? gpu)
    {
        _files[SumoToolPackageFile.Dat] = dat;
        _files[SumoToolPackageFile.Rel] = rel;
        _files[SumoToolPackageFile.Gpu] = gpu;
    }
    
    /// <summary>
    ///     Sets the locale siff data in this package.
    /// </summary>
    /// <param name="siff">Locale siff</param>
    public void SetLocaleData(SiffFile siff)
    {
        siff.BuildForSumoTool(out _files[SumoToolPackageFile.Lang], out _files[SumoToolPackageFile.LangRel],
            out _files[SumoToolPackageFile.LangGpu]);
    }

    /// <summary>
    ///     Sets the common siff data in this package.
    /// </summary>
    /// <param name="siff">Common siff</param>
    public void SetCommonData(SiffFile siff)
    {
        siff.BuildForSumoTool(out _files[SumoToolPackageFile.Dat], out _files[SumoToolPackageFile.Rel],
            out _files[SumoToolPackageFile.Gpu]);
    }

    /// <summary>
    ///     Checks whether or not this package contains locale data.
    /// </summary>
    /// <returns>True if the package contains locale data</returns>
    public bool HasLocaleData()
    {
        byte[]? file = _files[SumoToolPackageFile.Lang];
        return file != null && file.Length != 0;
    }

    /// <summary>
    ///     Checks whether or not this package contains common data.
    /// </summary>
    /// <returns>True if the package contains common data</returns>
    public bool HasCommonData()
    {
        byte[]? file = _files[SumoToolPackageFile.Dat];
        return file != null && file.Length != 0;
    }

    /// <summary>
    ///     Loads a Siff file instance from the locale data.
    /// </summary>
    /// <returns>Siff file</returns>
    public SiffFile GetLocaleSiff()
    {
        byte[]? langDat = _files[SumoToolPackageFile.Lang];
        byte[]? langRel = _files[SumoToolPackageFile.LangRel];

        // These should never be null if we're loading a locale siff.
        ArgumentNullException.ThrowIfNull(langDat);
        ArgumentNullException.ThrowIfNull(langRel);

        // GPU files don't need to exist and generally don't exist for locale siff data.
        byte[]? langGpu = _files[SumoToolPackageFile.LangGpu];

        return SiffFile.Load(langDat, langRel, langGpu);
    }

    /// <summary>
    ///     Loads a Siff file instance from the common data.
    /// </summary>
    /// <returns>Siff file</returns>
    public SiffFile GetCommonSiff()
    {
        byte[]? dat = _files[SumoToolPackageFile.Dat];
        byte[]? rel = _files[SumoToolPackageFile.Rel];

        // Should never be null
        ArgumentNullException.ThrowIfNull(dat);
        ArgumentNullException.ThrowIfNull(rel);

        // Not sure if this is required for common data? Common data doesn't
        // generally seem to be used, suppose it doesn't matter too much.
        byte[]? gpu = _files[SumoToolPackageFile.Gpu];

        return SiffFile.Load(dat, rel, gpu);
    }

    /// <summary>
    ///     Loads a compressed sumo tool package from a buffer.
    /// </summary>
    /// <param name="data">Sumo tool package buffer</param>
    /// <returns>Parsed sumo tool package</returns>
    public static SumoToolPackage Load(byte[] data)
    {
        using var stream = new MemoryStream(data);
        return Load(stream);
    }

    /// <summary>
    ///     Loads a compressed sumo tool package from a file.
    /// </summary>
    /// <param name="path">Path to sumo tool package</param>
    /// <returns>Parsed sumo tool package</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file is not found</exception>
    public static SumoToolPackage Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"No sumo tool package file at {path}!");

        using FileStream stream = File.OpenRead(path);
        return Load(stream);
    }

    /// <summary>
    ///     Loads a compressed sumo tool package from a stream.
    /// </summary>
    /// <param name="stream">Sumo tool package data stream</param>
    /// <returns>Parsed sumo tool package</returns>
    /// <exception cref="SerializationException">Thrown if there's not enough data in the header</exception>
    public static SumoToolPackage Load(Stream stream)
    {
        long start = stream.Position;

        // Sumo tool packages are just a collection of 6 entries,
        // each entry containing a offset, compressed size, and real size.
        const int headerDataSize = SumoToolPackageFile.Count * 0xc;
        if (stream.Length < headerDataSize)
            throw new SerializationException("Not enough data in compressed sumo tool package!");

        Span<byte> header = stackalloc byte[headerDataSize];
        stream.ReadExactly(header);

        SumoToolPackage package = new();
        for (int i = 0; i < SumoToolPackageFile.Count; ++i)
        {
            int offset = i * 0xc;
            int address = BinaryPrimitives.ReadInt32LittleEndian(header[offset..(offset + 4)]);
            int dataSize = BinaryPrimitives.ReadInt32LittleEndian(header[(offset + 4)..(offset + 8)]);
            int compressedSize = BinaryPrimitives.ReadInt32LittleEndian(header[(offset + 8)..(offset + 12)]);

            if (dataSize == 0) continue;

            stream.Position = start + address;
            byte[] file = new byte[dataSize];
            if (dataSize != compressedSize)
            {
                using var decompressor = new ZLibStream(stream, CompressionMode.Decompress, true);
                decompressor.ReadExactly(file);
            }
            else
            {
                stream.ReadExactly(file);
            }

            package._files[i] = file;
        }

        return package;
    }

    /// <summary>
    ///     Saves this sumo tool package to a file.
    /// </summary>
    /// <param name="path">Path of file to save</param>
    /// <param name="compress">Whether or not to compress files in the package</param>
    public void Save(string path, bool compress = true)
    {
        using FileStream stream = File.Create(path);
        Save(stream, compress);
    }

    /// <summary>
    ///     Saves this sumo tool package to a byte array.
    /// </summary>
    /// <param name="compress">Whether or not to compress files in the package</param>
    /// <returns></returns>
    public byte[] Save(bool compress = true)
    {
        using var stream = new MemoryStream();
        Save(stream, compress);
        return stream.ToArray();
    }

    /// <summary>
    ///     Saves this sumo tool package to a stream.
    /// </summary>
    /// <param name="stream">Stream to save package to</param>
    /// <param name="compress">Whether or not to compress files in the package</param>
    public void Save(Stream stream, bool compress = true)
    {
        const int headerSize = SumoToolPackageFile.Count * 0xc;

        using var writer = new BinaryWriter(stream);
        long nextFileOffset = headerSize;
        for (int i = 0; i < SumoToolPackageFile.Count; ++i)
        {
            long fileDataOffset = nextFileOffset;
            int fileDataSize = 0;
            int fileDataCompressedSize = 0;

            byte[]? file = _files[i];
            if (file != null && file.Length != 0)
            {
                stream.Position = fileDataOffset;

                fileDataSize = file.Length;

                if (compress)
                {
                    using var compressor = new ZLibStream(stream, CompressionLevel.Optimal, true);
                    compressor.Write(file);
                }
                else
                {
                    stream.Write(file);
                }

                fileDataCompressedSize = (int)(stream.Position - fileDataOffset);
                nextFileOffset = SlUtil.Align(stream.Position, 0x40);
            }

            // Seek back to the position of this file in the header to write the entry data
            stream.Position = i * 0xc;

            writer.Write((int)fileDataOffset);
            writer.Write(fileDataSize);
            writer.Write(fileDataCompressedSize);
        }

        // Just to make sure the padding at the end of the file is included.
        stream.SetLength(nextFileOffset);
    }
}