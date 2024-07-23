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
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Scene.Instances;
using SlLib.Resources.Skeleton;
using SlLib.Utilities;
using Image = SharpGLTF.Schema2.Image;

namespace SlLib.IO;

public class SlModelImporter(SlImportConfig config)
{
    private static readonly SlResourceDatabase ShaderCache =
        SlResourceDatabase.Load($"F:/sart/cache/shadercache.cpu.spc", $"F:/sart/cache/shadercache.gpu.spc");
    
    private string _fileName = Path.GetFullPath(config.GlbSourcePath).Replace("\\", "/").Replace("F:/", string.Empty);
    private ModelRoot _gltf = ModelRoot.Load(config.GlbSourcePath, new ReadSettings { Validation = ValidationMode.Skip });
    private SlImportConfig _config = config;

    private Dictionary<Node, SlModel> _modelCache = [];
    private Dictionary<Node, SeGraphNode> _nodeCache = [];
    private Dictionary<Node, SlSkeleton> _nodeSkeletonMap = [];
    private Dictionary<Material, MaterialData> _materialCache = [];
    private Dictionary<Image, SlTexture> _textureCache = [];
    
    private SeInstanceSceneNode _scene = new()
    {
        UidName = "DefaultScene",
        Definition = new SeDefinitionSceneNode()
    };
    
    private List<SeNodeBase> _nodes = [];
    
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
        if (_materialCache.TryGetValue(material, out MaterialData? materialData))
            return materialData;
        
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
        
        if (hasAlpha)
            attributes.Add("cat");
        
        if (!hasAlpha)
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
        
        // s u b f
        // s u p f
        
        attributes.AddRange(["cma", "s", "u", "p", "f"]);
        if (hasDiffuseTexture && hasAlpha)
            attributes.Add("ct");
        
        string header = string.Join('_', attributes) + "_co_so_go_god_sod";

        var slMaterial = ShaderCache.FindResourceByPartialName<SlMaterial2>(header, instance: true);
        if (slMaterial == null)
        {
            // sacrifice emission to find a match, a noble sacrifice...
            header = header.Replace("_em", string.Empty);
            slMaterial = ShaderCache.FindResourceByPartialName<SlMaterial2>(header, instance: true);
            
            if (slMaterial == null)
                throw new ArgumentException("Could not find valid shader template for given material! " + header);
        }
        
        
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

    private void GenerateMeshHierarchy(Node node, List<Node> meshes)
    {
        string name = node.Name.ToLower();
        
        // ignore special stuff like locators, animators, and entities
        if (name.StartsWith("se_")) return;
        
        if (node.Mesh != null)
            meshes.Add(node);
        
        foreach (Node? child in node.VisualChildren)
            GenerateMeshHierarchy(child, meshes);
    }

    private void GenerateSkeletonHierachy(Node node, Node? parent, SlSkeleton skeleton)
    {
        _nodeSkeletonMap[node] = skeleton;
        
        var joint = new SlJoint
        {
            Name = node.Name
        };
        
        if (parent != null)
            joint.Parent = skeleton.Joints.FindIndex(j => j.Name == parent.Name);
        
        joint.Rotation = node.LocalTransform.Rotation;
        joint.Scale = node.LocalTransform.Scale;
        joint.Translation = node.LocalTransform.Translation;

        // replace with skin data when we get to that
        joint.BindPose = node.WorldMatrix;
        Matrix4x4.Invert(joint.BindPose, out joint.InverseBindPose);
        
        skeleton.Joints.Add(joint);
        
        foreach (var child in node.VisualChildren)
            GenerateSkeletonHierachy(child, node, skeleton);
    }
    
