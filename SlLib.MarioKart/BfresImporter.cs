using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using BfresLibrary;
using BfresLibrary.Helpers;
using BfresLibrary.Switch;
using DirectXTexNet;
using SixLabors.ImageSharp;
using SlLib.Extensions;
using SlLib.IO;
using SlLib.Resources;
using SlLib.Resources.Database;
using SlLib.Resources.Model;
using SlLib.Resources.Model.Commands;
using SlLib.Resources.Scene.Definitions;
using SlLib.Utilities;
using Syroot.Maths;
using Syroot.NintenTools.NSW.Bntx;
using Syroot.NintenTools.NSW.Bntx.GFX;
using Image = DirectXTexNet.Image;
using Vector3 = System.Numerics.Vector3;
using Vector4 = System.Numerics.Vector4;

namespace SlLib.MarioKart;

public class BfresImporter
{
    private static readonly SlResourceDatabase ShaderCache =
        SlResourceDatabase.Load(@$"{KartConstants.ShaderCache}\shadercache.cpu.spc", @$"{KartConstants.ShaderCache}\shadercache.gpu.spc", inMemory: true);
    private const float VertexScale = 0.1f;
    
    private Dictionary<TextureShared, SlTexture> _textures = [];
    private Dictionary<Material, SlMaterial2> _materials = [];
    
    private string[] UsageNames = ["_p0", "_u0", "_n0", "_t0"];
    private int[] UsageMap = [SlVertexUsage.Position, SlVertexUsage.TextureCoordinate, SlVertexUsage.Normal, SlVertexUsage.Tangent];
    
    public SlResourceDatabase Database = new(SlPlatform.Win32);
    
    private ResFile _bfres;
    private string _scene = string.Empty;
    private string _path = string.Empty;

    public void Register(string path)
    {
        byte[] data = File.ReadAllBytes($"{KartConstants.MarioRoot}/{path}");
        if (path.EndsWith(".szs"))
            data = szs.Decode(data);
        
        _bfres = new ResFile(new MemoryStream(data));
        _scene = Path.GetFileNameWithoutExtension(path);
        _path = path;
        
        foreach (Model model in _bfres.Models.Values)
            Register(model);
    }

    public SlTexture Register(TextureShared sharedTexture, bool isNormalTexture = false)
    {
        if (_textures.TryGetValue(sharedTexture, out SlTexture? slTexture))
            return slTexture;
        
        Texture texture = ((SwitchTexture)sharedTexture).Texture;

        var slTextureType = SlTexture.SlTextureType.None;
        var format = DXGI_FORMAT.UNKNOWN;
        bool needsConversion = false;
        switch (texture.Format)
        {
            case SurfaceFormat.BC1_SRGB:
            case SurfaceFormat.BC1_UNORM:
                slTextureType = SlTexture.SlTextureType.Bc1;
                format = DXGI_FORMAT.BC1_UNORM;
                break;
            case SurfaceFormat.BC2_SRGB:
            case SurfaceFormat.BC2_UNORM:
                slTextureType = SlTexture.SlTextureType.Bc2;
                format = DXGI_FORMAT.BC2_UNORM;
                break;
            case SurfaceFormat.BC3_SRGB:
            case SurfaceFormat.BC3_UNORM:
                slTextureType = SlTexture.SlTextureType.Bc3;
                format = DXGI_FORMAT.BC3_UNORM;
                break;
            case SurfaceFormat.BC4_UNORM:
                slTextureType = SlTexture.SlTextureType.Bc3;
                format = DXGI_FORMAT.BC4_UNORM;
                needsConversion = true;
                break;
            case SurfaceFormat.BC4_SNORM:
                slTextureType = SlTexture.SlTextureType.Bc3;
                format = DXGI_FORMAT.BC4_SNORM;
                needsConversion = true;
                break;
            case SurfaceFormat.BC5_UNORM:
                slTextureType = SlTexture.SlTextureType.Bc3;
                format = DXGI_FORMAT.BC5_UNORM;
                needsConversion = true;
                break;
            case SurfaceFormat.BC5_SNORM:
                slTextureType = SlTexture.SlTextureType.Bc3;
                format = DXGI_FORMAT.BC5_SNORM;
                needsConversion = true;
                break;
            default:
                throw new Exception($"Unsupported texture format! {texture.Format.ToString()}");
        }

        byte[] textureData;
        if (needsConversion)
        {
            
            textureData = DdsUtil.DoShittyConvertTexture(DXGI_FORMAT.BC3_UNORM, format, sharedTexture.GetDeswizzledData(0, 0), (int)sharedTexture.Width, (int)sharedTexture.Height, isNormalTexture);
        }
        else
        {
            var metadata = new TexMetadata((int)texture.Width, (int)texture.Height, (int)texture.Depth, 1, (int)texture.MipCount,
                0, 0, format, (TEX_DIMENSION)(texture.Dim + 1));

            DdsUtil.DDS_HEADER header = DdsUtil.GenerateHeader(metadata);
            using var stream = new MemoryStream();
            stream.Write("DDS "u8);
            stream.Write(MemoryMarshal.Cast<DdsUtil.DDS_HEADER, byte>(new Span<DdsUtil.DDS_HEADER>(ref header)));
            for (int i = 0; i < texture.MipCount; ++i)
                stream.Write(sharedTexture.GetDeswizzledData(0, i));
            stream.Flush();
            textureData = stream.ToArray();
        }
        

        slTexture = new SlTexture
        {
            Width = (int)texture.Width,
            Height = (int)texture.Height,
            Format = slTextureType,
            Mips = (int)texture.MipCount,
            Data = textureData
        };

        _textures[sharedTexture] = slTexture;
        
        slTexture.Header.SetName($"{_path}:{texture.Name.ToLower()}.texture");
        Database.AddResource(slTexture);

        return slTexture;
    }

