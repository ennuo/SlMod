using System.Diagnostics;
using System.Security.Cryptography;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources.Scene;

public abstract class SeNodeBase : IResourceSerializable
{
    public SlResourceType Debug_ResourceType;

    /// <summary>
    ///     Size of this class in memory.
    /// </summary>
    public int FileClassSize { get; private set; }

    /// <summary>
    ///     Node base flags.
    /// </summary>
    public int BaseFlags = 0x2187;

    /// <summary>
    ///     The unique identifier for this node.
    /// </summary>
    public int Uid { get; private set; }

    /// <summary>
    ///     The name of this node.
    /// </summary>
    public string UidName
    {
        get => _name;
        set
        {
            _name = value;
            Uid = SlUtil.HashString(_name);
            ShortName = GetShortName();
            Scene = GetScene();
            CleanName = GetNameWithoutTimestamp();
        }
    }
    private string _name = string.Empty;
    
    /// <summary>
    ///     The name of this node without the file path.
    /// </summary>
    public string ShortName { get; private set; } = string.Empty;
    
    /// <summary>
    ///     The maya scene this node belongs to.
    /// </summary>
    public string Scene { get; private set; } = string.Empty;

    /// <summary>
    ///     Short name without the instance timestamp
    /// </summary>
    public string CleanName { get; private set; } = string.Empty;

    /// <summary>
    ///     The tag of this node.
    /// </summary>
    public string Tag = string.Empty;
    
    /// <summary>
    ///     The prefix used for this node.
    /// </summary>
    public virtual string Prefix => string.Empty;

    /// <summary>
    ///     The extension used for this node.
    /// </summary>
    public virtual string Extension => string.Empty;

    protected SeNodeBase()
    {
        Debug_ResourceType = SlUtil.ResourceId(GetType().Name);
    }

    public static (TDef def, TInst inst) Create<TDef, TInst>() 
        where TDef : SeDefinitionNode, new()
        where TInst : SeInstanceNode, new()
    {
        var def = new TDef();
        def.SetNameWithTimestamp(typeof(TDef).Name);

        var inst = new TInst();
        inst.SetNameWithTimestamp(typeof(TInst).Name);

        inst.Definition = def;
        
        return (def, inst);
    }
    
    /// <summary>
    ///     Sets the name of this node and appends a timestamp.
    /// </summary>
    /// <param name="name">Name to set</param>
    public void SetNameWithTimestamp(string name)
    {
        Span<byte> storage = stackalloc byte[4];
        RandomNumberGenerator.Fill(storage);
        int value = BitConverter.ToInt32(storage);
        
        DateTime date = DateTime.Now;
        UidName =
            $"{name}[{date.Second:d2}.{date.Minute:d2}.{date.Hour:d2}.{date.Day:d2}.{date.Month:d2}.{date.Year:d4}.{(uint)(date.Second * 1000000000 + date.Nanosecond)}][{value:x8}]";
    }
    
    /// <summary>
    ///     Gets the node name without the associated path.
    /// </summary>
    /// <returns>Node short name</returns>
    private string GetShortName()
    {
        if (string.IsNullOrEmpty(UidName)) return "NoName";
        int start = UidName.Length;
        while (start > 0)
        {
            char c = UidName[start - 1];
            if (c is '|' or '\\' or '/' or ':') break;
            start--;
        }
        
        return UidName[start..];
    }

    private string GetNameWithoutTimestamp()
    {
        string name = ShortName;
        int index = name.LastIndexOf('[');
        if (index != -1)
            name = name[..index];
        return Path.GetFileNameWithoutExtension(name);
    }
    
    private string GetScene()
    {
        if (!UidName.Contains(':')) return string.Empty;
        string parent = Path.GetFileName(UidName.Split(':')[0]);
        return parent.EndsWith(".mb") ? Path.GetFileNameWithoutExtension(parent) : string.Empty;
    }
    
    /// <summary>
    ///     Loads this node from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    /// <param name="offset">The offset in the buffer to load from</param>
    /// <returns>The offset of the next class base</returns>
    protected virtual int LoadInternal(ResourceLoadContext context, int offset)
    {
        FileClassSize = context.ReadInt32(offset + 0x8);
        BaseFlags = context.ReadBitset32(offset + 0xc);
        // offset + 0x10 is old flags, but it seems in serialization, they should always be the same.
        
        // + 0x18 is an atomic int
        
 
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            UidName = context.ReadStringPointer(offset + 0x20);
            Tag = context.ReadStringPointer(offset + 0x28);
        }
        else
        {
            UidName = context.ReadStringPointer(offset + 0x1c);
            Tag = context.ReadStringPointer(offset + 0x24);
        }
        
        Uid = context.ReadInt32(offset + 0x14);
        
        return offset + 0x40;
    }
    
    /// <summary>
    ///     Loads this structure from a buffer.
    /// </summary>
    /// <param name="context">The current load context</param>
    public virtual void Load(ResourceLoadContext context)
    {
        LoadInternal(context, context.Position);
    }
    
    /// <summary>
    ///     Saves this node to a buffer.
    /// </summary>
    /// <param name="context">The current save context</param>
    /// <param name="buffer">The buffer to save to</param>
    public virtual void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        FileClassSize = GetSizeForSerialization(context.Platform, context.Version);
        
        context.WriteInt32(buffer, FileClassSize, 0x8);
        context.WriteInt32(buffer, BaseFlags, 0xc);
        context.WriteInt32(buffer, BaseFlags, 0x10);
        context.WriteInt32(buffer, Uid, 0x14);
        context.WriteStringPointer(buffer, UidName, 0x1c);
        context.WriteStringPointer(buffer, Tag, 0x24, allowEmptyString: true);
    }
    
    /// <summary>
    ///     Gets the base size of this structure for serialization, not including allocations.
    /// </summary>
    /// <param name="platform">The target platform</param>
    /// <param name="version">The target file version</param>
    /// <returns>Size for serialization</returns>
    public virtual int GetSizeForSerialization(SlPlatform platform, int version) => 0x40;
}