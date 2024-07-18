using System.Runtime.Serialization;
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

    public List<int> UnknownNumberList = [];
    
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
        
        // I don't know if this is a list of integer lists
        int numElements = context.ReadInt32();
        if (numElements > 1)
            throw new SerializationException("Unsupported element count in material data!");
        
        int elementData = context.ReadPointer();
        if (elementData != 0)
        {
            if (context.ReadInt32(elementData) != 1)
                throw new SerializationException("Unsupported element value in material data!");
            
            UnknownNumberList.Add(context.ReadInt32(elementData + 4));
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
        
        //Console.WriteLine($"{Name} : 0x{start:x8}");
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, PixelShaderFlags, 0x4);
        context.WriteInt32(buffer, Hash, 0x8);

        ISaveBuffer textureListData = context.SaveGenericPointer(buffer, 0x78, Textures.Count * 4);

        if (UnknownNumberList.Count != 0)
        {
            context.WriteInt32(buffer, 1, 0x5c);
            ISaveBuffer numberListData = context.SaveGenericPointer(buffer, 0x60, 0x4 + UnknownNumberList.Count * 0x4);
            context.WriteInt32(numberListData, 1, 0x0);
            context.WriteInt32(numberListData, UnknownNumberList[0], 0x4);
        }
        
        context.WriteInt32(buffer, FloatList.Count, 0xc);
        ISaveBuffer floatListData = context.SaveGenericPointer(buffer, 0x10, FloatList.Count * 0x4);
        for (int i = 0; i < FloatList.Count; ++i)
            context.WriteFloat(floatListData, FloatList[i], i * 4);

        for (int i = 0; i < 6; ++i)
        {
            var layer = Layers[i];
            context.WriteInt32(buffer, layer.Count, 0x14 + (i * 4));
            if (layer.Count != 0)
            {
                ISaveBuffer layerData = context.SaveGenericPointer(buffer, 0x2c + (i * 4), layer.Count * 4);
                for (int j = 0; j < layer.Count; ++j)
                    context.WriteInt32(layerData, layer[j], j * 4);
            }
            
        }
        
        context.WriteInt32(buffer, Unknown_0x44, 0x44);
        context.WriteInt32(buffer, Unknown_0x48, 0x48);
        context.WriteInt32(buffer, Unknown_0x4c, 0x4c);
        
        context.WriteInt8(buffer, Unknown_0x51, 0x51);
        context.WriteInt32(buffer, Unknown_0x54, 0x54);
        
        context.WriteInt32(buffer, Unknown_0x64, 0x64);
        context.WriteInt32(buffer, Unknown_0x68, 0x68);
        context.WriteInt8(buffer, Unknown_0x69, 0x69);
        context.WriteInt8(buffer, Unknown_0x6a, 0x6a);
        context.WriteInt8(buffer, Unknown_0x6b, 0x6b);
        
        
        context.WriteInt32(buffer, Textures.Count, 0x74);
        //context.SavePointerArray(buffer, Textures, 0x78);
        
        for (int i = 0; i < Textures.Count; ++i)
            context.SavePointer(textureListData, Textures[i], i * 4);
        
        context.WriteStringPointer(buffer, Name, 0x7c);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x80;
    }
}