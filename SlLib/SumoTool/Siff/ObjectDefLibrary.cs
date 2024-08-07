using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.SumoTool.Siff.Objects;

namespace SlLib.SumoTool.Siff;

public class ObjectDefLibrary : IResourceSerializable
{
    public readonly Dictionary<int, IObjectDef> Objects = [];

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        context.Position += context.Platform.GetPointerSize() * 0x2;
        int numEntries = context.ReadInt32();
        int hashTable = context.ReadPointer();
        int objectData = context.ReadPointer();

        context.Position = objectData;
        for (int i = 0; i < numEntries; ++i)
        {
            int hash = context.ReadInt32();
            string type = context.ReadMagic();
            int address = context.ReadPointer();
            context.ReadPointer(); // SiffLoadSet
            
            // Dummy node I'm using to pad out quick lookup table until I feel like
            // figuring out how its calculated.
            if (type == "FUCK") continue;

            IObjectDef? objectDef = type switch
            {
                "GROP" => new GroupObject(),
                "TEXT" => new TextObject(),
                "TXTR" => new TextureObject(),
                "SCIS" => new ScissorObject(),
                "HELP" => new HelperObject(),
                "PNTR" => new PointerAreaObject(),
                "GORD" => new GouraudObject(),
                _ => null
            };

            if (objectDef == null)
                throw new NotSupportedException($"{type} is an unsupported object definition type!");

            int link = context.Position;
            context.Position = address;
            objectDef.Load(context);
            context.Position = link;

            Objects[hash] = objectDef;
        }
    }

    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        throw new NotImplementedException();
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return (platform.GetPointerSize() * 4) + 0x4;
    }
}