    private void HandleSceneNode(Node node, string path, Node? parent, SlSkeleton? skeleton = null)
    {
        string name = node.Name.ToLower();

        bool isEntity = name.StartsWith("se_entity_");
        bool isAnimator = name.StartsWith("se_animator_");
        bool isLocator = name.StartsWith("se_locator_");
        
        path = parent != null ? $"{path}|{name}" : $"{path}:{name}";
        
        if (isEntity)
        {
            string fp = $"{path}.model";
            
            var def = new SeDefinitionEntityNode { UidName = fp };
            var inst = SeInstanceNode.CreateObject<SeInstanceEntityNode>(def);
            
            _nodes.Add(def);
            _nodes.Add(inst);
            
            def.Parent = (parent != null) ? ((SeInstanceNode)_nodeCache[parent]).Definition : null;
            inst.Parent = (parent != null) ? _nodeCache[parent] : _scene;
            
            _nodeCache[node] = inst;

            if (node.Mesh != null) throw new Exception("SE_ENTITY nodes cannot have meshes attached directly!");
            List<Node> submeshes = [];
            foreach (Node? child in node.VisualChildren)
                GenerateMeshHierarchy(child, submeshes);
            
            if (submeshes.Count != 0)
            {
                // hack fix up how this works later
                config.Skeleton = skeleton;
                SlModel model = DoLegacyImport(submeshes.Select(x => x.Mesh!).ToList());
                model.Header.SetName(fp);
                model.Resource.Header.SetName($"{fp}Resource");
                _modelCache[node] = model;
                
                if (skeleton != null)
                    model.Resource.EntityIndex = skeleton.Joints.FindIndex(j => j.Name == node.Name);
                
                // todo: temp hack
                foreach (var command in model.Resource.RenderCommands)
                {
                    if (command is TestVisibilityNoSphereCommand vis)
                    {
                        vis.LocatorIndex = 1;
                    }

                    if (command is RenderSegmentCommand ren)
                    {
                        ren.PivotJoint = 1;
                    }
                }
                
                config.Database.AddResource(model);
            }
        }

        if (isAnimator)
        {
            string fp = $"{path}.skeleton";

            var def = new SeDefinitionAnimatorNode { UidName = fp };
            var inst = SeInstanceNode.CreateObject<SeInstanceAnimatorNode>(def);
            
            _nodes.Add(def);
            _nodes.Add(inst);
            
            def.Parent = (parent != null) ? ((SeInstanceNode)_nodeCache[parent]).Definition : null;
            inst.Parent = (parent != null) ? _nodeCache[parent] : _scene;
            
            _nodeCache[node] = inst;
            
            // Just pre-generate our skeleton joints here to save us a headache
            skeleton = new SlSkeleton();
            skeleton.Header.SetName(fp);
            foreach (Node child in node.VisualChildren)
                GenerateSkeletonHierachy(child, null, skeleton);
            config.Database.AddResource(skeleton);   
        }
        
        foreach (Node child in node.VisualChildren)
            HandleSceneNode(child, path, node, skeleton);
    }

    private void ImportMaterials()
    {
        // The materials define what vertex formats we're going to need for each
        // mesh segment, so register all of them first.
        foreach (Material material in _gltf.LogicalMaterials)
        {
            if (_materialCache.ContainsKey(material)) continue;
            
            MaterialData data = RegisterMaterial(material);
            data.VertexBufferFormat = StreamFlags.Position | StreamFlags.Normal | StreamFlags.TextureCoordinates |
                                      StreamFlags.Tangents;
            
            _materialCache.Add(material, data);
        }   
    }

