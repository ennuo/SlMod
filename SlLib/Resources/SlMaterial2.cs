using System.Buffers.Binary;
using System.Numerics;
using SharpGLTF.Schema2;
using SlLib.Resources.Buffer;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources;

public class SlMaterial2 : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     The shader used by this material.
    /// </summary>
    public SlResPtr<SlShader> Shader = new();

    /// <summary>
    ///     Samplers used by this material.
    /// </summary>
    public List<SlSampler> Samplers = [];
    
    /// <summary>
    ///     Not sure if that's actually what this is, but need to keep track of it nonetheless.
    /// </summary>
    public int Flags;

    /// <summary>
    ///     Sampler lookup by index.
    /// </summary>
    public readonly SlSampler?[] IndexToSampler = new SlSampler[16];

    /// <summary>
    ///     The constant buffers for this material.
    /// </summary>
    public List<SlConstantBuffer> ConstantBuffers = [];

    /// <summary>
    ///     Constant buffer lookup by index.
    /// </summary>
    public readonly SlConstantBuffer?[] IndexToConstantBuffer = new SlConstantBuffer[16];
    
    private List<ArraySegment<byte>> _shaderProgramDatas = [];
    private List<int> _shaderProgramOffsets = [];
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        Shader = context.LoadResourcePointer<SlShader>();

        // SlMaterialBinding, not used on SART-PC versions,
        // so I don't particularly care about it
        context.ReadPointer();
        
        int maxSamplers = context.Version >= SlPlatform.Win64.DefaultVersion ? 16 : 8;
        int maxConstantBuffers = context.Version >= SlPlatform.Win64.DefaultVersion ? 14 : 8;
        
        int bufferData, bufferOffsetData, numBuffers;
        
        // Around Android's version, they moved the counts below the pointers.
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            bufferData = context.ReadPointer(); 
            bufferOffsetData = context.ReadPointer();
            
            int samplerData = context.ReadPointer();
            for (int i = 0; i < maxSamplers; ++i)
                IndexToSampler[i] = context.LoadPointer<SlSampler>();
            
            int constantBufferData = context.ReadPointer();
            for (int i = 0; i < maxConstantBuffers; ++i)
                IndexToConstantBuffer[i] = context.LoadPointer<SlConstantBuffer>();
            
            numBuffers = context.ReadInt32();
            int numSamplers = context.ReadInt32();
            Flags = context.ReadInt32();
            int numConstantBuffers = context.ReadInt32();

            ConstantBuffers = context.LoadArray<SlConstantBuffer>(constantBufferData, numConstantBuffers);
            Samplers = context.LoadArray<SlSampler>(samplerData, numSamplers);
        }
        else
        {
            numBuffers = context.ReadInt32();
            bufferData = context.ReadPointer();
            bufferOffsetData = context.ReadPointer();
            
            Samplers = context.LoadArrayPointer<SlSampler>(context.ReadInt32());
            Flags = context.ReadInt32(); // ???
            for (int i = 0; i < maxSamplers; ++i)
                IndexToSampler[i] = context.LoadPointer<SlSampler>();

            ConstantBuffers = context.LoadArrayPointer<SlConstantBuffer>(context.ReadInt32());
            for (int i = 0; i < maxConstantBuffers; ++i)
                IndexToConstantBuffer[i] = context.LoadPointer<SlConstantBuffer>();
        }

        context.ReadPointer(); // Platform pointer
        
        for (int i = 0; i < numBuffers; ++i)
            _shaderProgramDatas.Add(context.LoadBuffer(bufferData + (i * 0x30), 0x30, false));
        for (int i = 0; i < numBuffers - 1; ++i)
            _shaderProgramOffsets.Add(context.ReadPointer(bufferOffsetData + (i * context.Platform.GetPointerSize())) - bufferData);
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.SaveResource(buffer, Shader, 0xc);
        // Write those weird shader buffers first
        context.WriteInt32(buffer, _shaderProgramDatas.Count, 0x14);
        ISaveBuffer shaderProgramOffsetData =
            context.SaveGenericPointer(buffer, 0x1c, _shaderProgramOffsets.Count * 0x4);
        ISaveBuffer shaderProgramData = context.SaveGenericPointer(buffer, 0x18, _shaderProgramDatas.Count * 0x30);
        for (int i = 0; i < _shaderProgramOffsets.Count; ++i)
            context.WritePointerAtOffset(shaderProgramOffsetData, i * 4,
                _shaderProgramOffsets[i] + shaderProgramData.Address);
        for (int i = 0; i < _shaderProgramDatas.Count; ++i)
            context.WriteBuffer(shaderProgramData, _shaderProgramDatas[i], i * 0x30);

        context.WriteInt32(buffer, Samplers.Count, 0x20);
        ISaveBuffer samplerData = context.SaveGenericPointer(buffer, 0x24, Samplers.Count * 0x28);
        for (int i = 0; i < Samplers.Count; ++i)
            context.SaveReference(samplerData, Samplers[i], i * 0x28);

        context.WriteInt32(buffer, Flags, 0x28);
        context.WriteInt32(buffer, ConstantBuffers.Count, 0x4c);

        // This is really dumb, but I'm trying to maintain order of the original data
        // for verification purposes.
        ISaveBuffer constantBufferData = context.SaveGenericPointer(buffer, 0x50, ConstantBuffers.Count * 0x30);
        for (int i = 0; i < ConstantBuffers.Count; ++i)
        {
            SlConstantBuffer cb = ConstantBuffers[i];
            int address = i * 0x30; // this is so duuuumb
            context.SaveReference(constantBufferData, cb, address);

            if (cb.Data.Count == 0) continue;
            context.SaveBufferPointer(constantBufferData, cb.Data, address + 0x20, align: 0x20);
        }

        for (int i = 0; i < ConstantBuffers.Count; ++i)
            context.SaveObject(constantBufferData, ConstantBuffers[i].Header, i * 0x30);

        context.SaveObject(buffer, Header, 0);
        for (int i = 0; i < 8; ++i)
            context.SavePointer(buffer, IndexToSampler[i], 0x2c + (i * 0x4));
        for (int i = 0; i < 8; ++i)
            context.SavePointer(buffer, IndexToConstantBuffer[i], 0x54 + (i * 0x4));
        
        context.SavePointer(buffer, this, 0x74);
    }
    
    public void SetTexture(string name, SlTexture texture)
    {
        SlSampler? sampler = Samplers.Find(s => s.Header.Name == name);
        if (sampler != null) sampler.Texture = new SlResPtr<SlTexture>(texture);
    }
    
    public bool HasConstant(string name)
    {
        return ConstantBuffers.Any(buffer => buffer.Chunk.Members.Exists(member => member.Name == name));
    }

    public Vector4 GetConstant(string name)
    {
        Vector4 value = new Vector4(0, 0, 0, 1);
        foreach (SlConstantBuffer buffer in ConstantBuffers)
        {
            if (buffer.Data.Count == 0) continue;
            SlConstantBufferMember? member = buffer.Chunk.Members.Find(member => member.Name == name);
            if (member == null) continue;
            for (int i = 0; i < member.MaxComponents; ++i)
            {
                int offset = member.Offset + (i * 4);
                var span = buffer.Data.AsSpan(offset, 4);
                if (buffer.IsBigEndian)
                    value[i] = BinaryPrimitives.ReadSingleBigEndian(span);
                else
                    value[i] = BinaryPrimitives.ReadSingleLittleEndian(span);
            }
            
            return value;
        }
        
        return value;
    }
    
    public void SetConstant(string name, Vector4 value)
    {
        foreach (SlConstantBuffer buffer in ConstantBuffers)
        {
            if (buffer.Data.Count == 0) continue;
            SlConstantBufferMember? member = buffer.Chunk.Members.Find(member => member.Name == name);
            if (member == null) continue;
            for (int i = 0; i < member.Components; ++i)
            {
                int offset = member.Offset + (i * 4);
                var span = buffer.Data.AsSpan(offset, 4);
                if (buffer.IsBigEndian)
                    BinaryPrimitives.WriteSingleBigEndian(span, value[i]);
                else
                    BinaryPrimitives.WriteSingleLittleEndian(span, value[i]);
            }
        }
    }
    
    public void CopyDataFrom(SlMaterial2 material)
    {
        Flags = material.Flags;
        
        // Copy any texture instances
        foreach (SlSampler sampler in Samplers)
        {
            bool isDiffuseTexture = sampler.Header.Name is "gAlbedoTexture" or "gDiffuseTexture";
            SlSampler? otherSampler = material.Samplers.Find(s =>
            {
                if (isDiffuseTexture)
                    return s.Header.Name is "gAlbedoTexture" or "gDiffuseTexture";
                
                return s.Header.Name == sampler.Header.Name;
            });
            if (otherSampler != null)
            {
                sampler.Texture = otherSampler.Texture;   
            }
        }
        
        // Copy all constant data
        foreach (SlConstantBuffer buffer in material.ConstantBuffers)
        {
            if (buffer.Data.Count == 0) continue;
            foreach (SlConstantBufferMember member in buffer.Chunk.Members)
            {
                // Only supporting copying vector/scalar constants, matrices shouldn't be included
                // in serialized constant buffers anyway.
                if (member.Dimensions >= 2) continue;
                var value = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
                for (int i = 0; i < member.MaxComponents; ++i)
                {
                    int offset = member.Offset + (i * 4);
                    var span = buffer.Data.AsSpan(offset, 4);
                    if (buffer.IsBigEndian)
                        value[i] = BinaryPrimitives.ReadSingleBigEndian(span);
                    else
                        value[i] = BinaryPrimitives.ReadSingleLittleEndian(span);
                }
                
                SetConstant(member.Name, value);
            }
        }
    }

    public void PrintConstantValues()
    {
        Console.WriteLine(Header.Name);
        foreach (SlConstantBuffer buffer in ConstantBuffers)
        {
            if (buffer.Data.Count == 0) continue;
            foreach (SlConstantBufferMember member in buffer.Chunk.Members)
            {
                Vector4 original = new Vector4(0, 0, 0, 0);
                for (int i = 0; i < member.MaxComponents; ++i)
                {
                    int offset = member.Offset + (i * 4);
                    var span = buffer.Data.AsSpan(offset, 4);
                    if (buffer.IsBigEndian)
                        original[i] = BinaryPrimitives.ReadSingleBigEndian(span);
                    else
                        original[i] = BinaryPrimitives.ReadSingleLittleEndian(span);
                }
                
                Console.WriteLine($"\t{member.Name} -> float{member.Components}{original}");
            }
        }
        Console.WriteLine();
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x78;
    }
}