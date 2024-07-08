using System.Buffers.Binary;
using SlLib.Serialization.Streams;

namespace SlLib.Resources.Database;

/// <summary>
///     Represents information about a platform's serialization.
/// </summary>
/// <param name="extension">The extension used for this platform's scene files</param>
/// <param name="isBigEndian">Whether or not data types should be read in big endian or not</param>
/// <param name="is64Bit">Whether or not pointers on this platform are 64-bit or 32-bit</param>
/// <param name="defaultVersion">The default file version for this platform</param>
public sealed class SlPlatform(string extension, bool isBigEndian, bool is64Bit, int defaultVersion)
{
    public static readonly SlPlatform Win32 = new("pc", false, false, 0x22);
    public static readonly SlPlatform Win64 = new("p2", false, true, 0x2e);
    public static readonly SlPlatform Android = new("ao", false, false, 0x27);
    public static readonly SlPlatform IOS = new("ip", false, false, 0x27);
    public static readonly SlPlatform WiiU = new("wu", true, false, 0x22);
    public static readonly SlPlatform Ps3 = new("p3", true, false, 0x17);
    public static readonly SlPlatform Xbox360 = new("xb", true, false, 0x22);
    public static readonly SlPlatform Vita = new("vt", false, false, 0x22);
    
    // used for both the switch and the ps4
    public static readonly SlPlatform NextGen = new("p4", false, true, 0x2e);

    // Wii never had SART or TSR, so the scene extension is null
    public static readonly SlPlatform Wii = new(string.Empty, true, false, 0x0);

    /// <summary>
    ///     The extension used for this platform's scene files.
    /// </summary>
    public readonly string Extension = extension;

    /// <summary>
    ///     Whether or not data types should be read in big endian or not.
    /// </summary>
    public readonly bool IsBigEndian = isBigEndian;

    /// <summary>
    ///     Whether or not pointers on this platform are 64-bit or 32-bit.
    /// </summary>
    public readonly bool Is64Bit = is64Bit;

    /// <summary>
    ///     The default file version for this platform.
    /// </summary>
    public readonly int DefaultVersion = defaultVersion;

    /// <summary>
    ///     Attempts to guess the platform of a resource from a file path.
    /// </summary>
    /// <param name="path">File path</param>
    /// <returns>Platform this resource belongs to</returns>
    public static SlPlatform GuessPlatformFromExtension(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.ToLowerInvariant() switch
        {
            ".spc" => Win32,
            ".sp2" => Win64,
            ".sao" => Android,
            ".swu" => WiiU,
            ".sp4" => NextGen,
            ".sp3" => Ps3,
            ".sxb" => Xbox360,
            ".sip" => IOS,
            ".svt" => Vita,
            _ => Win32
        };
    }

    public int GetPointerSize()
    {
        return Is64Bit ? sizeof(long) : sizeof(int);
    }

    public BinaryReader GetReader(Stream stream)
    {
        if (isBigEndian) return new BinaryReaderBE(stream);
        return new BinaryReaderLE(stream);
    }

    public BinaryWriter GetWriter(Stream stream)
    {
        if (isBigEndian) return new BinaryWriterBE(stream);
        return new BinaryWriterLE(stream);
    }

    public void WriteInt32(Span<byte> destination, int value)
    {
        if (isBigEndian) BinaryPrimitives.WriteInt32LittleEndian(destination, value);
        else BinaryPrimitives.WriteInt32LittleEndian(destination, value);
    }

    public int ReadInt32(ReadOnlySpan<byte> source)
    {
        if (isBigEndian)
            return BinaryPrimitives.ReadInt32BigEndian(source);
        return BinaryPrimitives.ReadInt32LittleEndian(source);
    }
}