    public void ImportHierarchy()
    {
        ImportMaterials();
        
        var roots = _gltf.DefaultScene.VisualChildren;
        foreach (Node? root in roots)
        {
            HandleSceneNode(root, _fileName, null);
        }
        
        foreach (Animation? glAnimation in _gltf.LogicalAnimations)
        {
            var anim = new SlAnim();
            
            // Hope the scenegraph is sane
            var node = glAnimation.Channels[0].TargetNode;
            SlSkeleton skeleton = _nodeSkeletonMap[node];
            var animator = (SeDefinitionAnimatorNode)_nodes.Find(n => n.Uid == skeleton.Header.Id)!;

            ((SeInstanceEntityNode)animator.Instances[0].FirstChild!).TransformFlags = 1;
            ((SeDefinitionEntityNode)animator.FirstChild!).TransformFlags = 1;
            
            string tag =
                $"{animator.ShortName.Replace(".skeleton", string.Empty).Replace("animator", "anim_stream")}|{glAnimation.Name.ToLower()}";
            anim.Header.SetName(animator.UidName.Replace(".skeleton", $"|{tag}.anim"));
            
            AnimationChannel channel = glAnimation.Channels[0];
            var sampler = channel.GetTranslationSampler();
            var keys = sampler.GetLinearKeys().ToList();
            
            anim.Skeleton = new SlResPtr<SlSkeleton>(skeleton);
            anim.AnimationTime = glAnimation.Duration;
            anim.FrameRate = 24;
            float delta = (1.0f / anim.FrameRate);
            anim.FrameCount = (short)keys.Count;
            anim.BoneCount = (short)skeleton.Joints.Count;

            // Tell game to use uncompressed channels,
            // makes my life easier
            anim.RotationType = 0;
            anim.PositionType = 0;
            anim.ScaleType = 0;
            anim.AttributeType = 0;

            anim.PositionJoints.Add(1);
            anim.PositionFrameCommands.Add(48);

            var branch = new SlAnim.SlAnimBlendBranch
            {
                FrameOffset = 0,
                NumFrames = anim.FrameCount,
                Flags = 819
            };

            SlAnim.SlAnimBlendLeaf leaf = branch.Leaf;
            leaf.NumFrames = anim.FrameCount;

            int dataSize = 0;
            int translationBasisOffset = dataSize;
            dataSize = SlUtil.Align(dataSize + 0xc, 0x10);
            int translationFramesOffset = dataSize;
            dataSize = SlUtil.Align(dataSize + anim.FrameCount * 0xc, 0x10);
            int frameFlagsOffset = dataSize;
            dataSize = SlUtil.Align(dataSize + ((anim.FrameCount + 7) / 8), 0x10);
            int boneFlagsOffset = dataSize;
            dataSize = SlUtil.Align(dataSize + ((1 + 7) / 8), 0x10);
            
            
            byte[] data = new byte[dataSize];
            
            // Just say we have data for every frame, which I mean, we do
            var frameFlags = data.AsSpan(frameFlagsOffset, ((anim.FrameCount + 7) / 8));
            frameFlags.Fill(0xFF);
            
            data.WriteFloat3(node.LocalTransform.Translation, translationBasisOffset);
            for (int i = 0; i < anim.FrameCount; ++i)
            {
                data.WriteFloat3(keys[i].Value, translationFramesOffset + (i * 0xc));
            }
            
            leaf.Offsets[1] = (short)translationBasisOffset;
            leaf.Offsets[5] = (short)translationFramesOffset;
            leaf.Offsets[8] = (short)frameFlagsOffset;
            leaf.Offsets[13] = (short)boneFlagsOffset;
            leaf.Data = data;
            
            anim.BlendBranches.Add(branch);
            
            config.Database.AddResource(anim);
            
            var def = new SeDefinitionAnimationStreamNode() { UidName = anim.Header.Name, PlayLooped = true, AutoPlay = true };
            var inst = SeInstanceNode.CreateObject<SeInstanceAnimationStreamNode>(def);
            
            
            _nodes.Add(def);
            _nodes.Add(inst);
            
            inst.Parent = animator.Instances[0];
            def.Parent = animator;

            def.Tag = tag;
            inst.Tag = tag;
        }
        
        foreach (SeNodeBase node in _nodes)
            config.Database.AddNode(node);
        
    }

    public SlModel Import()
    {
        return DoLegacyImport(_gltf.LogicalMeshes);
    }
    
