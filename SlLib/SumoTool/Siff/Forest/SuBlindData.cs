using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Forest.Blind;

namespace SlLib.SumoTool.Siff.Forest;

public class SuBlindData : IResourceSerializable
{
    public const int MaximalTreeCollisionOobbTypeHash = -0x6F8177CE;
    public const int FloatPairsInstanceHash = -0x265A4D45;
    public const int UintPairsInstanceHash = 0x7796ebea;
    
    public List<Element> Elements = [];
    
    public void Load(ResourceLoadContext context)
    {
        int numElements = context.ReadInt32();
        for (int i = 0; i < numElements; ++i)
        {
            int instanceHash = context.ReadInt32();
            int typeHash = context.ReadInt32();
            IResourceSerializable? data;
            
            if (typeHash == MaximalTreeCollisionOobbTypeHash)
                data = context.LoadPointer<MaximalTreeCollisionOOBB>();
            else if (instanceHash == FloatPairsInstanceHash)
                data = context.LoadPointer<SuNameFloatPairs>();
            else if (instanceHash == UintPairsInstanceHash)
                data = context.LoadPointer<SuNameUint32Pairs>();
            else throw new SerializationException($"Unsupported SuBlindData element! 0x{instanceHash:x8}");
            
            
            Elements.Add(new Element { InstanceHash = instanceHash, TypeHash = typeHash, Data = data });
        }
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, Elements.Count, 0x0);
        for (int i = 0; i < Elements.Count; ++i)
        {
            Element element = Elements[i];
            int offset = 0x4 + (i * 0xc);
            
            context.WriteInt32(buffer, element.InstanceHash, offset);
            context.WriteInt32(buffer, element.TypeHash, offset + 4);

            int align = element.TypeHash == MaximalTreeCollisionOobbTypeHash ? 0x10 : 0x4;
            context.SavePointer(buffer, element.Data, offset + 8, align);
        }
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x4 + Elements.Count * 0xc;
    }
    
    public class Element
    {
        public int InstanceHash;
        public int TypeHash;
        public IResourceSerializable? Data;
    }
}