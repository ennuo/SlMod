using System.Numerics;
using System.Reflection.Metadata;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using SharpGLTF.Validation;
using SixLabors.ImageSharp;
using SlLib.Extensions;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Scene;
using SlLib.Resources.Scene.Definitions;
using SlLib.Resources.Skeleton;
using SlLib.Utilities;
using Image = SharpGLTF.Schema2.Image;

namespace SlLib.IO;

public sealed class SlSceneExporter
{
    private readonly ModelRoot _gltf;
    private readonly Scene _scene;
    
    private readonly Dictionary<int, Mesh> _meshCache = [];
    private readonly Dictionary<int, Node[]> _skeletonCache = [];
    private readonly Dictionary<int, Material> _materialCache = [];
    private readonly Dictionary<int, Image> _textureCache = [];
    
    public SlSceneExporter()
    {
        _gltf = ModelRoot.CreateModel();
        _scene = _gltf.UseScene(0);
    }
    
    private Material RegisterMaterial(SlMaterial2? slMaterial)
    {
        ArgumentNullException.ThrowIfNull(slMaterial);
        if (_materialCache.TryGetValue(slMaterial.Header.Id, out Material? material))
            return material;

        string name = Path.GetFileNameWithoutExtension(SlUtil.GetShortName(slMaterial.Header.Name));
        material = _gltf.CreateMaterial(name);
        material.InitializePBRMetallicRoughness();
        material.DoubleSided = true;

        SlSampler? diffuseSampler = slMaterial.Samplers.Find(sampler => sampler.Header.Name is "gDiffuseTexture" or "gAlbedoTexture");
        MaterialChannel baseColorChannel = material.FindChannel("BaseColor").GetValueOrDefault();
        if (diffuseSampler != null && diffuseSampler.Texture.Instance != null && diffuseSampler.HasTextureData() && diffuseSampler.Texture.Instance!.Header.Platform != SlPlatform.WiiU)
        {
            baseColorChannel.SetTexture(0, RegisterTexture(diffuseSampler.Texture));
        }
        else if (slMaterial.HasConstant("gDiffuseColour"))
        {
            baseColorChannel.Color = slMaterial.GetConstant("gDiffuseColour");
        }


        MaterialChannel pbrChannel = material.FindChannel("MetallicRoughness").GetValueOrDefault();
        pbrChannel.SetFactor("MetallicFactor", 0.0f);
        
        // MaterialChannel specularColorChannel = material.FindChannel("SpecularGlossiness").GetValueOrDefault();
        // if (slMaterial.HasConstant("gSpecularColour"))
        // {
        //     float factor = slMaterial.GetConstant("gSpecularColour").X;
        //     foreach (IMaterialParameter parameter in specularColorChannel.Parameters)
        //     {
        //         Console.WriteLine(parameter.Name);
        //     }
        //     
        //     
        //     // specularColorChannel.Parameters[0].Value = new Vector3(factor);
        // }
        // else
        // {
        //     specularColorChannel.Parameters[0].Value = Vector3.Zero;
        // }


        if (slMaterial.HasConstant("gAlphaRef"))
        {
            float alphaRef = slMaterial.GetConstant("gAlphaRef").X;
            if (alphaRef > 0.0f)
            {
                material.Alpha = AlphaMode.MASK;
                material.AlphaCutoff = alphaRef;
            }   
        }
        
        //slMaterial.PrintConstantValues();
        
        SlSampler? normalSampler = slMaterial.Samplers.Find(sampler => sampler.Header.Name is "gNormalTexture");
        if (normalSampler != null && normalSampler.Texture.Instance != null && normalSampler.HasTextureData() && normalSampler.Texture.Instance!.Header.Platform != SlPlatform.WiiU)
        {
            MaterialChannel channel = material.FindChannel("Normal").GetValueOrDefault();
            channel.SetTexture(0, RegisterTexture(normalSampler.Texture));
        }
        
        _materialCache[slMaterial.Header.Id] = material; 
        return material;
    }

