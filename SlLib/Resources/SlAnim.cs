using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using SlLib.Resources.Database;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources;

// cmd buffer @ 0x3e
    // constant position
    // constant scale
    // constant attributes
    // position
    // scale


public class SlAnim : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();
    
    /// <summary>
    ///     The skeleton used by this animation.
    /// </summary>
    public SlResPtr<SlSkeleton> Skeleton = new();

    public float AnimationTime;
    public float FrameRate;
    public short FrameCount;
    public short BoneCount;
    public short AttributeCount;
    
    public List<short> ConstantRotationJoints = [];
    public List<short> RotationJoints = [];
    public List<short> ConstantPositionJoints = [];
    public List<short> PositionJoints = [];
    public List<short> ConstantScaleJoints = [];
    public List<short> ScaleJoints = [];
    public List<short> ConstantAttributeIndices = [];
    public List<short> AttributeIndices = [];
    
    public List<int> ConstantRotationFrameCommands = [];
    public List<int> RotationFrameCommands = [];
    public List<int> ConstantPositionFrameCommands = [];
    public List<int> PositionFrameCommands = [];
    public List<int> ConstantScaleFrameCommands = [];
    public List<int> ScaleFrameCommands = [];
    public List<int> ConstantAttributeFrameCommands = [];
    public List<int> AttributeFrameCommands = [];
    
    public byte RotationType = 0x3; /* 0x4 = F16, otherwise S16N */
    public byte PositionType = 0x1; /* 0x1 = FLOAT */
    public byte ScaleType = 0x1; /* 0x1 = FLOAT */
    public byte AttributeType = 0x1; /* 0x1 = FLOAT */

    public ArraySegment<byte> RotationAnimData = new([]);
    public ArraySegment<byte> PositionAnimData = new([]);
    public ArraySegment<byte> ScaleAnimData = new([]);
    public ArraySegment<byte> AttributeAnimData = new([]);
    public List<SlAnimBlendBranch> BlendBranches = [];
    
    public void Load(ResourceLoadContext context)
    {
        // I'm not going to parse the actual animation information right now,
        // but I just need at least enough to reverse the endianness

        Header = context.LoadObject<SlResourceHeader>();
        int animPoseData = context.ReadPointer();
        Skeleton = context.LoadResourcePointer<SlSkeleton>();

        context.Position = animPoseData;
        if (context.ReadInt32() != 0x414E494D /* ANIM */)
            throw new SerializationException("Not a valid animation resource! Magic is incorrect!");
        
        int someAddressPointer = animPoseData + context.ReadInt32(); // ???
        int firstBlendLeaf = animPoseData + context.ReadInt32();
        AnimationTime = context.ReadFloat();
        FrameRate = context.ReadFloat();
        
        BoneCount = context.ReadInt16();
        AttributeCount = context.ReadInt16();
        FrameCount = context.ReadInt16();
        int numBlendBranches = context.ReadInt16();
        
        int numConstantRotationFrames = context.ReadInt16();
        int numConstantPositionFrames = context.ReadInt16();
        int numConstantScaleFrames = context.ReadInt16();
        int numConstantAttributeFrames = context.ReadInt16();
        int numRotationFrames = context.ReadInt16();
        int numPositionFrames = context.ReadInt16();
        int numScaleFrames = context.ReadInt16();
        int numAttributeFrames = context.ReadInt16();

        RotationType = context.ReadInt8();
        PositionType = context.ReadInt8();
        ScaleType = context.ReadInt8();
        AttributeType = context.ReadInt8();

        int rotationAnimData = context.Position + (context.ReadInt16() & 0xffff);
        int positionAnimData = context.Position + (context.ReadInt16() & 0xffff);
        int scaleAnimData = context.Position + (context.ReadInt16() & 0xffff);
        int attributeAnimData = context.Position + (context.ReadInt16() & 0xffff);
        int jointData = context.Position + (context.ReadInt16() & 0xffff);
        int blendBranchData = context.Position + (context.ReadInt16() & 0xffff);
        int blendLeafData = animPoseData + (context.ReadInt16() & 0xffff);
        int commandData = context.Position + (context.ReadInt16() & 0xffff);
        
        // Read commands for all buffers
        context.Position = commandData;
        ReadCommands(ConstantRotationFrameCommands, numConstantRotationFrames);
        ReadCommands(ConstantPositionFrameCommands, numConstantPositionFrames);
        ReadCommands(ConstantScaleFrameCommands, numConstantScaleFrames);
        ReadCommands(ConstantAttributeFrameCommands, numConstantAttributeFrames);
        ReadCommands(RotationFrameCommands, numRotationFrames);
        ReadCommands(PositionFrameCommands, numPositionFrames);
        ReadCommands(ScaleFrameCommands, numScaleFrames);
        ReadCommands(AttributeFrameCommands, numAttributeFrames);

        context.Position = jointData;
        ReadJoints(ConstantRotationJoints, numConstantRotationFrames);
        ReadJoints(ConstantPositionJoints, numConstantPositionFrames);
        ReadJoints(ConstantScaleJoints, numConstantScaleFrames);
        ReadJoints(ConstantAttributeIndices, numConstantAttributeFrames);
        ReadJoints(RotationJoints, numRotationFrames);
        ReadJoints(PositionJoints, numPositionFrames);
        ReadJoints(ScaleJoints, numScaleFrames);
        ReadJoints(AttributeIndices, numAttributeFrames);
        
        RotationAnimData = context.LoadBuffer(rotationAnimData, numConstantRotationFrames * 0x6, false);
        PositionAnimData = context.LoadBuffer(positionAnimData, CalculateBufferSize(ConstantPositionFrameCommands, 3), false);
        ScaleAnimData = context.LoadBuffer(scaleAnimData, CalculateBufferSize(ConstantScaleFrameCommands, 3), false);
        AttributeAnimData = context.LoadBuffer(attributeAnimData,
            CalculateBufferSize(ConstantAttributeFrameCommands, 1), false);
        
        for (int i = 0; i < numBlendBranches; ++i)
        {
            context.Position = (blendBranchData + (i * 0x10));
            var branch = new SlAnimBlendBranch
            {
                FrameOffset = context.ReadInt32(),
                NumFrames = context.ReadInt32(),
                Flags = context.ReadInt32()
            };

            int leafData = animPoseData + context.ReadInt32();
            context.Position = leafData;
            
            var leaf = new SlAnimBlendLeaf
            {
                FrameOffset = context.ReadInt16(), // 0x0
                NumFrames = context.ReadInt16() // 0x2
            };
            
            // 0 = rotation basis data (0x4)
            // 1 = position basis data (0x6)
            // 2 = scale basis data (0x8)
            // 3 = attribute basis data (0xa)
            // 4 = rotation frame data (0xc)
            // 5 = position frame data (0xe)
            // 6 = scale frame data (0x10)
            // 7 = attribute frame data (0x12)
            // 8 = frame masks (0x14)
            // 9 = unknown for rotation data (0x16)
            // 10 = unknown for position data (0x18)
            // 11 = unknown for scale data (0x1a)
            // 12 = unknown for attribute data (0x1c)
            // 13 = unknown bits for each bone (0x1e)
            
            for (int j = 0; j < 0xe; ++j) // 0x4 -> 0x20
                leaf.Offsets[j] = (short)(context.ReadInt16() - 0x28);
            short size = context.ReadInt16(); // 0x20
            leaf.Data = context.LoadBuffer(leafData + 0x28, size - 0x28, false);
            branch.Leaf = leaf;
            
            branch.Debug_DataSize = size - 0x28;

            // int frames = leaf.NumFrames;
            
            // if (AttributeIndices.Count != 0)
            // {
            //     Console.WriteLine("Guessing data size of attributes channel");
            //     
            //     int channelSize = 0;
            //     for (int j = 0; j < numAttributeFrames; ++j)
            //     {
            //         int startSize = channelSize;
            //         
            //         int offset = leaf.Offsets[8] + (j * ((frames + 7) >> 3)) + (numRotationFrames * ((frames + 7) >> 3));
            //         int bits = leaf.Data[offset++];
            //         int remaining = 8;
            //         
            //         for (int k = 0; k < frames; ++k)
            //         {
            //             if (((bits >> (remaining - 1)) & 1) != 0)
            //                 channelSize += SlUtil.SumoAnimGetStrideFromBitPacked(AttributeFrameCommands[j]);
            //         
            //             remaining -= 1;
            //             if (remaining == 0)
            //             {
            //                 bits = leaf.Data[offset++];
            //                 remaining = 8;
            //             }
            //         }
            //
            //         if (startSize == channelSize)
            //         {
            //             Console.WriteLine($"Attribute {j} wasn't animated!");
            //         }
            //     }
            //
            //     channelSize = (channelSize + 7) / 8;
            //     Console.WriteLine($"0x{channelSize:x}");
            // }
            //
            // if (RotationJoints.Count != 0)
            // {
            //     Console.WriteLine("Guessing data size of rotation channel");
            //     
            //     int channelSize = 0;
            //     for (int j = 0; j < numRotationFrames; ++j)
            //     {
            //         int startSize = channelSize;
            //         
            //         int offset = leaf.Offsets[8] + j * ((frames + 7) >> 3);
            //         int bits = leaf.Data[offset++];
            //         int remaining = 8;
            //         
            //         for (int k = 0; k < frames; ++k)
            //         {
            //             if (((bits >> (remaining - 1)) & 1) != 0) channelSize += 6;
            //         
            //             remaining -= 1;
            //             if (remaining == 0)
            //             {
            //                 bits = leaf.Data[offset++];
            //                 remaining = 8;
            //             }
            //         }
            //
            //         if (startSize == channelSize)
            //         {
            //             Console.WriteLine($"Rotation bone {j} wasn't animated!");
            //         }
            //     }
            //     
            //     Console.WriteLine($"0x{channelSize:x}");
            // }

            // for (int j = 0; j < 0xe; ++j)
            // {
            //     if (leaf.Offsets[j] == leaf.Data.Count)
            //         leaf.Offsets[j] = -1;
            // }
            //
            BlendBranches.Add(branch);
        }
        
        return;

        void ReadCommands(ICollection<int> commands, int count)
        {
            for (int i = 0; i < count; ++i)
                commands.Add(context.ReadInt32());
        }

        void ReadJoints(ICollection<short> joints, int count)
        {
            for (int i = 0; i < count; ++i)
                joints.Add(context.ReadInt16());
        }
        
        int CalculateBufferSize(IReadOnlyList<int> commands, int numComponents)
        {
            int bits = 0;
            for (int i = 0; i < commands.Count; ++i)
            {
                int c = commands[i];
                for (int j = 0; j < numComponents; ++j)
                {
                    bits += (c >>> 0x1f) + ((c << 1) >>> 0x1c) + ((c << 5) >>> 0x1b);
                    c <<= 10;
                }
            }
            
            return (bits + 7) / 8;
        }
    }
    
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        int compressedAnimationsSize = (
                                        ConstantRotationJoints.Count +
                                        RotationJoints.Count +
                                        ConstantPositionJoints.Count +
                                        ConstantScaleJoints.Count +
                                        ConstantAttributeIndices.Count +
                                        PositionJoints.Count +
                                        ScaleJoints.Count +
                                        AttributeIndices.Count);
        
        int animDataSize = 0x40;
        int commandBufferData = animDataSize;
        animDataSize += compressedAnimationsSize * 4;
        int jointData = animDataSize;
        animDataSize += compressedAnimationsSize * 2;
        int rotationAnimData = animDataSize;
        animDataSize += RotationAnimData.Count;
        int positionAnimData = animDataSize;
        animDataSize += PositionAnimData.Count;
        int scaleAnimData = animDataSize;
        animDataSize += ScaleAnimData.Count;
        int attributeAnimData = animDataSize;
        animDataSize += AttributeAnimData.Count;
        int frameDataSize = animDataSize;
        animDataSize = SlUtil.Align(animDataSize, 0x10);
        int blendBranchData = animDataSize;
        animDataSize += (BlendBranches.Count * 0x10);
        int blendLeafData = animDataSize;
        int[] blendLeafOffsets = new int[BlendBranches.Count];
        for (int i = 0; i < BlendBranches.Count; ++i)
        {
            SlAnimBlendBranch branch = BlendBranches[i];
            SlAnimBlendLeaf leaf = branch.Leaf;
            blendLeafOffsets[i] = animDataSize;
            animDataSize += (0x28 + leaf.Data.Count);
        }

        animDataSize = SlUtil.Align(animDataSize, 0x10);
        
        ISaveBuffer animData = context.SaveGenericPointer(buffer, 0xc, animDataSize, align: 0x10);
        context.SaveObject(buffer, Header, 0x0);
        context.SaveResource(buffer, Skeleton, 0x10);
        
        context.WriteInt32(animData, 0x414E494D, 0x0); /* ANIM */
        context.WriteInt32(animData, frameDataSize, 0x4);
        context.WriteInt32(animData, blendLeafData, 0x8);
        context.WriteFloat(animData, AnimationTime, 0xc);
        context.WriteFloat(animData, FrameRate, 0x10);
        context.WriteInt16(animData, BoneCount, 0x14);
        context.WriteInt16(animData, AttributeCount, 0x16);
        context.WriteInt16(animData, FrameCount, 0x18);
        context.WriteInt16(animData, (short)BlendBranches.Count, 0x1a);
        context.WriteInt16(animData, (short)ConstantRotationJoints.Count, 0x1c);
        context.WriteInt16(animData, (short)ConstantPositionJoints.Count, 0x1e);
        context.WriteInt16(animData, (short)ConstantScaleJoints.Count, 0x20);
        context.WriteInt16(animData, (short)ConstantAttributeIndices.Count, 0x22);
        context.WriteInt16(animData, (short)RotationJoints.Count, 0x24);
        context.WriteInt16(animData, (short)PositionJoints.Count, 0x26);
        context.WriteInt16(animData, (short)ScaleJoints.Count, 0x28);
        context.WriteInt16(animData, (short)AttributeIndices.Count, 0x2a);
        context.WriteInt8(animData, RotationType, 0x2c);
        context.WriteInt8(animData, PositionType, 0x2d);
        context.WriteInt8(animData, ScaleType, 0x2e);
        context.WriteInt8(animData, AttributeType, 0x2f);
        
        context.WriteInt16(animData, (short)(rotationAnimData - 0x30), 0x30);
        context.WriteInt16(animData, (short)(positionAnimData - 0x32), 0x32);
        context.WriteInt16(animData, (short)(scaleAnimData - 0x34), 0x34);
        context.WriteInt16(animData, (short)(attributeAnimData - 0x36), 0x36);
        context.WriteInt16(animData, (short)(jointData - 0x38), 0x38);
        context.WriteInt16(animData, (short)(blendBranchData - 0x3a), 0x3a);
        context.WriteInt16(animData, (short)(blendLeafData), 0x3c);
        context.WriteInt16(animData, (short)(commandBufferData - 0x3e), 0x3e);

        int commandOffset = commandBufferData;
        int jointOffset = jointData;
        
        WriteCommands(ConstantRotationFrameCommands);
        WriteCommands(ConstantPositionFrameCommands);
        WriteCommands(ConstantScaleFrameCommands);
        WriteCommands(ConstantAttributeFrameCommands);
        WriteCommands(RotationFrameCommands);
        WriteCommands(PositionFrameCommands);
        WriteCommands(ScaleFrameCommands);
        WriteCommands(AttributeFrameCommands);
        
        WriteJoints(ConstantRotationJoints);
        WriteJoints(ConstantPositionJoints);
        WriteJoints(ConstantScaleJoints);
        WriteJoints(ConstantAttributeIndices);
        WriteJoints(RotationJoints);
        WriteJoints(PositionJoints);
        WriteJoints(ScaleJoints);
        WriteJoints(AttributeIndices);

        context.WriteBuffer(animData, RotationAnimData, rotationAnimData);
        context.WriteBuffer(animData, PositionAnimData, positionAnimData);
        context.WriteBuffer(animData, ScaleAnimData, scaleAnimData);
        context.WriteBuffer(animData, AttributeAnimData, attributeAnimData);

        for (int i = 0; i < BlendBranches.Count; ++i)
        {
            SlAnimBlendBranch branch = BlendBranches[i];
            int address = blendBranchData + (i * 0x10);
            
            context.WriteInt32(animData, branch.FrameOffset, address);
            context.WriteInt32(animData, branch.NumFrames, address + 4);
            context.WriteInt32(animData, branch.Flags, address + 8);
            context.WriteInt32(animData, blendLeafOffsets[i], address + 12);

            SlAnimBlendLeaf leaf = branch.Leaf;
            address = blendLeafOffsets[i];
            context.WriteInt16(animData, leaf.FrameOffset, address);
            context.WriteInt16(animData, leaf.NumFrames, address + 2);
            for (int j = 0; j < 0xe; ++j)
                context.WriteInt16(animData, (short)(leaf.Offsets[j] + 0x28), address + 4 + (j * 2));
            context.WriteInt16(animData, (short)(leaf.Data.Count + 0x28), address + 0x20);
            context.WriteBuffer(animData, leaf.Data, address + 0x28);
        }
        
        return;

        void WriteCommands(IEnumerable<int> commands)
        {
            foreach (int command in commands)
            {
                context.WriteInt32(animData, command, commandOffset);
                commandOffset += 4;
            }
        }

        void WriteJoints(IEnumerable<short> joints)
        {
            foreach (short joint in joints)
            {
                context.WriteInt16(animData, joint, jointOffset);
                jointOffset += 2;
            }
        }
    }
    
    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x30 : 0x20;
    }

    public class SlAnimBlendBranch
    {
        public int FrameOffset;
        public int NumFrames;
        public int Flags;
        public int Debug_DataSize;
        public SlAnimBlendLeaf Leaf = new();
    }
    
    public class SlAnimBlendLeaf
    {
        public short FrameOffset;
        public short NumFrames;
        public short[] Offsets = new short[0xe];
        public ArraySegment<byte> Data = new byte[0x8];
    }
}