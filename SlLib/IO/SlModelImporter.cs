using System.Numerics;
using System.Text.Json;
using DirectXTexNet;
using SharpGLTF.Schema2;
using SharpGLTF.Validation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SlLib.Extensions;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Utilities;
using Image = SharpGLTF.Schema2.Image;

namespace SlLib.IO;

public class SlModelImporter(SlImportConfig config)
{
    private static readonly SlResourceDatabase ShaderCache =
        SlResourceDatabase.Load($"F:/sart/cache/shadercache.cpu.spc", $"F:/sart/cache/shadercache.gpu.spc");
    
    private SlModel _model = new();
    private string _fileName = Path.GetFullPath(config.GlbSourcePath).Replace("\\", "/");
    private ModelRoot _gltf = ModelRoot.Load(config.GlbSourcePath, new ReadSettings { Validation = ValidationMode.Skip });
    private SlImportConfig _config = config;
    private Dictionary<Image, SlTexture> _textureCache = [];
    
    private SlTexture RegisterTexture(Texture texture, bool isNormalTexture = false)
    {
        Image imageNode = texture.PrimaryImage;
        if (_textureCache.TryGetValue(imageNode, out SlTexture? slTexture)) return slTexture;
        
        var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imageNode.Content.Open());
        string name = $"{_fileName}:{imageNode.Name}.texture";
        slTexture = new SlTexture(name, image, isNormalTexture);
        _config.Database.AddResource(slTexture);
        
