using SlLib.Serialization;
using SlLib.SumoTool.Siff.Objects;

namespace SlLib.SumoTool.Siff;

public class ObjectDefLibrary : ILoadable
{
    public readonly Dictionary<int, IObjectDef> Objects = [];

    /// <inheritdoc />
    public void Load(ResourceLoadContext context, int offset)
    {
        int numEntries = context.ReadInt32(offset + 8);
        int objectData = context.ReadInt32(offset + 16);
        for (int i = 0; i < numEntries; ++i)
        {
            int address = objectData + i * 16;

            int hash = context.ReadInt32(address);
            string type = context.ReadMagic(address + 4);

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

            objectDef.Load(context, context.ReadInt32(address + 8));
            Objects[hash] = objectDef;
        }
    }
}