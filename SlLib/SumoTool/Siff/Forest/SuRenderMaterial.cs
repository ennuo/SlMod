using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.SumoTool.Siff.Forest;

public class SuRenderMaterial : IResourceSerializable
{
    /// <summary>
    ///     The name of this material.
    /// </summary>
    public string Name = string.Empty;
    
    /// <summary>
    ///     Textures used by this material.
    /// </summary>
    public List<SuRenderTexture> Textures = [];

    public int PixelShaderFlags;
    public int Hash;
    public List<float> FloatList = [];
    public List<int>[] Layers;
    
    // Don't particularly care what these represent right now,
    // just trying to convert files between platforms.
    public int Unknown_0x44;
    public int Unknown_0x48;
    public int Unknown_0x4c;
    public byte Unknown_0x51;
    public int Unknown_0x54;
    public int Unknown_0x64;
    
    public byte Unknown_0x68;
    public byte Unknown_0x69;
    public byte Unknown_0x6a;
    public byte Unknown_0x6b;
    
    public SuRenderMaterial()
    {
        Layers = new List<int>[6];
        for (int i = 0; i < 6; ++i)
            Layers[i] = [];
    }
    
    public void Load(ResourceLoadContext context)
    {
        int start = context.Position + context._data.Offset;
        
        context.ReadInt32(); // Always 0?
        PixelShaderFlags = context.ReadInt32();
        Hash = context.ReadInt32();
        FloatList = context.LoadArrayPointer(context.ReadInt32(), context.ReadFloat);
        
        Span<int> numLayers = stackalloc int[6];
        for (int i = 0; i < 6; ++i)
            numLayers[i] = context.ReadInt32();

        for (int i = 0; i < 6; ++i)
            Layers[i] = context.LoadArrayPointer(numLayers[i], context.ReadInt32);

        Unknown_0x44 = context.ReadInt32();
        Unknown_0x48 = context.ReadInt32();
        Unknown_0x4c = context.ReadInt32();

        if (context.ReadInt8() != 0) Console.WriteLine($"Unknown_0x50 was set in material!");
        Unknown_0x51 = context.ReadInt8();
        if (context.ReadInt16() != 0)
            Console.WriteLine($"Unknown_0x52_53 was set in material!");

        Unknown_0x54 = context.ReadInt32();
        
        if (context.ReadInt32() != 0)
            Console.WriteLine("Unknown_0x58 was set in material!");
        
        int numElements = context.ReadInt32();
        int elementData = context.ReadPointer();
        
        // These two only seem set in glowing materials?
        if (numElements != 0) // count for next pointer?
        {
            Console.WriteLine($"0x{(elementData + context._data.Offset):x8} [{numElements}]");   
        }

        Unknown_0x64 = context.ReadInt32();
        Unknown_0x68 = context.ReadInt8();
        Unknown_0x69 = context.ReadInt8();
        Unknown_0x6a = context.ReadInt8();
        Unknown_0x6b = context.ReadInt8();
        
        if (context.ReadInt32() != 0)
            Console.WriteLine("Unknown_0x6c was set in material!");
        if (context.ReadInt32() != 0)
            Console.WriteLine("Unknown_0x70 was set in material!");
        
        Textures = context.LoadPointerArray<SuRenderTexture>(context.ReadInt32());
        Name = context.ReadStringPointer();
        
        Console.WriteLine($"{Name} : 0x{start:x8}");
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Textures.Count, 0x74);
        context.SavePointerArray(buffer, Textures, 0x78);
        context.WriteStringPointer(buffer, Name, 0x7c);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x80; // 0x88 maybe?
    }
}