    private Image RegisterTexture(SlTexture? texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (_textureCache.TryGetValue(texture.Header.Id, out Image? image))
            return image;

        image = _gltf.CreateImage();
        image.Name = texture.Header.Name;

        using var stream = new MemoryStream();
        texture.GetImage().SaveAsPng(stream);
        image.Content = new MemoryImage(stream.ToArray());
        
        _textureCache[texture.Header.Id] = image;
        return image;
    }
    
    private Node[] RegisterSkeleton(SlSkeleton? skeleton, Node? animator)
    {
        if (skeleton == null) return [];
        
        // If the skeleton was already parsed, return the existing node instance
        if (_skeletonCache.TryGetValue(skeleton.Header.Id, out var nodes))
            return nodes;
        
        nodes = new Node[skeleton.Joints.Count];
        
        // Attach all root nodes to the scene
        for (int i = 0; i < skeleton.Joints.Count; ++i)
        {
            SlJoint joint = skeleton.Joints[i];
            if (joint.Parent != -1) continue;
            
            Node? node = animator == null ? _scene.CreateNode(joint.Name) : animator.CreateNode(joint.Name);
            nodes[i] = node;
            node.LocalTransform = new AffineTransform(joint.Scale, joint.Rotation, joint.Translation);
            
            RecurseSkeletonHierarchy(node, i);
        }
        
        // Cache the skeleton in-case it's referenced again
        _skeletonCache[skeleton.Header.Id] = nodes;
        return nodes;

        // Recursively attaches children to each root node
        void RecurseSkeletonHierarchy(IVisualNodeContainer node, int index)
        {
            for (int i = 0; i < skeleton.Joints.Count; ++i)
            {
                SlJoint child = skeleton.Joints[i];
                if (child.Parent != index) continue;
                Node? childNode = node.CreateNode(child.Name);
                nodes[i] = childNode;
                childNode.LocalTransform = new AffineTransform(child.Scale, child.Rotation, child.Translation);
                RecurseSkeletonHierarchy(childNode, i);
            }
        }
    }

