using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Serialization;

namespace SlLib.Resources.Model.Platform;

public class SlModelResourcePlatformWiiU : SlModelResourcePlatform
{ 
    /// <summary>
    ///     Wii specific command buffer.
    /// </summary>
    public List<int> CommandBuffer;

    /// <summary>
    ///     Cached information about segments from command buffer.
    /// </summary>
    public List<WiiSegmentInfo> SegmentInfos = [];
    
    /// <summary>
    ///     Work area for Wii command data.
    /// </summary>
    public ArraySegment<byte> WorkArea;
    
    public override void Load(ResourceLoadContext context)
    {
        base.Load(context);
        if (Resource == null)
            throw new SerializationException("Model resource pointer was invalid!");
        
        // Pre-initialize the segment info
        for (int i = 0; i < Resource.Segments.Count; ++i)
        {
            SegmentInfos.Add(new WiiSegmentInfo());
            Resource.Segments[i].MaterialIndex = -1;
        }

        int commandElementCount = context.ReadInt32(); // 0x1c
        int commandData = context.ReadPointer(); // 0x20
        int commandOffset = commandData;
        int displayListData = context.ReadPointer();
        int displayListWordOffset = 0;
        
        int currentFlags = 0;
        int currentLocatorIndex = -1;
        int currentMaterial = 0;
         for (int i = 0; i < commandElementCount; ++i)
         {
             short numWords = context.ReadInt16(commandOffset);
             short type = context.ReadInt16(commandOffset + 2);
             // Console.WriteLine($"Command[x{type:x2}] @ {commandOffset}");
             
             // 0x1 has material
             // 0x12 also has a material
             
             // Buffer starts with 0x8
                // then for each segment...
                    // 0x3, 0x6, 0x0d (if skinned), 0x07
                    // 0x00, 0x12, 0x0c
             // Buffer ends with 0x2, 0x0?
                    
             // 0x0 is for flushing display lists?
             // 0x3 is a visibility test?
                // 0x4 - int flags
                // 0x8 - int Offset in buffer to skip to if test fails
             // 0x6 - No idea, looks like it always has a single int value of 0? A vis test?
             // 0x7 - Locator Index
             // 0x8 - Either a giant visibility test, or the size of the buffer?
             // 0xd/0xf is for skinning?
                // 0x4 - int SegmentIndex
                // 0x8 - int NumBones - Gets Replaced by Segment->SkinData->Count
                // 0xc - int BindDataOffset
                // 0x10 - int JointDataOffset
                // 0x14 - int - Gets Replaced by Segment-SkinData->Indices
                // 0x18 - int - ???
                // 0x1c - Gets set to the number of vertices in the first sector
                // ... blah blah blah
             // 0x12 is for setting the material for a segment?
             // 0xc is the render command?
                 // 0x4 - int VisibilityIndex
                 // 0x8 - int ??
                 // 0xc - int SegmentIndex
                 // 0x10 - int ??
                 
             // Visibility test command
             if (type == 0x3)
                 currentFlags = context.ReadInt32(commandOffset + 0x4);
             
             // type == 0x7, single integer, what is it, this might actually be the locator index
             if (type == 0x7)
                 currentLocatorIndex = context.ReadInt32(commandOffset + 0x4);
             
             if (type == 0xd)
             {
                 int segmentIndex = context.ReadInt32(commandOffset + 0x4);
                 int numBones = context.ReadInt32(commandOffset + 0x8);
                 int bindData = commandData + context.ReadInt32(commandOffset + 0xc);
                 int jointData = commandData + context.ReadInt32(commandOffset + 0x10);
                 
                 var joints = new List<short>(numBones);
                 var matrices = new List<Matrix4x4>(numBones);
                 
                 for (int j = 0; j < numBones; ++j)
                 {
                     joints.Add(context.ReadInt16(jointData + (j * 2)));
                     matrices.Add(context.ReadMatrix(bindData + (j * 64)));
                 }

                 WiiSegmentInfo info = SegmentInfos[segmentIndex];
                 var segment = (SlModelSegmentWiiU)Resource.Segments[segmentIndex];
                 info.IsSkinned = true;
                 info.Joints = segment.Indices.Select(index => joints[index]).ToList();
                 info.InvBindMatrices = segment.Indices.Select(index => matrices[index]).ToList();
             }
             
             // Render command, flush the visibility index
             if (type == 0xc)
             {
                 int segmentIndex = context.ReadInt32(commandOffset + 0xc);
                 WiiSegmentInfo info = SegmentInfos[segmentIndex];

                 if (Resource.Segments[segmentIndex].MaterialIndex == -1)
                     Resource.Segments[segmentIndex].MaterialIndex = currentMaterial;
                 
                 info.Flags = currentFlags;
                 info.LocatorIndex = currentLocatorIndex;
                 info.VisibilityIndex = context.ReadInt32(commandOffset + 0x4);
             }

             if (type == 0x12)
             {
                 int count = context.ReadInt32(commandOffset + 0x4);
                 int materialIndex = context.ReadInt32(commandOffset + 0x8);
                 int offset = commandOffset + 0x18;
                 for (int j = 0; j < count; ++j)
                 {
                     int segmentIndex = context.ReadInt32(offset + 0x8);
                     Resource.Segments[segmentIndex].MaterialIndex = materialIndex;
                     offset += 0xc;
                 }
             }

             if (type == 0x0)
             {
                 int end = context.ReadInt32(commandOffset + 0x4);
                 do
                 {
                     if (end <= displayListWordOffset) break;
                     int address = displayListData + (displayListWordOffset * 4);
                     int header = context.ReadInt32(address);
                     int command = header & 0xffff;
                     int words = (header >> 0x10) + 1;
                     if (command == 0)
                         currentMaterial = context.ReadInt32(address + 4);
                     displayListWordOffset += words;

                 } while (true);
             }
             
             commandOffset += (4 + numWords * 4);
             i += numWords;
         }
        
        CommandBuffer = context.LoadArray(commandData, commandElementCount, context.ReadInt32);
        WorkArea = context.LoadBufferPointer(context.ReadInt32(), out _);
    }

    public class WiiSegmentInfo
    {
        public List<short> Joints = [];
        public List<Matrix4x4> InvBindMatrices = [];
        public bool IsSkinned;
        public int Flags;
        public int LocatorIndex = -1;
        public int VisibilityIndex = -1;
    }
}