    public SlModel DoLegacyImport(IReadOnlyList<Mesh> meshes)
    {
        SlModel model = new();
        SlModelResource resource = model.Resource;
        List<MaterialData> materials = [];
        
        Vector3 minGlobalVert = new(float.PositiveInfinity); 
        Vector3 maxGlobalVert = new(float.NegativeInfinity);
     
        ImportMaterials();
        
        // Do a first pass over the meshes to calculate the
        // vertices needed for each stream, as well as the total index count
        int numIndices = 0;
        Dictionary<StreamFlags, int> verticesPerStream = [];
        foreach (Mesh mesh in meshes)
        foreach (MeshPrimitive primitive in mesh.Primitives)
        {
            int vertexCount = primitive.VertexAccessors["POSITION"].Count;
            StreamFlags flags = _materialCache[primitive.Material].VertexBufferFormat;
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

        bool hasSkinnedSegment = false;
        
        // Start adding data to all streams and creating mesh segments
        foreach (Mesh mesh in meshes)
        foreach (MeshPrimitive primitive in mesh.Primitives)
        {
            int segmentIndex = resource.Segments.Count;
            int vertexCount = primitive.VertexAccessors["POSITION"].Count;
            int indexCount = primitive.IndexAccessor.Count;
            
            int materialIndex = materials.FindIndex(material => material.Model == primitive.Material);
            if (materialIndex == -1)
            {
                materialIndex = materials.Count;
                materials.Add(_materialCache[primitive.Material]);
            }
            
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
                FirstIndex = firstIndex,
                Sectors = [sector],
                Format = builder.Format,
                VertexStreams = builder.Streams,
                IndexStream = indexStream,
            };
            
            builder.RegisterSegment(segment, primitive);

            Vector3 max = segment.Sector.Center + segment.Sector.Extents;
            Vector3 min = segment.Sector.Center - segment.Sector.Extents;
            
            maxGlobalVert = Vector3.Max(maxGlobalVert, max);
            minGlobalVert = Vector3.Min(minGlobalVert, min);
            
            var indices = primitive.GetIndices();
            for (int i = 0; i < indexCount; ++i)
                indexStream.Data.WriteInt16((short)indices[i], (firstIndex * 2) + (i * 2));
            resource.Segments.Add(segment);
            firstIndex += indexCount;
            
            bool skinned = primitive.VertexAccessors.ContainsKey("WEIGHTS_0");
            hasSkinnedSegment |= skinned;
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
        
        foreach (MaterialData material in materials)
            model.Materials.Add(new SlResPtr<SlMaterial2>(material.Material));
        
        resource.Skeleton = new SlResPtr<SlSkeleton>(_config.Skeleton);
        resource.PlatformResource.IndexStream = indexStream;
        foreach (StreamBuilder builder in vertexStreamBuilders.Values)
        {
            resource.PlatformResource.Declarations.Add(builder.Format);
            foreach (SlStream? stream in builder.Streams)
            {
                if (stream == null) continue;
                stream.Gpu = !hasSkinnedSegment;
                resource.PlatformResource.VertexStreams.Add(stream);
            }
        }
        
        model.Header.SetName(Path.ChangeExtension(_fileName, ".model"));
        model.Resource.Header.SetName(Path.ChangeExtension(_fileName, ".modelResource"));
        model.WorkArea = new byte[SlUtil.Align(workAreaSize, 0x10)];
        
        
        Vector3 center = (minGlobalVert + maxGlobalVert) / 2.0f;
        Vector3 extents = Vector3.Max(Vector3.Abs(maxGlobalVert - center), Vector3.Abs(minGlobalVert - center));

        model.CullSphere.SphereCenter = center;
        model.CullSphere.BoxCenter = new Vector4(center, 1.0f);
        model.CullSphere.Extents = new Vector4(extents, 0.0f);
        model.CullSphere.Radius = Math.Max(Math.Max(extents.X, extents.Y), extents.Z);
        
        return model;
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

    private class MaterialData(Material model, SlMaterial2 material, StreamFlags format)
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

        public void RegisterSegment(SlModelSegment segment, MeshPrimitive primitive)
        {
            int vertexOffset = _vertexPointer;
            int vertexCount = primitive.VertexAccessors["POSITION"].Count;
            foreach (string attribute in UsageLookup.Keys)
            {
                int usage = UsageLookup[attribute];
                if (!Format.HasAttribute(usage)) continue;
                
                // Maybe this should throw an error or a warning instead?
                if (!primitive.VertexAccessors.TryGetValue(attribute, out Accessor? accessor)) continue;

                var vertices = accessor.Dimensions switch
                {
                    DimensionType.VEC2 => accessor.AsVector2Array().Select(v => new Vector4(v, 0.0f, 1.0f)).ToArray(),
                    DimensionType.VEC3 => accessor.AsVector3Array().Select(v => new Vector4(v, 1.0f)).ToArray(),
                    _ => accessor.AsVector4Array().ToArray()
                };

                if (attribute == "POSITION")
                {
                    Vector4 max = new(float.NegativeInfinity);
                    Vector4 min = new(float.PositiveInfinity);
                    foreach (Vector4 vertex in vertices)
                    {
                        max = Vector4.Max(vertex, max);
                        min = Vector4.Min(vertex, min);
                    }
                    
                    Vector4 center = (min + max) / 2.0f;
                    Vector4 extents = Vector4.Max(Vector4.Abs(max - center), Vector4.Abs(min - center));
                    
                    segment.Sector.Center = new Vector3(center.X, center.Y, center.Z);
                    segment.Sector.Extents = new Vector3(extents.X, extents.Y, extents.Z);
                }
                
                format.Set(Streams, usage, vertices, vertexOffset);
            }
            
            _vertexPointer += vertexCount;
            segment.VertexStart = vertexOffset;
        }
    }
}