using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using SlLib.Resources.Database;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Platform;

public class SlModelSegmentWiiU : SlModelSegment
{
    /// <summary>
    ///     Whether or not this segment is skinned.
    /// </summary>
    public bool IsSkinned => SkinnedFormat != null;
    
    /// <summary>
    ///     The vertex declaration used in a skinning context.
    /// </summary>
    public SlVertexDeclaration? SkinnedFormat;
    
    /// <summary>
    ///     The indices of the joints used by this segment.
    /// </summary>
    public readonly List<short> Indices = [];
    
    public int VertexCount;
    
    /// <summary>
    ///     Loads the Wii U platform version of an SlModelSegment.
    /// </summary>
    /// <param name="context">The current load context</param>
    public override void Load(ResourceLoadContext context)
    {
        PrimitiveType = (SlPrimitiveType)context.ReadInt32(); // 0x0
        VertexStart = context.ReadInt32(); // 0x4
        FirstIndex = context.ReadInt32(); // 0x8
        
        Sectors = context.LoadArrayPointer<SlModelSector>(context.ReadInt32()); // 0x10[0xc]
        Format = context.LoadPointer<SlVertexDeclaration>()!; // 0x14
        for (int i = 0; i < 3; ++i)
            VertexStreams[i] = context.LoadPointer<SlStream>(); // 0x18, 0x1c, 0x20
        
        IndexStream = context.LoadPointer<SlStream>()!; // 0x24
        SkinnedFormat = context.LoadPointer<SlVertexDeclaration>(); // 0x28
        WeightBuffer = context.LoadBufferPointer(Sector.NumVerts * 0x4, out _); // 0x2c
        
        context.ReadInt32(); // Maybe a boolean for is skinned?, 0x30
        int skinData = context.ReadInt32(); // 0x34
        if (skinData != 0) 
        {
            VertexCount = context.ReadInt32(skinData); // ???
            int numJoints = context.ReadInt32(skinData + 0x4);
            int jointData = context.ReadInt32(skinData + 0x8);
            for (int i = 0; i < numJoints; ++i)
                Indices.Add(context.ReadInt16(jointData + (i * 0x2)));
        }
        JointBuffer = context.LoadBufferPointer(Sector.NumVerts * 0x4, out _); // 0x38
        // 0x3c
        // 0x40
    }
    
    public override int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return 0x44;
    }
}