    private void RegisterModel(SlModel model, Node rootNode)
    {
        model.Convert(SlPlatform.Win32);
        bool hasSkeleton = !model.Resource.Skeleton.IsEmpty;
        SlModelResource resource = model.Resource;
        
        var skeleton = RegisterSkeleton(model.Resource.Skeleton, null);
        var materials = model.Materials.Select(material => RegisterMaterial(material)).ToList();
        
        // Node rootNode;
        // string name;
        // if (hasSkeleton)
        // {
        //     rootNode = skeleton[resource.EntityIndex];
        //     name = rootNode.Name;
        // }
        // else
        // {
        //     name = Path.GetFileNameWithoutExtension(model.Header.Name.Split(':')[1])
        //         .Replace("se_entity_", "SE_ENTITY_");
        //     rootNode = _scene.CreateNode(name);
        // }


        // Split the mesh by locators, I guess?
        // Generally only non-skinned meshes should use locators, I think
        List<LocatorGroup> segmentLocatorGroups =
        [
            new LocatorGroup(rootNode, -1) // Treat -1 as the root entity
        ];
        
        // I wonder if one of the flags dictate if a model is skinned?
        // A skeleton doesn't necessarily mean that a mesh is skinned.
        bool isSkinned = resource.Segments.Exists(segment => segment.WeightBuffer.Count != 0);
        int[][] primitiveJointRemap = new int[resource.Segments.Count][];

        var segments = resource.Segments;
        // bool hasLodMeshes = resource.RenderCommands.Exists(command => command is SelectLodCommand);
        // if (hasLodMeshes)
        // {
        //     var primarySegments = new List<SlModelSegment>();
        //     bool isNextSegmentVisible = true;
        //     foreach (IRenderCommand renderCommand in resource.RenderCommands)
        //     {
        //         switch (renderCommand)
        //         {
        //             case TestVisibilityNoSphereCommand visCmd:
        //                 isNextSegmentVisible = visCmd.LodIndex == 0;
        //                 break;
        //             case RenderSegmentCommand renderCmd:
        //             {
        //                 if (isNextSegmentVisible)
        //                 {
        //                     primarySegments.Add(resource.Segments[renderCmd.SegmentIndex]);   
        //                 }
        //                 
        //                 break;
        //             }
        //         }
        //     }
        //
        //     segments = primarySegments;
        // }
        
        // Gather locator groups
        foreach (IRenderCommand renderCommand in resource.RenderCommands)
        {
            if (renderCommand is not RenderSegmentCommand command) continue;
            
            int index = command.PivotJoint;
            LocatorGroup? group = segmentLocatorGroups.Find(group => group.LocatorIndex == index);
            if (group == null)
            {
                group = new LocatorGroup(skeleton[index], index);
                segmentLocatorGroups.Add(group);
            }
                
            group.Segments.Add(command.SegmentIndex);
        }

        Skin? skin = null;
        if (isSkinned)
        {
            List<Node> joints = [];
            List<(Node Joint, Matrix4x4 InverseBindMatrix)> bind = [];
            List<int> jointMap = [];
            
            // The indices and inverse bind matrices for joints are stored in
            // the command buffer, each command can have different joint indices,
            // so we have to account for this.
            
            foreach (IRenderCommand renderCommand in resource.RenderCommands)
            {
                // CalcBindMatricesCommand stores the joint indices and inverses for a given segment
                if (renderCommand is CalcBindMatricesCommand command)
                {
                    for (int i = 0; i < command.NumBones; ++i)
                    {
                        Node joint = skeleton[command.Joints[i]];
                        int index = joints.IndexOf(joint);
                        if (index == -1)
                        {
                            index = joints.Count;
                            joints.Add(joint);

                            Matrix4x4 invBindMatrix = command.InvBindMatrices[i];
                            bind.Add((joint, invBindMatrix));
                        }
                        
                        jointMap.Add(index);
                    }
                }
                
                // Flush the joint map for the current primitive
                if (renderCommand is RenderSegmentCommand renderSegmentCommand)
                {
                    primitiveJointRemap[renderSegmentCommand.SegmentIndex] = jointMap.ToArray();
                    jointMap.Clear();
                }
            }
            
            // Now that we've fixed up the joints, create the skin instance
            skin = _gltf.CreateSkin();
            skin.BindJoints(bind.ToArray());
            //node.LocalTransform = new AffineTransform(Matrix4x4.Identity);
            //node.Skin = skin;
        }
        
        foreach (LocatorGroup group in segmentLocatorGroups)
        {
            if (group.Segments.Count == 0) continue;
            
            Node node = group.Parent;
            Mesh mesh = _gltf.CreateMesh();
            
            node.Mesh = mesh;
            if (skin != null) node.Skin = skin;
            
            foreach (int segmentIndex in group.Segments)
            {
                MeshPrimitive primitive = mesh.CreatePrimitive();
                SlModelSegment segment = segments[segmentIndex];
                SlModelSector sector = segment.Sector;
                SlVertexDeclaration format = segment.Format;
                SlStream?[] streams = segment.VertexStreams;
                SlStream indexStream = segment.IndexStream;

                int vertexStart = segment.VertexStart;
                int firstIndex = segment.FirstIndex;
                int numVerts = sector.NumVerts;
                int numIndices = sector.NumElements;
                
                // Positions
                {
                    var data = new byte[numVerts * 0xc];
                    var elements = new Vector3Array(data);
                    elements.Fill(format.Get(streams, SlVertexUsage.Position, vertexStart, numVerts).Select(v => new Vector3(v.X, v.Y, v.Z)));
                    var accessor = _gltf.CreateAccessor();
                    accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC3, EncodingType.FLOAT, false);
                    primitive.SetVertexAccessor("POSITION", accessor);
                }

                // Normals
                if (format.HasAttribute(SlVertexUsage.Normal))
                {
                    var data = new byte[numVerts * 0xc];
                    var elements = new Vector3Array(data);
                    elements.Fill(format.Get(streams, SlVertexUsage.Normal, vertexStart, numVerts).Select(v => new Vector3(v.X, v.Y, v.Z)));
                    var accessor = _gltf.CreateAccessor();
                    accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC3, EncodingType.FLOAT, false);
                    primitive.SetVertexAccessor("NORMAL", accessor);
                }
                
                // Tangents
                if (format.HasAttribute(SlVertexUsage.Tangent))
                {
                    var data = new byte[numVerts * 0xc];
                    var elements = new Vector3Array(data);
                    elements.Fill(format.Get(streams, SlVertexUsage.Normal, vertexStart, numVerts).Select(v => new Vector3(v.X, v.Y, v.Z)));
                    var accessor = _gltf.CreateAccessor();
                    accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC3, EncodingType.FLOAT, false);
                    primitive.SetVertexAccessor("TANGENT", accessor);
                }