    public SlMaterial2 Register(Material material)
    {
        if (_materials.TryGetValue(material, out SlMaterial2? slMaterial))
            return slMaterial;
        
        int albedoTextureIndex = material.Samplers.IndexOf("_a0");
        int normalTextureIndex = material.Samplers.IndexOf("_n0");
        int specularTextureIndex = material.Samplers.IndexOf("_s0");
        int emissiveTextureIndex = material.Samplers.IndexOf("_e0");
        
        bool hasDiffuseTexture = albedoTextureIndex != -1;
        bool hasNormalTexture = normalTextureIndex != -1;
        bool hasEmissiveTexture = emissiveTextureIndex != -1;
        bool hasSpecularTexture = specularTextureIndex != -1;
        bool hasAlpha = material.GetRenderInfoString("gsys_alpha_test_enable") == "true";
        bool hasTexture = hasDiffuseTexture || hasNormalTexture || hasEmissiveTexture || hasSpecularTexture;

        List<string> attributes = ["vp", "vn"];
        if (hasTexture) attributes.Add("vt");
        if (hasNormalTexture) attributes.Add("vtg");
        attributes.Add("tnv");
        if (hasNormalTexture) attributes.Add("ttv");
        if (hasAlpha) attributes.Add("cat");
        else attributes.Add(hasDiffuseTexture ? "ct" : "cm");
        if (hasNormalTexture) attributes.Add("nt");
        attributes.Add(hasSpecularTexture ? "st" : "sm");
        if (hasEmissiveTexture) attributes.Add("et");
        attributes.AddRange(["cma", "s", "u", "p", "f"]);
        if (hasDiffuseTexture && hasAlpha)
            attributes.Add("ct");
        
        string header = string.Join('_', attributes) + "_co_so_go_god_sod";
        slMaterial = ShaderCache.FindResourceByPartialName<SlMaterial2>(header, instance: true);
        if (slMaterial == null)
        {
            // try switching to blinn
            header = header.Replace("_p_", "_b_");
            slMaterial = ShaderCache.FindResourceByPartialName<SlMaterial2>(header, instance: true);
        }
        if (slMaterial == null)
        {
            // sacrifice emission to find a match, a noble sacrifice...
            header = header.Replace("_em", string.Empty);
            header = header.Replace("_et", string.Empty);
            slMaterial = ShaderCache.FindResourceByPartialName<SlMaterial2>(header, instance: true);
        }
        
        if (slMaterial == null)
            throw new ArgumentException("Could not find valid shader template for given material! " + header);

        if (hasDiffuseTexture)
        {        
            string name = material.TextureRefs[albedoTextureIndex].Name;
            SlTexture texture = Register(_bfres.Textures[name]);
            slMaterial.SetTexture("gDiffuseTexture", texture);
            slMaterial.SetTexture("gAlbedoTexture", texture);
        }

        if (hasNormalTexture)
        {
            slMaterial.SetTexture("gNormalTexture", Register(_bfres.Textures[material.TextureRefs[normalTextureIndex].Name], true));   
        }
        if (hasSpecularTexture)
        {
            slMaterial.SetTexture("gSpecularTexture", Register(_bfres.Textures[material.TextureRefs[specularTextureIndex].Name]));   
        }
        if (hasEmissiveTexture)
        {
            float intensity = (float)material.ShaderParams["emission_intensity"].DataValue;
            float[] color = (float[])material.ShaderParams["emission_color"].DataValue;
            
            slMaterial.SetTexture("gEmissiveTexture", Register(_bfres.Textures[material.TextureRefs[emissiveTextureIndex].Name]));

            Vector4 v = new Vector4(color[0], color[1], color[2], 1.0f) * intensity;
            v.W = 1.0f;
            slMaterial.SetConstant("gEmissiveColour", v);
        }

        float[] albedoColorElements = (float[])material.ShaderParams["albedo_tex_color"].DataValue;
        Vector4 albedoColor = new Vector4(albedoColorElements[0], albedoColorElements[1], albedoColorElements[2], 1.0f);
        slMaterial.SetConstant("gDiffuseColour", albedoColor);
        slMaterial.SetConstant("gAlbedoColour", albedoColor);
        
        slMaterial.SetConstant("gSpecularColour",  new Vector4((float)material.ShaderParams["specular_intensity"].DataValue, 0.0f, 0.0f, (float)material.ShaderParams["shiny_specular_fresnel"].DataValue));
        slMaterial.SetConstant("gAlphaRef", new Vector4((float)material.ShaderParams["gsys_alpha_test_ref_value"].DataValue, 0.0f, 0.0f, 0.0f));
        
        slMaterial.Header.SetName($"{_path}:{material.Name.ToLower()}.material");
        
        ShaderCache.CopyResourceByHash<SlShader>(Database, slMaterial.Shader.Id);
        foreach (SlConstantBuffer buffer in slMaterial.ConstantBuffers)
            ShaderCache.CopyResourceByHash<SlConstantBufferDesc>(Database, buffer.ConstantBufferDesc.Id);
        Database.AddResource(slMaterial);

        _materials[material] = slMaterial;
        
        return slMaterial;
    }
    
