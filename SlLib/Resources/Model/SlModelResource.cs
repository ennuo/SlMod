using System.Numerics;
using System.Runtime.Serialization;
using SlLib.Resources.Database;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Model.Platform;
using SlLib.Serialization;

namespace SlLib.Resources.Model;

/// <summary>
///     Resources containing the data for a model.
/// </summary>
public class SlModelResource : ISumoResource
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     The skeleton used by this model.
    /// </summary>
    public SlResPtr<SlSkeleton> Skeleton = new();

    /// <summary>
    ///     Render flags.
    /// </summary>
    public int Flags = 0x11;
    
    /// <summary>
    ///     The segments of this model/
    /// </summary>
    public List<SlModelSegment> Segments = [];

    /// <summary>
    ///     Instructions used to render this model.
    /// </summary>
    public List<IRenderCommand> RenderCommands = [];

    /// <summary>
    ///     Cull spheres used for visibility testing.
    /// </summary>
    public List<SlCullSphere> CullSpheres = [];

    /// <summary>
    ///     The index of the first cull sphere parameter in the skeleton.
    /// </summary>
    public int CullSphereAttributeIndex = -1;

    /// <summary>
    ///     The index of the entity this model belongs to in the skeleton.
    /// </summary>
    public int EntityIndex = -1;

    /// <summary>
    ///     Platform specific model data.
    /// </summary>
    public SlModelResourcePlatform PlatformResource = new();
    
    public SlModelResource()
    {
        PlatformResource.Resource = this;
    }
    
    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        bool isLegacyModel = context.Version <= 0x13;
        
        Header = context.LoadObject<SlResourceHeader>();
        
        // Oh my god, it looks like they don't even reference the skeleton in earlier versions, they just completely
        // serialize all relevant skeleton data to the model itself, that's annoying.
        // Should I just generate a fake skeleton? I'm not adding extra cases for this data.
        if (!isLegacyModel)
            Skeleton = context.LoadResourcePointer<SlSkeleton>();

        int segmentData, renderCommandData, cullSphereData;
        int numSegments, numCullSpheres;
        
        // These are just going to be used for fixing up legacy model data
        // by converting them to the latest format.
        List<short> jointRemapData = [];
        List<short> attributeRemapData = [];
        List<Matrix4x4> inverseWorldMatrices = [];
        
        // From Android version onwards, all pointers were moved up before counts
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            // Skip joint/attribute array references
            context.Position += (context.Platform.GetPointerSize() * 0x2);
            
            segmentData = context.ReadPointer();
            renderCommandData = context.ReadPointer();
            cullSphereData = context.ReadPointer();
            
            Flags = context.ReadInt32();
            
            // Skip attribute/joint counts
            context.Position += (0x4 * 0x2);
            
            numSegments = context.ReadInt32();
            numCullSpheres = context.ReadInt32();
        }
        else
        {
            Flags = context.ReadInt32();
            if (isLegacyModel)
            {
                int numJoints = context.ReadInt32();
                context.ReadPointer(); // world matrix data?
                inverseWorldMatrices = context.LoadArrayPointer(numJoints, context.ReadMatrix);
                jointRemapData = context.LoadArrayPointer(numJoints, context.ReadInt16);
                int numAttributes = context.ReadInt32();
                context.ReadPointer(); // attribute data
                attributeRemapData = context.LoadArrayPointer(numAttributes, context.ReadInt16);
            }
            else
            {
                // Skip joint/attribute array references since they're gathered from the skeleton
                context.Position += (0x4 * 0x2) + (context.Platform.GetPointerSize() * 0x2);   
            }
            
            numSegments = context.ReadInt32();
            segmentData = context.ReadPointer();
            renderCommandData = context.ReadPointer();
            numCullSpheres = context.ReadInt32();
            cullSphereData = context.ReadPointer();
        }
        
        if (!isLegacyModel)
        {
            CullSphereAttributeIndex = context.ReadInt32();
            context.ReadInt32(); // Some index? Seems to always be -1
            EntityIndex = context.ReadInt32();
        }
        
        // Earlier Wii U data is basically the same as every other version,
        // since they didn't add the Wii specific platform data yet,
        // so just load the normal segments instead.
        if (context.Platform == SlPlatform.WiiU && !isLegacyModel)
        {
            Segments = context.LoadArray<SlModelSegment>(segmentData, numSegments,
                context.LoadReference<SlModelSegmentWiiU>);
        }
        else Segments = context.LoadArray<SlModelSegment>(segmentData, numSegments);

        if (isLegacyModel)
        {
            // calc bind matrices command wasn't used yet, not fixing it right now
            foreach (SlModelSegment segment in Segments)
            {
                segment.WeightBuffer = ArraySegment<byte>.Empty;
                segment.JointBuffer = ArraySegment<byte>.Empty;
            }
        }
        
        if (renderCommandData != 0)
        {
            // Have to remap older branch command offsets
            List<int> rawCommandDataOffsets = [];
            List<int> adjustedCommandDataOffsets = [];
            int adjustedCommandOffset = 0;
            
            int commandOffset = renderCommandData;
            while (context.ReadInt16(commandOffset) != 2)
            {
                // if no locator, matrix is set to matrix @ 0x0
                // if locator, matrix is set to matrix @ 0x40 * locator matrix
                
                // matrix @ 0x40 = matrix @ 0x0 
                
                // SlModelInstanceData
                    // 0x0 = Matrix4x4 = World Position
                    // 0x40 = Matrix4x4 = Inv Entity Bind Pose * World Position
                    // 0x80 = int = RenderMask
                    // 0x84 = byte = ???[4] lod groups? bytes contain active lod index (-1 for no lod)
                    // 0x88 = short = ??? (culled?)
                    // 0x8a = short = Visibility(?) (> 1 = visible?)
                    // 0x90 = Matrix4x4 = Vertex Program World Matrix
                    // 0xd0 = Matrix4x4 = Cull Matrix
                    // 0x110 = SlModelSHCachePacket**
                
                    
                // SlModelContext
                    // + 0x54 = SlConstantBuffer* = CBuffer_WorldMatrix
                    // + 0x58 = SlConstantBuffer* = CBuffer_ViewProjection
                    // + 0x5c = SlConstantBuffer* = CBuffer_SHData
                    // + 0xf0 = Matrix4x4 = World Matrix
                    // + 0x170 = Matrix4x4 = View Projection Matrix?
                    // + 0x1f0 = Matrix4x4 = CalcCullMatrix(ViewProjectionMatrix)
                    // + 0x220 = Matrix4x4 = View Matrix
                    // + 0x260 = Matrix4x4 = Projection Matrix
                    
                
                // SlRenderContext
                    // (+ 0x9c8) + 0xf0 = world matrix
                    // (+ 0x9c8) + 0x170 = view projection matrix?
                    // (+ 0x9c8) + 0x220 = view matrix
                    // (+ 0x9c8) + 0x260 = projection matrix
                    // (+ 0x220)
                    // + (0x9c8) + 0x1f0
                    
                    
                // SlRenderCommandWork
                    // 0x0 = Matrix4 = ???
                    // 0x40 = Matri4x4 = ???
                    // 0x80 = Matrix4x4 = ???
                    // 0xc4 = int = NumBackupInstances
                    // 0xc8 = SlModelInstanceData* = BackupInstances
                    // 0xd0 = short* = CurrentCommand
                    // 0xd4 = short* = NextCommand
                    // 0xd8 = SlModelInstanceData* = InstanceWorkData (each is 0x120 bytes, allocated by command)
                    // 0xe4 = SlModelRenderContext
                        // 0xe4 - 00x0 = int = NumInstances
                        // 0xe8 - 0x4 = SlModelInstanceData* = Instances
                        // 0xf4 - 0x10 = SlMaterial2* = NextMaterial
                        // 0xf8 - 0x14 = SlMaterial2* = CurrentMaterial
                        // 0xfc - 0x18 = Matrix4x4* = BindMatrices
                        // 0x100 - 0x1c = int = NumBindMatrices
                        // 0x104 - 0x20 = void* = BindMatrixWorkTagPointer? (used to make sure bind matrix is only calculated once per frame?)
                // need these commands
                    // 6
                    // 7 - AllocWorkInstances
                        // int numInstances (allocates numInstances * 0x120) of data, assigns it to 0xd8 of SlRenderCommandWork
                    // 12
                    // 14
                    // 15
                    // 16
                    
                rawCommandDataOffsets.Add(commandOffset - renderCommandData);
                adjustedCommandDataOffsets.Add(adjustedCommandOffset);
                
                int type = context.ReadInt16(commandOffset);
                int size = context.ReadInt16(commandOffset + 2);
                IRenderCommand? command = type switch
                {
                    0x00 => new TestVisibilityCommand(),
                    0x01 => new RenderSegmentCommand(),
                    0x05 => new SelectLodCommand(),
                    0x06 => new SetupInstancesCommand(),
                    0x07 => new AllocWorkInstancesCommand(),
                    0x09 => new TestVisibilityPvsCommand(),
                    0x0a => new TestVisibilityNoSphereCommand(),
                    0x0b => new CalcBindMatricesCommand(),
                    0x0c => new TestVisibilityNoSpherePvsCommand(),
                    0x0d => new SetDynamicVertexBuffersCommand(),
                    0x0e => new TestVisibilityPvsSectorCommand(),
                    0x0f => new TestVisibilityNoSpherePvsSectorCommand(),
                    0x10 => new SetupInstancesPvsCommand(),
                    0x11 => new SetDynamicVertexBuffers2Command(),
                    _ => null
                };
                
                if (command == null)
                {
                    Console.WriteLine($"Unsupported command {commandOffset} : {type} (size = {size}) in {Header.Name}");
                    commandOffset += size;
                    continue;
                }
                
                command.Load(context, renderCommandData, commandOffset);
                if (command.Size != size)
                {
                    Console.WriteLine($"size doesn't match for {command.GetType().Name} ({command.Type} @ {commandOffset:x8}, base = {renderCommandData:x8} expected 0x{command.Size:x8}, got 0x{size:x8}");
                }
                
                // fixup joint references for legacy models
                if (isLegacyModel)
                {
                    switch (command)
                    {
                        case RenderSegmentCommand rsc:
                        {
                            if (rsc.PivotJoint != -1) 
                                rsc.PivotJoint = jointRemapData[rsc.PivotJoint];
                            break;
                        }
                        
                        case TestVisibilityNoSphereCommand vnsc:
                        {
                            if (vnsc.LocatorIndex != -1) 
                                vnsc.LocatorIndex = jointRemapData[vnsc.LocatorIndex];
                            if (vnsc.VisibilityIndex != -1)
                                vnsc.VisibilityIndex = attributeRemapData[vnsc.VisibilityIndex];

                            break;
                        }

                        case TestVisibilityCommand vc:
                        {
                            if (vc.LocatorIndex != -1)
                                vc.LocatorIndex = jointRemapData[vc.LocatorIndex];
                            if (vc.VisibilityIndex != -1)
                                vc.VisibilityIndex = attributeRemapData[vc.VisibilityIndex];
                            break;
                        }
                    }
                }
                
                // Wii U doesn't store material indices in segments, so pull it from any commands
                if (context.Platform == SlPlatform.WiiU && command is RenderSegmentCommand render)
                    Segments[render.SegmentIndex].MaterialIndex = render.MaterialIndex;
                
                RenderCommands.Add(command);
                commandOffset += size;
                adjustedCommandOffset += command.Size;
            }
            
            // special case for end of command data
            adjustedCommandDataOffsets.Add(adjustedCommandOffset);
            rawCommandDataOffsets.Add(commandOffset - renderCommandData);
            
            // have to fixup command offsets
            if (context.Version <= 0x1b)
            {
                for (int i = 0; i < RenderCommands.Count; ++i)
                {
                    if (RenderCommands[i] is IBranchRenderCommand command)
                    {
                        int index = rawCommandDataOffsets.IndexOf(command.BranchOffset);
                        if (index == -1)
                            throw new SerializationException($"type: {command.GetType().Name} Could not find branch command offset for {command.BranchOffset}!");
                        command.BranchOffset = adjustedCommandDataOffsets[index];
                    }
                }
                
                // TODO: Fixup workarea
            }
        }
        
        CullSpheres = context.LoadArray<SlCullSphere>(cullSphereData, numCullSpheres);
        
        PlatformResource = context.Platform == SlPlatform.WiiU && !isLegacyModel
            ? context.LoadObject<SlModelResourcePlatformWiiU>()
            : context.LoadObject<SlModelResourcePlatform>();
    }
    
     /// <inheritdoc />
     public void Save(ResourceSaveContext context, ISaveBuffer buffer)
     {
         SlSkeleton? skeleton = Skeleton.Instance;
         context.SaveResource(buffer, Skeleton, 0xc);
         context.WriteInt32(buffer, Flags, 0x10);
         if (skeleton != null)
         {
             context.WriteInt32(buffer, skeleton.Joints.Count, 0x14);
             context.WriteInt32(buffer, skeleton.Attributes.Count, 0x1c);
         }

         // Doing all the serialization here in a weird way to make sure the data stays in the same order,
         // For example...
         // Weight/Joint streams of buffers get serialized before any of the vertex/index buffers get saved
         // Dynamic vertex command buffers get serialized before any of the other render commannd data
         // Header is saved after most data, but before vertex streams

         context.WriteInt32(buffer, Segments.Count, 0x24);
         ISaveBuffer segmentData = context.SaveGenericPointer(buffer, 0x28, 0x38 * Segments.Count, 0x10);
         for (int i = 0; i < Segments.Count; ++i)
         {
             ISaveBuffer segmentBuffer = segmentData.At(i * 0x38, 0x38);
             SlModelSegment segment = Segments[i];

             context.WriteInt32(segmentBuffer, (int)segment.PrimitiveType, 0x0);
             context.WriteInt32(segmentBuffer, segment.MaterialIndex, 0x4);
             context.WriteInt32(segmentBuffer, segment.VertexStart, 0x8);
             context.WriteInt32(segmentBuffer, segment.FirstIndex, 0xc);
             context.WriteInt32(segmentBuffer, segment.Sectors.Count, 0x10);

             ISaveBuffer sectorData = context.SaveGenericPointer(segmentBuffer, 0x14, 0x2c * segment.Sectors.Count);
             for (int j = 0; j < segment.Sectors.Count; ++j)
                 context.SaveObject(sectorData, segment.Sectors[j], j * 0x2c);

             // Skipping over stream and index buffers for now

             context.SaveBufferPointer(segmentBuffer, segment.WeightBuffer, 0x2c);
             context.SaveBufferPointer(segmentBuffer, segment.JointBuffer, 0x30, 0x10);
         }

         int commandBufferSize = 0x2;
         int dynBufCount = 0x0;
         foreach (IRenderCommand command in RenderCommands)
         {
             commandBufferSize += command.Size;
             if (command is SetDynamicVertexBuffersCommand dynVertexBufferCmd)
                 dynBufCount += dynVertexBufferCmd.Buffers.Count;
         }

         ISaveBuffer commandData = context.SaveGenericPointer(buffer, 0x2c, commandBufferSize, 0x10);
         ISaveBuffer? dynData = null;

         // Pre-allocate the dynamic vertex buffer command to preserve original order of data
         if (dynBufCount != 0)
             dynData = context.Allocate(dynBufCount * 0x4, 0x10);

         int commandOffset = 0;
         int dynOffset = 0;
         foreach (IRenderCommand command in RenderCommands)
         {
             ISaveBuffer crumb = commandData.At(commandOffset, command.Size);
             ISaveBuffer? dynCrumb = null;

             // Kind of gross, but whatever
             if (command is SetDynamicVertexBuffersCommand dynVertexBufferCmd)
             {
                 int size = dynVertexBufferCmd.Buffers.Count * 4;
                 dynCrumb = dynData!.At(dynOffset, size);
                 dynOffset += size;
             }

             context.WriteInt16(crumb, (short)command.Type, 0x0);
             context.WriteInt16(crumb, (short)command.Size, 0x2);

             command.Save(context, commandData, crumb, dynCrumb);

             commandOffset += command.Size;
         }

         context.WriteInt16(commandData, 2, commandOffset);

         // Cull sphere related data
         context.WriteInt32(buffer, CullSpheres.Count, 0x30);
         ISaveBuffer cullBuffer = context.SaveGenericPointer(buffer, 0x34, CullSpheres.Count * 0x30, 0x10);
         for (int i = 0; i < CullSpheres.Count; ++i)
             context.SaveObject(cullBuffer, CullSpheres[i], i * 0x30);
         context.WriteInt32(buffer, CullSphereAttributeIndex, 0x38);

         // Not actually sure what this index actually is,
         // it's always -1 it seems? Will have to check all models to see if
         // it's ever not at some point.
         context.WriteInt32(buffer, -1, 0x3c);
         context.WriteInt32(buffer, EntityIndex, 0x40);

         // A bunch of structures reference themselves for whatever reason.
         context.SavePointer(buffer, this, 0x44);
         context.SaveObject(buffer, Header, 0);

         var vertexStreams = PlatformResource.VertexStreams;
         var vertexDeclarations = PlatformResource.Declarations;
         var indexStream = PlatformResource.IndexStream;

         // Now to finally do all the vertex streams and declarations before fixing up the
         // model segments.
         context.WriteInt32(buffer, vertexStreams.Count, 0x48);
         ISaveBuffer vertexData = context.SaveGenericPointer(buffer, 0x4c, vertexStreams.Count * 0x2c);
         for (int i = 0; i < vertexStreams.Count; ++i)
             context.SaveReference(vertexData, vertexStreams[i], i * 0x2c);

         context.WriteInt32(buffer, vertexDeclarations.Count, 0x50);
         ISaveBuffer declData = context.SaveGenericPointer(buffer, 0x54, vertexDeclarations.Count * 0x1c);
         for (int i = 0; i < vertexDeclarations.Count; ++i)
             context.SaveReference(declData, vertexDeclarations[i], i * 0x1c);

         context.SavePointer(buffer, PlatformResource.IndexStream, 0x58);

         // Fixup stream references in model segments now that we've serialized everything else
         for (int i = 0; i < Segments.Count; ++i)
         {
             ISaveBuffer segmentBuffer = segmentData.At(i * 0x38, 0x38);
             SlModelSegment segment = Segments[i];

             if (!vertexDeclarations.Contains(segment.Format))
                 throw new SerializationException("Model resource contains unregistered vertex declaration!");

             context.SavePointer(segmentBuffer, segment.Format, 0x18);
             for (int j = 0; j < 3; ++j)
             {
                 SlStream? stream = segment.VertexStreams[j];
                 if (stream == null) continue;

                 if (!vertexStreams.Contains(stream))
                     throw new SerializationException("Model resource contains unregistered vertex stream!");

                 context.SavePointer(segmentBuffer, stream, 0x1c + j * 4);
             }

             if (segment.IndexStream != indexStream)
                 throw new SerializationException("Model resource contains unregistered index stream!");

             context.SavePointer(segmentBuffer, segment.IndexStream, 0x28);
         }
     }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        // need to add platform resource sizes
        return (platform.Is64Bit ? 0x50 : 0x44) + PlatformResource.GetSizeForSerialization(platform, version);
    }
}