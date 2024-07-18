using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Logic;

namespace SlLib.SumoTool.Siff;

public class LogicData : IResourceSerializable
{
    public int NameHash;
    public int LogicVersion;
    
    public List<Trigger> Triggers = [];
    public List<TriggerAttribute> Attributes = [];
    public List<Locator> Locators = [];

    public void Load(ResourceLoadContext context)
    {
        NameHash = context.ReadInt32();
        LogicVersion = context.ReadInt32();
        
        int numTriggers = context.ReadInt32();
        int numLocators = context.ReadInt32();
        
        Triggers = context.LoadArrayPointer<Trigger>(numTriggers);
        
        int numAttributes = 0;
        foreach (Trigger trigger in Triggers)
            numAttributes = Math.Max(numAttributes, trigger.AttributeStartIndex + trigger.NumAttributes);
        Attributes = context.LoadArrayPointer<TriggerAttribute>(numAttributes);
        
        Locators = context.LoadArrayPointer<Locator>(numLocators);
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        context.WriteInt32(buffer, NameHash, 0x0);
        context.WriteInt32(buffer, LogicVersion, 0x4);
        
        context.WriteInt32(buffer, Triggers.Count, 0x8);
        context.WriteInt32(buffer, Locators.Count, 0xc);
        
        context.SaveReferenceArray(buffer, Triggers, 0x10, align: 0x10);
        context.SaveReferenceArray(buffer, Attributes, 0x14);
        context.SaveReferenceArray(buffer, Locators, 0x18, align: 0x10);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x20;
    }
}