        _textureCache[imageNode] = slTexture;
        return slTexture;
    }
    
    private MaterialData RegisterMaterial(Material material)
    {
        // Figure out what material to use as a base from the setup.
        
        var baseColorChannel = material.FindChannel("BaseColor");
        var normalChannel = material.FindChannel("Normal");
        var emissiveChannel = material.FindChannel("Emissive");
        var specularChannel = material.FindChannel("SpecularColor");
        
        bool hasDiffuseTexture = baseColorChannel?.Texture != null;
        bool hasNormalTexture = normalChannel?.Texture != null;
        bool hasEmissiveTexture = emissiveChannel?.Texture != null;
        bool hasEmissiveColor = emissiveChannel?.Color != new Vector4(0.0f, 0.0f, 0.0f, 1.0f);
        bool hasSpecularTexture = specularChannel?.Texture != null;
        
        bool hasAlpha = material.Alpha != AlphaMode.OPAQUE;
        bool hasTexture = hasDiffuseTexture || hasNormalTexture || hasEmissiveTexture || hasSpecularTexture;
        
        // No matter what material we use, we're going to need vertex positions and normals
        StreamFlags flags = StreamFlags.Position | StreamFlags.Normal;
        List<string> attributes = ["vp", "vn"];
        if (hasTexture)
        {
            flags |= StreamFlags.TextureCoordinates;
            attributes.Add("vt");
        }
        
        // Normal textures are in tangent space, so we need them
        if (hasNormalTexture)
        {
            attributes.Add("vtg");
            flags |= StreamFlags.Tangents;
        }
        
        attributes.Add("tnv");
        if (hasNormalTexture) attributes.Add("ttv");
        
        attributes.Add(hasDiffuseTexture ? "ct" : "cm");
        if (hasNormalTexture) attributes.Add("nt");
        attributes.Add(hasSpecularTexture ? "st" : "sm");
        
        if (hasEmissiveTexture) attributes.Add("et");
        else if (hasEmissiveColor) attributes.Add("em");
        
        // p = ??? uses the same shader as b?
        // l = lambert
        // b = blinn
        // f = fog?
        // s = specular OR shadow(?)
        
        string header = string.Join('_', attributes) + "_cma_s_u_p_f_co_so_go_god_sod";

        var slMaterial = ShaderCache.FindResourceByPartialName<SlMaterial2>(header, instance: true);
        if (slMaterial == null)
            throw new ArgumentException("Could not find valid shader template for given material!");
        
        if (baseColorChannel != null)
        {
            if (hasDiffuseTexture)
            {
                SlTexture diffuse = RegisterTexture(baseColorChannel.Value.Texture);
                slMaterial.SetTexture("gDiffuseTexture", diffuse);
                slMaterial.SetTexture("gAlbedoTexture", diffuse);                
            }
            
            slMaterial.SetConstant("gDiffuseColour", baseColorChannel.Value.Color);
        }
        
        if (normalChannel != null && hasNormalTexture)
            slMaterial.SetTexture("gNormalTexture", RegisterTexture(normalChannel.Value.Texture, isNormalTexture: true));

        if (specularChannel != null)
        {
            if (hasSpecularTexture)
                slMaterial.SetTexture("gSpecularTexture", RegisterTexture(specularChannel.Value.Texture));
        }

        if (emissiveChannel != null)
        {
            if (hasEmissiveTexture)
                slMaterial.SetTexture("gEmissiveTexture", RegisterTexture(emissiveChannel.Value.Texture));
            if (hasEmissiveColor)
            {
                IMaterialParameter? emFactor = 
                    emissiveChannel.Value.Parameters
                    .SingleOrDefault(p => p.Name == "EmissiveStrength");
                float factor = emFactor != null ? (float)emFactor.Value : 1.0f;
                
                slMaterial.SetConstant("gEmissiveColour", emissiveChannel.Value.Color * factor);   
            }
        }
        
        slMaterial.SetConstant("gSpecularColour", new Vector4(0.01f, 0.0f, 0.0f, 2.0f));
        
        slMaterial.SetConstant("gAlphaRef", hasAlpha ? new Vector4(material.AlphaCutoff) : Vector4.Zero);
        slMaterial.Header.SetName($"{_fileName}:{material.Name}.material");
        
        // Copy everything into the target database
        ShaderCache.CopyResourceByHash<SlShader>(_config.Database, slMaterial.Shader.Id);
        foreach (SlConstantBuffer buffer in slMaterial.ConstantBuffers)
            ShaderCache.CopyResourceByHash<SlConstantBufferDesc>(_config.Database, buffer.ConstantBufferDesc.Id);
        _config.Database.AddResource(slMaterial);

        return new MaterialData(material, slMaterial, flags);
    }

    public SlModel Import()
    {
        SlModelResource resource = _model.Resource;
        
        // The materials define what vertex formats we're going to need for each
        // mesh segment, so register all of them first.
        var materials = new List<MaterialData>(_gltf.LogicalMaterials.Count);
        foreach (Material material in _gltf.LogicalMaterials)
        {
            MaterialData data = RegisterMaterial(material);
            data.VertexBufferFormat = StreamFlags.Position | StreamFlags.Normal | StreamFlags.TextureCoordinates |
                                      StreamFlags.Tangents;
            _model.Materials.Add(new SlResPtr<SlMaterial2>(data.Material));
            materials.Add(data);
        }

        // Do a first pass over the meshes to calculate the
        // vertices needed for each stream, as well as the total index count
        int numIndices = 0;
        Dictionary<StreamFlags, int> verticesPerStream = [];
        foreach (Mesh mesh in _gltf.LogicalMeshes)
        foreach (MeshPrimitive primitive in mesh.Primitives)
        {
            int vertexCount = primitive.VertexAccessors["POSITION"].Count;
            StreamFlags flags = materials.Find(material => material.Model == primitive.Material).VertexBufferFormat;
            if (!verticesPerStream.TryAdd(flags, vertexCount)) verticesPerStream[flags] += vertexCount;
            numIndices += primitive.IndexAccessor.Count;
        }
        
        // Construct all the vertex declarations and their associated empty streams.
        Dictionary<StreamFlags, StreamBuilder> vertexStreamBuilders = [];
        foreach (StreamFlags flags in verticesPerStream.Keys)
        {
            int numVertices = verticesPerStream[flags];
            
            var format = new SlVertexDeclaration();
            if (flags.HasFlag(StreamFlags.Position)) format.AddAttribute(0, 0, SlVertexElementType.Float, 4, SlVertexUsage.Position, 0);
            if (flags.HasFlag(StreamFlags.Normal)) format.AddAttribute(1, 0, SlVertexElementType.Float, 4, SlVertexUsage.Normal, 0);
            if (flags.HasFlag(StreamFlags.Tangents)) format.AddAttribute(1, 0x10, SlVertexElementType.Float, 4, SlVertexUsage.Tangent, 0);
            if (flags.HasFlag(StreamFlags.TextureCoordinates)) format.AddAttribute(2, 0, SlVertexElementType.Half, 2, SlVertexUsage.TextureCoordinate, 0);

            vertexStreamBuilders[flags] = new StreamBuilder(format, numVertices);
        }
        
        var indexStream = new SlStream(numIndices, 2);
        int firstIndex = 0;
        int workAreaSize = 0, commandDataSize = 0;
        
        // Start adding data to all streams and creating mesh segments
        foreach (Mesh mesh in _gltf.LogicalMeshes)
        foreach (MeshPrimitive primitive in mesh.Primitives)
        {
            int segmentIndex = resource.Segments.Count;
            int vertexCount = primitive.VertexAccessors["POSITION"].Count;
            int indexCount = primitive.IndexAccessor.Count;
            int materialIndex = materials.FindIndex(material => material.Model == primitive.Material);
            StreamFlags vertexFlags = materials[materialIndex].VertexBufferFormat;
            StreamBuilder builder = vertexStreamBuilders[vertexFlags];
            
            var sector = new SlModelSector
            {
                NumElements = indexCount,
                NumVerts = vertexCount
            };

            var segment = new SlModelSegment
            {
                PrimitiveType = SlPrimitiveType.Triangles,
                MaterialIndex = materialIndex,
                VertexStart = builder.AddSegment(primitive),
                FirstIndex = firstIndex,
                Sectors = [sector],
                Format = builder.Format,
                VertexStreams = builder.Streams,
                IndexStream = indexStream,
            };
            
            var indices = primitive.GetIndices();
            for (int i = 0; i < indexCount; ++i)
                indexStream.Data.WriteInt16((short)indices[i], (firstIndex * 2) + (i * 2));
            resource.Segments.Add(segment);
            firstIndex += indexCount;


            bool skinned = primitive.VertexAccessors.ContainsKey("WEIGHTS_0");
            if (skinned)
            {
                byte[] weightStream = new byte[0x10 * vertexCount];
                byte[] jointStream = new byte[0x10 * vertexCount];
                var weights = primitive.VertexAccessors["WEIGHTS_0"].AsVector4Array();
                var joints = primitive.VertexAccessors["JOINTS_0"].AsVector4Array();
                for (int i = 0; i < vertexCount; ++i)
                {
                    int offset = (i * 0x10);
                    for (int j = 0; j < 4; ++j)
                        weightStream.WriteFloat(weights[i][j], offset + (j * 4));
                    for (int j = 0; j < 4; ++j)
                    {
                        int joint = (int)joints[i][j];
                        if (weights[i][j] == 0.0) joint = -1;
                        jointStream.WriteInt32(joint, offset + (j * 4));
                    }
                }
                
                segment.WeightBuffer = weightStream;
                segment.JointBuffer = jointStream;
            }

            // Start creating the command buffer
            List<IRenderCommand> commands = [];
            if (skinned)
            {
                commands.Add(new SetDynamicVertexBuffersCommand
                {
                    Buffers = [((short)vertexCount, 16), ((short)vertexCount, 32)],
                    WorkPass = workAreaSize
                });
                workAreaSize += 24;

                List<(string Bone, int Parent)> nodeJointPairs = [];
                
                // Oops, fucked up
                Skin? skin = _gltf.LogicalNodes.First(node => node.Mesh == mesh).Skin;
                for (int i = 0; i < skin.JointsCount; ++i)
                {
                    Node joint = skin.GetJoint(i).Joint;

                    int parentIndex = -1;
                    for (int j = 0; j < skin.JointsCount; ++j)
                    {
                        Node parent = skin.GetJoint(j).Joint;
                        if (parent != joint.VisualParent) continue;
                        
                        parentIndex = j;
                        break;
                    }
                    
                    nodeJointPairs.Add((joint.Name, parentIndex));
                }

                var joints = nodeJointPairs.Select((pair, index) =>
                { 
                    string name = pair.Bone;
                    if (_config.BoneRemapCallback != null)
                        name = _config.BoneRemapCallback(nodeJointPairs, index);

                    int jointIndex = config.Skeleton?.Joints.FindIndex(j => j.Name == name) ?? -1;
                    if (jointIndex == -1)
                        throw new ArgumentException($"Could not find {name} in the skeleton!");

                    return (short)jointIndex;
                }).ToList();

                var matrices = joints.Select(joint => config.Skeleton!.Joints[joint].InverseBindPose).ToList();
                
                int workPass = workAreaSize;
                workAreaSize = SlUtil.Align(workAreaSize + 4, 0x40);
                int workResult = workAreaSize;
                workAreaSize += (skin.JointsCount * 0x40);
                    
                commands.Add(new CalcBindMatricesCommand
                {
                    NumBones = skin.JointsCount,
                    Joints = joints,
                    InvBindMatrices = matrices,
                    WorkPass = workPass,
                    WorkResult = workResult
                });
            }
            
            // Add final render command
            commands.Add(new RenderSegmentCommand
            {
                MaterialIndex = (short)materialIndex,
                PivotJoint = -1,
                SegmentIndex = (short)segmentIndex,
                WorkPass = workAreaSize
            });
            
            // Depending on whether or not the mesh is skinned, we use different visibility testing
            commandDataSize += commands.Sum(command => command.Size);
            if (_config.Skeleton != null)
            {
                var noSphereCommand = new TestVisibilityNoSphereCommand
                {
                    Flags = 0x11,
                    CalculateCullMatrix = false
                };
                    
                commandDataSize += noSphereCommand.Size;
                noSphereCommand.BranchOffset = commandDataSize;
                resource.RenderCommands.Add(noSphereCommand);
            }
            else
            {
                var command = new TestVisibilityCommand
                {
                    CalculateCullMatrix = false,
                    Flags = 0x11,
                };
                
                commandDataSize += command.Size;
                command.BranchOffset = commandDataSize;
                resource.RenderCommands.Add(command);
            }
                    
            resource.RenderCommands.AddRange(commands);
        }
        
        resource.Skeleton = new SlResPtr<SlSkeleton>(_config.Skeleton);
        resource.PlatformResource.IndexStream = indexStream;
        foreach (StreamBuilder builder in vertexStreamBuilders.Values)
        {
            resource.PlatformResource.Declarations.Add(builder.Format);
            foreach (SlStream? stream in builder.Streams)
            {
                if (stream == null) continue;
                //stream.Gpu = false;
                resource.PlatformResource.VertexStreams.Add(stream);
            }
        }
        _model.Header.SetName(Path.ChangeExtension(_fileName, ".model"));
        _model.Resource.Header.SetName(Path.ChangeExtension(_fileName, ".modelResource"));
        _model.WorkArea = new byte[SlUtil.Align(workAreaSize, 0x10)];
        
        return _model;
    }

    [Flags]
    private enum StreamFlags
    {
        Position = 1,
        Normal = 2,
        TextureCoordinates = 4,
        Tangents = 8,
        Afterburner = 16,
        Color = 32
    }

    private struct MaterialData(Material model, SlMaterial2 material, StreamFlags format)
    {
        public StreamFlags VertexBufferFormat = format;
        public readonly Material Model = model;
        public readonly SlMaterial2 Material = material;
    }
    
    private class StreamBuilder(SlVertexDeclaration format, int vertexCount)
    {
        private static Dictionary<string, int> UsageLookup = new()
        {
            { "POSITION", SlVertexUsage.Position },
            { "NORMAL", SlVertexUsage.Normal },
            { "TANGENT", SlVertexUsage.Tangent },
            { "TEXCOORD_0", SlVertexUsage.TextureCoordinate },
        };
        
        public SlVertexDeclaration Format = format;
        public SlStream?[] Streams = format.Create(vertexCount);
        
        private int _vertexPointer;
        private int _vertexCount = vertexCount;

        public int AddSegment(MeshPrimitive primitive)
        {
            int vertexOffset = _vertexPointer;
            int vertexCount = primitive.VertexAccessors["POSITION"].Count;
            foreach (string attribute in UsageLookup.Keys)
            {
                int usage = UsageLookup[attribute];
                if (!Format.HasAttribute(usage)) continue;
                
                // Maybe this should throw an error or a warning instead?
                if (!primitive.VertexAccessors.ContainsKey(attribute)) continue;
                
                Accessor accessor = primitive.VertexAccessors[attribute];
                var vertices = accessor.Dimensions switch
                {
                    DimensionType.VEC2 => accessor.AsVector2Array().Select(v => new Vector4(v, 0.0f, 1.0f)).ToArray(),
                    DimensionType.VEC3 => accessor.AsVector3Array().Select(v => new Vector4(v, 1.0f)).ToArray(),
                    _ => accessor.AsVector4Array().ToArray()
                };
                
                format.Set(Streams, usage, vertices, vertexOffset);
            }
            
            _vertexPointer += vertexCount;
            return vertexOffset;
        }
    }
}