                // Texture Coordinates
                if (format.HasAttribute(SlVertexUsage.TextureCoordinate))
                {
                    var data = new byte[numVerts * 0x8];
                    var elements = new Vector2Array(data);
                    elements.Fill(format.Get(streams, SlVertexUsage.TextureCoordinate, vertexStart, numVerts).Select(v => new Vector2(v.X, v.Y)));
                    var accessor = _gltf.CreateAccessor();
                    accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC2, EncodingType.FLOAT, false);
                    primitive.SetVertexAccessor("TEXCOORD_0", accessor);
                }

                // Afterburner 
                if (format.HasAttribute(SlVertexUsage.TextureCoordinate, 1))
                {
                    var data = new byte[numVerts * 0x8];
                    var elements = new Vector2Array(data);
                    elements.Fill(format.Get(streams, SlVertexUsage.TextureCoordinate, vertexStart, numVerts, 1).Select(v => new Vector2(v.X, v.Y)));
                    var accessor = _gltf.CreateAccessor();
                    accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC2, EncodingType.FLOAT, false);
                    primitive.SetVertexAccessor("TEXCOORD_1", accessor);
                }

                // Indices
                {
                    byte[] data = new byte[numIndices * 2];
                    indexStream.Data.AsSpan(firstIndex * 2, numIndices * 2).CopyTo(data);
                    var accessor = _gltf.CreateAccessor();
                    accessor.SetData(_gltf.UseBufferView(data), 0, numIndices, DimensionType.SCALAR, EncodingType.UNSIGNED_SHORT, false);
                    primitive.SetIndexAccessor(accessor);
                }

                if (isSkinned && segment.WeightBuffer.Count != 0)
                {
                    byte[] weightData = new byte[numVerts * 16];
                    segment.WeightBuffer.CopyTo(weightData);
                    {
                        var accessor = _gltf.CreateAccessor();
                        accessor.SetData(_gltf.UseBufferView(weightData), 0, numVerts, DimensionType.VEC4, EncodingType.FLOAT, false);
                        primitive.SetVertexAccessor("WEIGHTS_0", accessor);
                    }
                    
                    
                    byte[] jointData = new byte[numVerts * 4];
                    
                    // Remap joint buffer to bytes
                    for (int i = 0; i < numVerts; ++i)
                    {
                        int jointBase = i * 0x10;
                        int targetBase = i * 0x4;
                        for (int j = 0; j < 4; ++j)
                        {
                            int joint = segment.JointBuffer.ReadInt32(jointBase + (j * 4));
                            if (joint != -1) joint = primitiveJointRemap[segmentIndex][joint];
                            else joint = 0;
                            jointData[targetBase + j] = (byte)joint;
                        }
                    }
                    
                    {
                        var accessor = _gltf.CreateAccessor();
                        accessor.SetData(_gltf.UseBufferView(jointData), 0, numVerts, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
                        primitive.SetVertexAccessor("JOINTS_0", accessor);
                    }
                }

                primitive.Material = materials[segment.MaterialIndex];
            }
        }

        // Mesh mesh = _gltf.CreateMesh(name);
        // node.Mesh = mesh;
        // for (int segmentIndex = 0; segmentIndex < segments.Count; ++segmentIndex)
        // {
        //     MeshPrimitive primitive = mesh.CreatePrimitive();
        //     SlModelSegment segment = segments[segmentIndex];
        //     SlModelSector sector = segment.Sector;
        //     SlVertexDeclaration format = segment.Format;
        //     SlStream?[] streams = segment.VertexStreams;
        //     SlStream indexStream = segment.IndexStream;
        //
        //     int vertexStart = segment.VertexStart;
        //     int firstIndex = segment.FirstIndex;
        //     int numVerts = sector.NumVerts;
        //     int numIndices = sector.NumElements;
        //     
        //     // Positions
        //     {
        //         var data = new byte[numVerts * 0xc];
        //         var elements = new Vector3Array(data);
        //         elements.Fill(format.Get(streams, SlVertexUsage.Position, vertexStart, numVerts).Select(v => new Vector3(v.X, v.Y, v.Z)));
        //         var accessor = _gltf.CreateAccessor();
        //         accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC3, EncodingType.FLOAT, false);
        //         primitive.SetVertexAccessor("POSITION", accessor);
        //     }
        //
        //     // Normals
        //     if (format.HasAttribute(SlVertexUsage.Normal))
        //     {
        //         var data = new byte[numVerts * 0xc];
        //         var elements = new Vector3Array(data);
        //         elements.Fill(format.Get(streams, SlVertexUsage.Normal, vertexStart, numVerts).Select(v => new Vector3(v.X, v.Y, v.Z)));
        //         var accessor = _gltf.CreateAccessor();
        //         accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC3, EncodingType.FLOAT, false);
        //         primitive.SetVertexAccessor("NORMAL", accessor);
        //     }
        //     
        //     // Tangents
        //     if (format.HasAttribute(SlVertexUsage.Tangent))
        //     {
        //         var data = new byte[numVerts * 0xc];
        //         var elements = new Vector3Array(data);
        //         elements.Fill(format.Get(streams, SlVertexUsage.Normal, vertexStart, numVerts).Select(v => new Vector3(v.X, v.Y, v.Z)));
        //         var accessor = _gltf.CreateAccessor();
        //         accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC3, EncodingType.FLOAT, false);
        //         primitive.SetVertexAccessor("TANGENT", accessor);
        //     }
        //
        //     // Texture Coordinates
        //     if (format.HasAttribute(SlVertexUsage.TextureCoordinate))
        //     {
        //         var data = new byte[numVerts * 0x8];
        //         var elements = new Vector2Array(data);
        //         elements.Fill(format.Get(streams, SlVertexUsage.TextureCoordinate, vertexStart, numVerts).Select(v => new Vector2(v.X, v.Y)));
        //         var accessor = _gltf.CreateAccessor();
        //         accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC2, EncodingType.FLOAT, false);
        //         primitive.SetVertexAccessor("TEXCOORD_0", accessor);
        //     }
        //
        //     // Afterburner 
        //     if (format.HasAttribute(SlVertexUsage.TextureCoordinate, 1))
        //     {
        //         var data = new byte[numVerts * 0x8];
        //         var elements = new Vector2Array(data);
        //         elements.Fill(format.Get(streams, SlVertexUsage.TextureCoordinate, vertexStart, numVerts, 1).Select(v => new Vector2(v.X, v.Y)));
        //         var accessor = _gltf.CreateAccessor();
        //         accessor.SetData(_gltf.UseBufferView(data), 0, numVerts, DimensionType.VEC2, EncodingType.FLOAT, false);
        //         primitive.SetVertexAccessor("TEXCOORD_1", accessor);
        //     }
        //
        //     // Indices
        //     {
        //         byte[] data = new byte[numIndices * 2];
        //         indexStream.Data.AsSpan(firstIndex * 2, numIndices * 2).CopyTo(data);
        //         var accessor = _gltf.CreateAccessor();
        //         accessor.SetData(_gltf.UseBufferView(data), 0, numIndices, DimensionType.SCALAR, EncodingType.UNSIGNED_SHORT, false);
        //         primitive.SetIndexAccessor(accessor);
        //     }
        //
        //     if (isSkinned && segment.WeightBuffer.Count != 0)
        //     {
        //         byte[] weightData = new byte[numVerts * 16];
        //         segment.WeightBuffer.CopyTo(weightData);
        //         {
        //             var accessor = _gltf.CreateAccessor();
        //             accessor.SetData(_gltf.UseBufferView(weightData), 0, numVerts, DimensionType.VEC4, EncodingType.FLOAT, false);
        //             primitive.SetVertexAccessor("WEIGHTS_0", accessor);
        //         }
        //         
        //         
        //         byte[] jointData = new byte[numVerts * 4];
        //         
        //         // Remap joint buffer to bytes
        //         for (int i = 0; i < numVerts; ++i)
        //         {
        //             int jointBase = i * 0x10;
        //             int targetBase = i * 0x4;
        //             for (int j = 0; j < 4; ++j)
        //             {
        //                 int joint = segment.JointBuffer.ReadInt32(jointBase + (j * 4));
        //                 if (joint != -1) joint = primitiveJointRemap[segmentIndex][joint];
        //                 else joint = 0;
        //                 jointData[targetBase + j] = (byte)joint;
        //             }
        //         }
        //         
        //         {
        //             var accessor = _gltf.CreateAccessor();
        //             accessor.SetData(_gltf.UseBufferView(jointData), 0, numVerts, DimensionType.VEC4, EncodingType.UNSIGNED_BYTE, false);
        //             primitive.SetVertexAccessor("JOINTS_0", accessor);
        //         }
        //     }
        //
        //     primitive.Material = materials[segment.MaterialIndex];
        // }
    }
    
    private void RegisterScene(SlResourceDatabase database, string directory, string scene)
    {
        //database.Debug_PrintSceneRoots(scene);
        
        // Let's get the proper file paths, create the structure by finding the root entity nodes belonging to thi sscene
        foreach (SeDefinitionEntityNode entity in database.GetNodesOfType<SeDefinitionEntityNode>(scene))
        {
            // I assume everything should start from a root entity node
            if (entity.Parent != null) continue;
            AddNodeToSceneGraph(entity, null, null);
        }
        
        foreach (SeDefinitionAnimatorNode entity in database.GetNodesOfType<SeDefinitionAnimatorNode>(scene))
        {
            // I assume everything should start from a root entity node
            if (entity.Parent != null) continue;
            AddNodeToSceneGraph(entity, null, null);
        }

        foreach (SeDefinitionLocatorNode entity in database.GetNodesOfType<SeDefinitionLocatorNode>(scene))
        {
            // I assume everything should start from a root entity node
            if (entity.Parent != null) continue;
            AddNodeToSceneGraph(entity, null, null);
        }
        
        var settings = new WriteSettings { Validation = ValidationMode.Skip };
        _gltf.SaveGLB(Path.Join(directory, scene + ".glb"), settings);
        
        return;

        void AddNodeToSceneGraph(SeGraphNode node, Node[]? skeleton, Node? parentGltfNode)
        {
            string name = Path.GetFileNameWithoutExtension(node.ShortName);
            name = name.Replace("se_entity_", "SE_ENTITY_");
            name = name.Replace("se_animator_", "SE_ANIMATOR_");
            name = name.Replace("se_locator_", "SE_LOCATOR_");

            if (node is SeDefinitionAnimationStreamNode) return;
            
            
            // The skeleton will create the hierarchy for most of the components of a node,
            // so pull from the current skeleton if available
            Node gltfNode;
            if (skeleton != null) gltfNode = skeleton.First(joint => string.Equals(name, joint.Name, StringComparison.InvariantCultureIgnoreCase));
            else gltfNode = parentGltfNode == null ? _scene.CreateNode(name) : parentGltfNode.CreateNode(name);
            
            if (node is SeDefinitionTransformNode transformNode)
            {
                gltfNode.LocalTransform = new AffineTransform(transformNode.Scale, transformNode.Rotation,
                    transformNode.Translation);
            }
            
            if (node is SeDefinitionAnimatorNode animatorNode)
                skeleton = RegisterSkeleton(animatorNode.Skeleton, gltfNode);
            if (node is SeDefinitionEntityNode entityNode)
            {
                SlModel? model = entityNode.Model;
                if (model != null) RegisterModel(model, gltfNode);
            }
            
            SeGraphNode? child = node.FirstChild;
            while (child != null)
            {
                AddNodeToSceneGraph(child, skeleton, gltfNode);
                child = child.NextSibling;
            }
        }
    }

    public static void Export(SeDefinitionEntityNode entity, string file)
    {
        var exporter = new SlSceneExporter();
        AddNodeToSceneGraph(entity, null, null);
        
        var settings = new WriteSettings { Validation = ValidationMode.Skip };
        exporter._gltf.SaveGLB(file, settings);
        
        return;

        void AddNodeToSceneGraph(SeGraphNode node, Node[]? skeleton, Node? parentGltfNode)
        {
            string name = Path.GetFileNameWithoutExtension(node.ShortName);
            name = name.Replace("se_entity_", "SE_ENTITY_");
            name = name.Replace("se_animator_", "SE_ANIMATOR_");
            name = name.Replace("se_locator_", "SE_LOCATOR_");
            
            // The skeleton will create the hierarchy for most of the components of a node,
            // so pull from the current skeleton if available
            Node gltfNode;
            if (skeleton != null) gltfNode = skeleton.First(joint => string.Equals(name, joint.Name, StringComparison.OrdinalIgnoreCase));
            else gltfNode = parentGltfNode == null ? exporter._scene.CreateNode(name) : parentGltfNode.CreateNode(name);
            
            if (node is SeDefinitionTransformNode transformNode)
            {
                gltfNode.LocalTransform = new AffineTransform(transformNode.Scale, transformNode.Rotation,
                    transformNode.Translation);
            }
            
            if (node is SeDefinitionAnimatorNode animatorNode)
                skeleton = exporter.RegisterSkeleton(animatorNode.Skeleton, gltfNode);
            if (node is SeDefinitionEntityNode entityNode)
            {
                SlModel? model = entityNode.Model;
                if (model != null) exporter.RegisterModel(model, gltfNode);
            }
            
            SeGraphNode? child = node.FirstChild;
            while (child != null)
            {
                AddNodeToSceneGraph(child, skeleton, gltfNode);
                child = child.NextSibling;
            }
        }
        
    }
    
    public static void Export(SlResourceDatabase database, string directory)
    {
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        
        // Export each scene as a separate file
        foreach (string scene in database.GetSceneList())
        {
            var exporter = new SlSceneExporter();
            exporter.RegisterScene(database, directory, scene);
        }
    }

    private class LocatorGroup(Node node, int index)
    {
        public int LocatorIndex = index;
        public Node Parent = node;
        public List<int> Segments = [];
    }
}