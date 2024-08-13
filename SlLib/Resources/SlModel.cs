using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text.Json;
using SlLib.Extensions;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Model.Platform;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Serialization;
using SlLib.Utilities;

namespace SlLib.Resources;

public class SlModel : ISumoResource, IPlatformConvertable
{
    /// <inheritdoc />
    public SlResourceHeader Header { get; set; } = new();

    /// <summary>
    ///     Mesh data
    /// </summary>
    public SlModelResource Resource = new();

    /// <summary>
    ///     Cull sphere used for visibility testing.
    /// </summary>
    public SlCullSphere CullSphere = new();

    /// <summary>
    ///     List of materials that this model uses.
    /// </summary>
    public List<SlResPtr<SlMaterial2>> Materials = [];

    /// <summary>
    ///     The work buffer used by render commands for storing results of operations.
    /// </summary>
    public ArraySegment<byte> WorkArea;

    /// <summary>
    ///     Removes any unused sections and materials from the mesh.
    /// </summary>
    public void RemoveUnusedData()
    {
        List<SlMaterial2> materials = [];
        List<SlModelSegment> segments = [];
        
        foreach (IRenderCommand command in Resource.RenderCommands)
        {
            if (command is not RenderSegmentCommand render) continue;
            
            SlMaterial2? material = Materials[render.MaterialIndex];
            if (material == null) continue;

            int materialIndex = materials.IndexOf(material);
            if (materialIndex == -1)
            {
                materialIndex = materials.Count;
                materials.Add(material);
            }

            SlModelSegment segment = Resource.Segments[render.SegmentIndex];

            int segmentIndex = segments.IndexOf(segment);
            if (segmentIndex == -1)
            {
                segmentIndex = segments.Count;
                segments.Add(segment);
            }
            
            render.MaterialIndex = (short)materialIndex;
            render.SegmentIndex = (short)segmentIndex;
        }

        Materials = materials.Select(material => new SlResPtr<SlMaterial2>(material)).ToList();
        Resource.Segments = segments;
    }