    public void Register(Model model)
    {
        var slModel = new SlModel();
        
        slModel.Resource.CullSpheres.Add(slModel.CullSphere);
        
        // z -= 100
        
        SlModelResource resource = slModel.Resource;
        slModel.Header.SetName($"{_path}:se_entity_{model.Name.ToLower()}.model");
        slModel.Resource.Header.SetName(slModel.Header.Name + "Resource");
        foreach (Material? material in model.Materials.Values)
            slModel.Materials.Add(new SlResPtr<SlMaterial2>(Register(material)));
        
        var format = new SlVertexDeclaration();
        format.AddAttribute(0, 0, SlVertexElementType.Float, 4, SlVertexUsage.Position, 0);
        format.AddAttribute(1, 0, SlVertexElementType.Float, 4, SlVertexUsage.Normal, 0);
        format.AddAttribute(1, 0x10, SlVertexElementType.Float, 4, SlVertexUsage.Tangent, 0);
        format.AddAttribute(2, 0, SlVertexElementType.Half, 2, SlVertexUsage.TextureCoordinate, 0);

        // Do a first pass over all the shapes/meshes to see how many we actually need
        // Right now we'll ignore any LOD meshes
        int totalNumVerts = 0, totalNumIndices = 0;
        foreach (Shape shape in model.Shapes.Values)
        {
            // First mesh is the primary LOD apparently?
            Mesh mesh = shape.Meshes.First();
            
            totalNumIndices += (int)mesh.IndexCount;
            totalNumVerts += (int)mesh.GetIndices().Max() + 1;
        }

        var indexStream = new SlStream(totalNumIndices, 2);
        var streams = format.Create(totalNumVerts);
        int firstIndex = 0, vertexStart = 0;
        int workAreaSize = 0, commandDataSize = 0;

        var modelMinVert = new Vector3(float.PositiveInfinity);
        var modelMaxVert = new Vector3(float.NegativeInfinity);
        var modelCenter = Vector3.Zero;
        
        foreach (Shape shape in model.Shapes.Values)
        {
            Material material = model.Materials[shape.MaterialIndex];
            SlMaterial2 slMaterial = _materials[material];
            
            VertexBuffer buffer = model.VertexBuffers[shape.VertexBufferIndex];
            var helper = new VertexBufferHelper(buffer, _bfres.ByteOrder);
            
            Mesh mesh = shape.Meshes.First();
            var indices = mesh.GetIndices().ToList();
            int vertexCount = (int)indices.Max() + 1;
            int numIndices = indices.Count;
            
            int segmentIndex = resource.Segments.Count;
            int materialIndex = slModel.Materials.FindIndex(ptr => ptr.Instance == slMaterial);
            var sector = new SlModelSector
            {
                NumElements = numIndices,
                NumVerts = vertexCount,
            };
            
            var segment = new SlModelSegment
            {
                PrimitiveType = SlPrimitiveType.Triangles,
                MaterialIndex = materialIndex,
                FirstIndex = firstIndex,
                VertexStart = vertexStart,
                Sectors = [sector],
                Format = format,
                VertexStreams = streams,
                IndexStream = indexStream
            };
            
            resource.Segments.Add(segment);
            for (int i = 0; i < numIndices; ++i)
                indexStream.Data.WriteInt16((short)indices[i], (firstIndex * 2) + (i * 2));

            for (int i = 0; i < UsageMap.Length; ++i)
            {
                if (!helper.Contains(UsageNames[i])) continue;

                float vertexScale = UsageNames[i] == "_p0" ? VertexScale : 1.0f;
                var data = helper[UsageNames[i]].Data
                    .Skip((int)mesh.FirstVertex)
                    .Take(vertexCount)
                    .Select(v => new Vector4(v.X, v.Y, v.Z, v.W) * vertexScale).ToArray();
                
                format.Set(streams, UsageMap[i], data, vertexStart);
            }
            
            firstIndex += (int)mesh.IndexCount;
            vertexStart += vertexCount;

            List<IRenderCommand> commands = [];
            commands.Add(new RenderSegmentCommand
            {
                MaterialIndex = (short)materialIndex,
                PivotJoint = -1,
                SegmentIndex = (short)segmentIndex,
                WorkPass = workAreaSize
            });

            // Depending on whether or not the mesh is skinned, we use different visibility testing
            commandDataSize += commands.Sum(command => command.Size);
            
            var command = new TestVisibilityCommand
            {
                CalculateCullMatrix = false,
                LocatorIndex = -1,
                CullSphereIndex = (short)slModel.Resource.CullSpheres.Count,
                Flags = 0x11,
            };
                
            commandDataSize += command.Size;
            command.BranchOffset = commandDataSize;
            resource.RenderCommands.Add(command);
            
            resource.RenderCommands.AddRange(commands);

            Bounding bounding = shape.SubMeshBoundings.Last();
            Vector3 extents = new Vector3(bounding.Extent.X, bounding.Extent.Y, bounding.Extent.Z) * KartConstants.GameScale;
            Vector3 center = new Vector3(bounding.Center.X, bounding.Center.Y, bounding.Center.Z) * KartConstants.GameScale;
            
            segment.Sector.Extents = extents;
            segment.Sector.Center = center;
            
            modelMaxVert = Vector3.Max(modelMaxVert, center + extents);
            modelMinVert = Vector3.Min(modelMinVert, center - extents);
            modelCenter += center;
            
            slModel.Resource.CullSpheres.Add(new SlCullSphere
            {
                BoxCenter = new Vector4(center, 1.0f),
                SphereCenter = center,
                Extents = new Vector4(extents, 0.0f),
                Radius = shape.RadiusArray.First() * KartConstants.GameScale
            });
        }
        
        modelCenter /= slModel.Resource.Segments.Count;
        Vector3 modelExtents = Vector3.Abs(modelMaxVert - modelMinVert) / 2.0f;
        
        slModel.CullSphere = new SlCullSphere
        {
            BoxCenter = new Vector4(modelCenter, 1.0f),
            SphereCenter = modelCenter,
            Extents = new Vector4(modelExtents, 0.0f),
            Radius = Math.Max(modelExtents.X, Math.Max(modelExtents.Y, modelExtents.Z))
        };

        resource.PlatformResource.IndexStream = indexStream;
        resource.PlatformResource.Declarations.Add(format);
        foreach (SlStream? stream in streams)
        {
            if (stream == null) continue;
            resource.PlatformResource.VertexStreams.Add(stream);   
        }

        slModel.WorkArea = new byte[SlUtil.Align(workAreaSize, 0x10)];
        Database.AddResource(slModel);

        var slEntity = new SeDefinitionEntityNode
        {
            UidName = slModel.Header.Name,
            Model = new SlResPtr<SlModel>(slModel)
        };
        Database.AddNode(slEntity);
        
        Console.WriteLine(slEntity.UidName);
        
        // _p0 = position
        // _n0 = normals
        // _t0 = tangents
        // _u0 = UV0
        // _u1 = UV1
        // _i0 = bone indices
        // _w0 = bone weights
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
}