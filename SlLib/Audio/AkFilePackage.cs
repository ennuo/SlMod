using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using SlLib.Extensions;

namespace SlLib.Audio;

public class AkFilePackage
{
    private const int FourCC = 0x4B504B41;
    private const int FileVersion = 1;

    public Dictionary<int, string> LanguageMap = [];
    public List<AkFileEntry<int>> SoundBanks = [];
    public List<AkFileEntry<int>> StreamedFiles = [];
    public List<AkFileEntry<long>> Externals = [];
    
    public static AkFilePackage Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Could not find AkFilePackage at {path}");
        
        AkFilePackage package = new();
        using FileStream fs = File.OpenRead(path);
        
        Span<byte> headerByteData = stackalloc byte[28];
        fs.ReadExactly(headerByteData);
        AkFileHeader header = MemoryMarshal.Cast<byte, AkFileHeader>(headerByteData)[0];
        if (header.FourCC != FourCC)
            throw new SerializationException("Not a valid AkFilePackage, file magic doesn't match!");

        int tableSize = header.LanguageMapSize + header.SoundBanksTableSize + header.StreamingFilesTableSize +
                        header.ExternalsTableSize;
        // if (header.HeaderSize < 0x1c + tableSize)
        // {
        //     throw new SerializationException("Header size does not contain enough data!");
        // }
        
        if (header.Version != FileVersion)
            throw new SerializationException("Unsupported AkFilePackage version!");

        byte[] tableData = new byte[tableSize];
        fs.ReadExactly(tableData);

        using var stream = new MemoryStream(tableData);
        using var reader = new BinaryReader(stream);
        long position = 0;

        int numStrings = reader.ReadInt32();
        for (int i = 0; i < numStrings; ++i)
        {
            int offset = reader.ReadInt32();
            int id = reader.ReadInt32();

            long link = stream.Position;
            stream.Position = position + offset;
            package.LanguageMap[id] = reader.ReadTerminatedUnicodeString();
            stream.Position = link;
        }

        position += header.LanguageMapSize;
        stream.Position = position;

        package.SoundBanks = [..new AkFileEntry<int>[reader.ReadInt32()]];
        stream.ReadExactly(MemoryMarshal.Cast<AkFileEntry<int>, byte>(CollectionsMarshal.AsSpan(package.SoundBanks)));
        
        position += header.SoundBanksTableSize;
        stream.Position = position;
        package.StreamedFiles = [..new AkFileEntry<int>[reader.ReadInt32()]];
        stream.ReadExactly(MemoryMarshal.Cast<AkFileEntry<int>, byte>(CollectionsMarshal.AsSpan(package.StreamedFiles)));
        
        position += header.StreamingFilesTableSize;
        stream.Position = position;
        package.Externals = [..new AkFileEntry<long>[reader.ReadInt32()]];
        stream.ReadExactly(MemoryMarshal.Cast<AkFileEntry<long>, byte>(CollectionsMarshal.AsSpan(package.Externals)));
        
        return package;
    }

    private struct AkFileHeader
    {
        public int FourCC;
        public int HeaderSize;
        public int Version;
        public int LanguageMapSize;
        public int SoundBanksTableSize;
        public int StreamingFilesTableSize;
        public int ExternalsTableSize;
    }
    
    public struct AkFileEntry<TFileId>
    {
        public TFileId FileId;
        public int BlockSize;
        public int FileSize;
        public int StartBlock;
        public int LanguageId;
    }
}