    /// <inheritdoc />
    public void Load(ResourceLoadContext context)
    {
        Header = context.LoadObject<SlResourceHeader>();
        
        // version <= 0x6 or so had SlModelResource as a separate chunk,
        // although again, I don't feel like dealing with that, so we'll ignore it for now,
        // only one file seems to contain data from that version anyway.
        Resource = context.LoadPointer<SlModelResource>() ??
                   throw new SerializationException("SlModel is missing SlModelResource!");

        bool isLegacyModel = context.Version <= 0x13;
        
        // Cull sphere is a pointer below this data in later versions
        if (!isLegacyModel)
        {
            // Cull sphere has Vector4 elements stored in it, so depending on the platform,
            // we need to make sure we're aligned to a vector boundary.
            context.Align(0x10);
            CullSphere = context.LoadObject<SlCullSphere>();   
        }

        // Around Android's version, they moved the counts below the pointers.
        if (context.Version >= SlPlatform.Android.DefaultVersion)
        {
            int materialData = context.ReadPointer();

            // These are just the skeleton joints and attributes pointers, they get
            // set at runtime.
            context.Position += context.Platform.GetPointerSize() * 0x2;

            int workAreaData = context.ReadPointer(out bool isWorkAreaFromGpu);
            int numMaterials = context.ReadInt32();
            context.ReadInt32(); // Not sure what this is, it's always 1
            int workAreaSize = context.ReadInt32();
            
            Materials = context.LoadArray(materialData, numMaterials, context.LoadResourcePointer<SlMaterial2>);
            WorkArea = context.LoadBuffer(workAreaData, workAreaSize, isWorkAreaFromGpu);
        }
        else
        {
            Materials = context.LoadArrayPointer(context.ReadInt32(), context.LoadResourcePointer<SlMaterial2>);
            
            // In earlier versions, it seems they actually serialize the matrix/float values here,
            // in later versions it just stores the pointer from the skeleton here
            // It always gets set at runtime in current versions, so no point in loading it.
            context.Position += context.Platform.GetPointerSize() * 0x2;

            if (!isLegacyModel) context.ReadInt32(); // Always 1?
            
            // There's technically always 2 cull spheres in this pointer, I guess? Only really care for the first one for now,
            // I don't plan on serializing back to this revision. It looks like they end up being the same anyway?
            // This might just be duplicating the cull spheres array from later really
            else CullSphere = context.LoadPointer<SlCullSphere>() ?? throw new SerializationException();

            if (context.Version > 0x1b)
                WorkArea = context.LoadBufferPointer(context.ReadInt32(), out _);
        }

        // Try to pull the skeleton from the definition node if we're on a version
        // that doesn't serialize the skeleton reference.
        if (isLegacyModel)
        {
            SeGraphNode? node = context.LoadNode(Header.Id);
            var animator = node?.FindAncestorThatDerivesFrom<SeDefinitionAnimatorNode>();
            if (animator != null)
            {
                Resource.Skeleton = animator.Skeleton;
                
                // Also try to fixup the entity index
                SlSkeleton? skeleton = animator.Skeleton;
                if (skeleton != null)
                {
                    string name = Path.GetFileNameWithoutExtension(node.ShortName);
                    Resource.EntityIndex = skeleton.Joints.FindIndex(j =>
                        j.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
                }
                
            }
        }
    }

    /// <inheritdoc />
    public void Save(ResourceSaveContext context, ISaveBuffer buffer)
    {
        // As per usual, most of this is serialized in a weird order to make sure
        // that the original file can be re-created 1:1.

        context.SaveObject(buffer, CullSphere, 0x10);

        // Should I add helper methods for dealing with arrays?
        context.WriteInt32(buffer, Materials.Count, 0x40);
        ISaveBuffer materialData = context.SaveGenericPointer(buffer, 0x44, Materials.Count * 4);
        for (int i = 0; i < Materials.Count; ++i)
            context.SaveResource(materialData, Materials[i], i * 4);

        // Not sure what this actually is, always seems to be 1?
        context.WriteInt32(buffer, 1, 0x50);

        context.WriteInt32(buffer, WorkArea.Count, 0x54);
        context.SaveBufferPointer(buffer, WorkArea, 0x58, 0x10);

        // The platform resource generally seems to be empty across all versions,
        // so no reason to have a separate struct for it.
        context.SavePointer(buffer, this, 0x5c);

        // Saving the header last because the tag string is always
        // after all the data for whatever reason, do they serialize backwards or something?
        context.SaveObject(buffer, Header, 0x0);

        context.SavePointer(buffer, Resource, 0xc);
    }

    public int GetSizeForSerialization(SlPlatform platform, int version)
    {
        return platform.Is64Bit ? 0x90 : 0x60;
    }

    /// <inheritdoc />
    public void Convert(SlPlatform target)
    {
        // Make sure this resource is actually convertible right now
        if (!CanConvert(target)) return;

        if (target == SlPlatform.Win32) DoConversionToWin32Internal();
        else throw new NotSupportedException("Conversion to invalid platform!");

        // Make sure to cache the new platform info
        Resource.Header.Platform = target;
        Header.Platform = target;
    }

    /// <inheritdoc />
    public bool CanConvert(SlPlatform target)
    {
        // If we're already on the target platform, no reason to do any conversions
        if (Header.Platform == target && Resource.Header.Platform == target) return false;

        // Right now only conversions to Win32 are supported.
        return target == SlPlatform.Win32;
    }

    /// <summary>
    ///     Converts this resource to Win32 format.
    /// </summary>
    private void DoConversionToWin32Internal()
    {
        SlPlatform target = SlPlatform.Win32;
        
        if (Header.Platform == SlPlatform.Win64 || Header.Platform == SlPlatform.Android)
        {
            // The only difference is that TSR on Win64 AND SART on Android uses a new command
            // for setting dynamic vertex buffers, but they're similar enough,
            // we can just convert them to the old format.
            var commands = Resource.RenderCommands;
            for (int i = 0; i < commands.Count; ++i)
            {
                // Convert to old format
                if (commands[i] is SetDynamicVertexBuffers2Command command)
                {
                    commands[i] = new SetDynamicVertexBuffersCommand
                    {
                        Buffers = command.Buffers,
                        WorkPass = command.WorkPass
                    };
                }
            }
        }

        // Wii U's main difference is in the model segments
        // and that they use a separate command buffer from the rest of the games.
        if (Header.Platform == SlPlatform.WiiU)
        {
            // Legacy versions of Wii U models just need to have their endianness swapped, can ignore
            // all the other nonsense
            bool isLegacy = Resource.PlatformResource is not SlModelResourcePlatformWiiU;
            if (isLegacy)
            {
                foreach (SlModelSegment segment in Resource.Segments)
                {
                    segment.Format.SwapEndiannessForPlatform(segment.VertexStreams, target);
                    if (segment.IndexStream.IsBigEndian != target.IsBigEndian)
                        segment.IndexStream.SwapEndianness16();

                    if (segment.WeightBuffer.Count != 0)
                    {
                        var span = MemoryMarshal.Cast<byte, int>(segment.WeightBuffer);
                        BinaryPrimitives.ReverseEndianness(span, span);
                    }
                    
                    if (segment.JointBuffer.Count != 0)
                    {
                        var span = MemoryMarshal.Cast<byte, int>(segment.JointBuffer);
                        BinaryPrimitives.ReverseEndianness(span, span);
                    }
                }
                
                return;
            }
            
            
            var segments = Resource.Segments;
            int workAreaSize = 0;
            int commandDataSize = 0;
            
            // Convert the model platform resource
            var wiiPlatform = (SlModelResourcePlatformWiiU)Resource.PlatformResource;
            var winPlatform = new SlModelResourcePlatform
            {
                Resource = wiiPlatform.Resource,
                Declarations = wiiPlatform.Declarations,
                VertexStreams = wiiPlatform.VertexStreams,
                IndexStream = wiiPlatform.IndexStream,
                ExtraIndexStream = wiiPlatform.ExtraIndexStream,
            };
            Resource.PlatformResource = winPlatform;
            
            // Generally speaking, most of the model formats are the same, it's just that
            // the model segments have extra platform information.
            for (int i = 0; i < segments.Count; ++i)
            {
                // Copy common data into a new segment instance
                SlModelSegment segment = segments[i];
                SlModelResourcePlatformWiiU.WiiSegmentInfo info = wiiPlatform.SegmentInfos[i];
                var wiiSegment = (SlModelSegmentWiiU)segment;
                var winSegment = new SlModelSegment
                {
                    FirstIndex = segment.FirstIndex,
                    Format = segment.Format,
                    IndexStream = segment.IndexStream,
                    MaterialIndex = segment.MaterialIndex,
                    PrimitiveType = segment.PrimitiveType,
                    Sectors = segment.Sectors,
                    VertexStart = segment.VertexStart,
                    VertexStreams = segment.VertexStreams
                };
                segments[i] = winSegment;
                
                // Make sure streams are matching the target endianness
                wiiSegment.Format.SwapEndiannessForPlatform(wiiSegment.VertexStreams, target);
                if (wiiSegment.IndexStream.IsBigEndian != target.IsBigEndian)
                    wiiSegment.IndexStream.SwapEndianness16();
                    
                // Wii U version doesn't store traditional render commands, so we'll
                // have to build the list.
                List<IRenderCommand> commands = [];


                // If the segment is skinned, convert the weight and joint buffers.
                if (info.IsSkinned)
                {
                    int numVerts = wiiSegment.Sector.NumVerts;
            
                    var weightBuffer = wiiSegment.WeightBuffer;
                    var jointBuffer = wiiSegment.JointBuffer;
                        
                    // The Wii U version supports an extra vertex declaration for the formats of
                    // weight/joint buffers, although they're generally always VEC4 of unsigned bytes,
                    // whereas Windows uses a VEC4 of unsigned integers/floats.
                    byte[] winWeightBuffer = new byte[numVerts * 16];
                    byte[] winJointBuffer = new byte[numVerts * 16];
                    
                    for (int j = 0; j < numVerts; ++j)
                    for (int k = 0; k < 4; ++k)
                    {
                        float weight = weightBuffer[(j * 4) + k] / 255.0f;
                        int joint = jointBuffer[(j * 4) + k] & 0xff;
                        if (joint == byte.MaxValue) joint = -1;
            
                        winWeightBuffer.WriteFloat(weight, (j * 16) + (k * 4));
                        winJointBuffer.WriteInt32(joint, (j * 16) + (k * 4));
                    }
                        
                    winSegment.WeightBuffer = winWeightBuffer;
                    winSegment.JointBuffer = winJointBuffer;
            
                    // Push relevant skinning commands
                    commands.Add(new SetDynamicVertexBuffersCommand
                    {
                        Buffers = [((short)numVerts, 16), ((short)numVerts, 32)],
                        WorkPass = workAreaSize
                    });
                    workAreaSize += 24;
                    
                    int workPass = workAreaSize;
                    workAreaSize = SlUtil.Align(workAreaSize + 4, 0x40);
                    int workResult = workAreaSize;
                    workAreaSize += (wiiSegment.Indices.Count * 0x40);
                    
                    commands.Add(new CalcBindMatricesCommand
                    {
                        NumBones = wiiSegment.Indices.Count,
                        Joints = info.Joints,
                        InvBindMatrices = info.InvBindMatrices,
                        WorkPass = workPass,
                        WorkResult = workResult
                    });
                }
                
                // Add final render command
                commands.Add(new RenderSegmentCommand
                {
                    MaterialIndex = (short)winSegment.MaterialIndex,
                    PivotJoint = (short)info.LocatorIndex,
                    SegmentIndex = (short)i,
                    WorkPass = workAreaSize
                });
                
                // Depending on whether or not the mesh is skinned, we use different visibility testing
                commandDataSize += commands.Sum(command => command.Size);
                if (!Resource.Skeleton.IsEmpty)
                {
                    var noSphereCommand = new TestVisibilityNoSphereCommand
                    {
                        LocatorIndex = (short)info.LocatorIndex,
                        VisibilityIndex = (short)info.VisibilityIndex,
                        Flags = info.Flags,
                        CalculateCullMatrix = false
                    };
                    
                    commandDataSize += noSphereCommand.Size;
                    noSphereCommand.BranchOffset = commandDataSize;
                    Resource.RenderCommands.Add(noSphereCommand);
                }
                else
                {
                    var command = new TestVisibilityCommand
                    {
                        LocatorIndex = (short)info.LocatorIndex,
                        CalculateCullMatrix = false,
                        CullSphereIndex = -1,
                        Flags = info.Flags,
                        VisibilityIndex = info.VisibilityIndex
                    };
                
                    commandDataSize += command.Size;
                    command.BranchOffset = commandDataSize;
                    Resource.RenderCommands.Add(command);
                }
                    
                Resource.RenderCommands.AddRange(commands);
            }
            
            // Have to update the work area for any added commands
            workAreaSize = SlUtil.Align(workAreaSize, 0x10);
            WorkArea = new byte[workAreaSize];
        }
    }
}