using System.Numerics;
using System.Runtime.Intrinsics;
using KclLibrary;
using SimpleScene.Util.ssBVH;
using SlLib.Enums;
using SlLib.Resources;
using SlLib.Resources.Collision;

namespace SlLib.MarioKart;

public class CollisionImporter
{
    private string _path;
    
    private KCLFile _kcl;
    private SlResourceCollision _resource;
    private SlResourceMesh _mesh;

    private const int TrackMaterialIndex = 0;
    private const int WallMaterialIndex = 1;
    
    private static string[] MarioKartAttributeList =
    [
        "Road 1", "Road 2", "Road 3", "Road 4",
        "Sand", "Light Offroad", "Offroad","Offroad 2", "Heavy Offroad",
        "Slippery", "Dash", "Gravity Pad", "Glider Pad", "Pull",
        "Moving Terrain", "Item Road", "Lakitu Rescue",
        "Wall 1", "Wall 2","Wall 3",
        "LWALL", "Item Wall", "BWALL", "Invisible Wall", "Dummy1",
        "Cannon", "Effect Trigger", "Sound Effect", "Fall Out", "Dummy2", "Dummy3", "Zone"
    ];
    
    public CollisionImporter(string path, string name)
    {
        byte[] data = File.ReadAllBytes(@$"{KartConstants.MarioRoot}\{path}");
        if (path.EndsWith(".szs"))
            data = szs.Decode(data);

        _path = path;
        _kcl = new KCLFile(new MemoryStream(data));
        _resource = new SlResourceCollision();
        _resource.Header.SetName($"{path}:se_collision_{name}.collision");
        _mesh = _resource.Mesh;
        
        SetupDefaultMaterials();
    }

    public SlResourceCollision Import()
    {
        // Gather all prisms from all the meshes, since we don't really
        // have a concept of individual models, although I guess we could
        // group meshes into separate sections, then subdivide that, but whatever.
        List<PrismTriangle> kclTriangles = [];
        foreach (KCLModel model in _kcl.Models)
        foreach (KclPrism prism in model.Prisms)
            kclTriangles.Add(new PrismTriangle(model, prism));
        
        const bool doDumbShit = false;
        if (doDumbShit)
        {
            foreach (PrismTriangle prism in kclTriangles)
            {
                var section = new SlResourceMeshSection
                {
                    Roots = 1,
                    Type = 2
                };
                
                section.Leafs.Add(new SlCollisionResourceLeafNode
                {
                    Data = new SlResourceMeshDataSingleTriangleFloat
                    {
                        A = prism.A,
                        B = prism.B,
                        C = prism.C,

                        Center = prism.Center,
                        Min = prism.Min,
                        Max = prism.Max,
                        
                        CollisionMaterialIndex = prism.IsWall ? WallMaterialIndex : TrackMaterialIndex
                    }
                });
                
                section.Branches.Add(new SlCollisionResourceBranchNode
                {
                    Flags = prism.IsWall ? 4 : 1,
                    Leaf = 0,
                    Next = -1,
                    Center = new Vector4(prism.Center, 0.0f),
                    Extents = new Vector4(prism.Extents, 0.0f)
                });
                
                _mesh.Sections.Add(section);
            }
        }
        else
        {
            Vector3 globalMin = _kcl.MinCoordinate;
            Vector3 globalMax = _kcl.MaxCoordinate;
            var boxSize = new Vector3(100.0f, 100.0f, 100.0f) / KartConstants.GameScale;
            Vector3 boxHalfSize = boxSize / 2.0f;

            Vector3 dim = ((globalMax - globalMin) / boxSize);

            int gridSizeX = (int)Math.Ceiling(dim.X);
            int gridSizeY = (int)Math.Ceiling(dim.Y);
            int gridSizeZ = (int)Math.Ceiling(dim.Z);

            HashSet<PrismTriangle> usedTriangles = [];
            for (int z = 0; z < gridSizeZ; ++z)
            for (int y = 0; y < gridSizeY; ++y)
            for (int x = 0; x < gridSizeX; ++x)
            {
                Vector3 cubePosition = globalMin + boxSize * new Vector3(x, y, z);
                List<PrismTriangle> cubeTriangles = [];
                foreach (var triangle in kclTriangles)
                {
                    if (usedTriangles.Contains(triangle)) continue;
                    if (!TriangleBoxIntersect.TriBoxOverlap(triangle.Triangle, cubePosition + boxHalfSize, boxHalfSize))
                        continue;
                    
                    usedTriangles.Add(triangle);
                    cubeTriangles.Add(triangle);
                }

                if (cubeTriangles.Count == 0) continue;
            
                AddCollisionSection(cubeTriangles, cubePosition * KartConstants.GameScale, boxHalfSize * KartConstants.GameScale);

                //Console.WriteLine($"[{x}:{y}:{z}]({cubePosition}) - {cubeTriangles.Count} triangles");
            }   
        }
        
        return _resource;
    }
    
    private void AddCollisionSection(List<PrismTriangle> prisms, Vector3 boxPosition, Vector3 boxHalfExtents)
    {
        var bvh = new ssBVH<PrismTriangle>(new SSTriangleBVHNodeAdaptor(), prisms);
        var section = new SlResourceMeshSection
        {
            Type = 2,
            Roots = bvh.maxDepth + 1
        };
        
        AddNode(bvh.rootBVH);
        _mesh.Sections.Add(section);
        
        return;
        
        void AddNode(ssBVHNode<PrismTriangle>? node)
        {
            if (node == null) return;
            
            var min = new Vector3(node.box.Min.X, node.box.Min.Y, node.box.Min.Z);
            var max = new Vector3(node.box.Max.X, node.box.Max.Y, node.box.Max.Z);
            var center = new Vector4((max + min) / 2.0f, 0.0f);
            var extents = new Vector4(Vector3.Abs(max - min) / 2.0f, 0.0f);
            
            if (node.IsLeaf)
            {
                PrismTriangle triangle = node.gobjects.First();
                
                section.Branches.Add(new SlCollisionResourceBranchNode
                {
                    Center = center,
                    Extents = extents,
                    Flags = triangle.IsWall ? 4 : 1,
                    Leaf = section.Leafs.Count
                });
                
                section.Leafs.Add(new SlCollisionResourceLeafNode
                {
                    Data = new SlResourceMeshDataSingleTriangleFloat
                    {
                        A = triangle.A,
                        B = triangle.B,
                        C = triangle.C,

                        Center = center.AsVector128().AsVector3(),
                        Min = min,
                        Max = max,

                        CollisionMaterialIndex = triangle.IsWall ? WallMaterialIndex : TrackMaterialIndex
                    }
                });
            }
            else
            {
                var branch = new SlCollisionResourceBranchNode
                {
                    Flags = 5,
                    First = section.Branches.Count + 1,
                    Center = center,
                    Extents = extents
                };
                
                var left = node.left;
                var right = node.right;
                if (left.IsLeaf && right != null)
                    (left, right) = (right, left);
                
                section.Branches.Add(branch);
                
                AddNode(left);
                if (right != null)
                {
                    branch.Next = section.Branches.Count;
                    AddNode(right);
                }
            }
        }
    }
        
    private void SetupDefaultMaterials()
    {
        _mesh.Materials.Add(new SlCollisionMaterial
        {
            Name = $"{_path}:default.collisionmaterial",
            Flags = CollisionFlags.Land,
            Type = SurfaceType.Dirt,
        });
    
        _mesh.Materials.Add(new SlCollisionMaterial
        {
            Name = $"{_path}:track_wall.collisionmaterial",
            Flags = CollisionFlags.Wall,
            Type = SurfaceType.Wood,
        });
    }
    
    public class PrismTriangle
    {
        public KclPrism Prism ;
        public Triangle Triangle;

        public Vector3 A;
        public Vector3 B;
        public Vector3 C;
        
        public Vector3 Center;
        public Vector3 Max;
        public Vector3 Min;
        public Vector3 Extents;
        public float Radius;
        public bool IsWall;

        public PrismTriangle(KCLModel model, KclPrism prism)
        {
            Prism = prism;
            Triangle = model.GetTriangle(prism);
            Center = Triangle.GetTriangleCenter() * KartConstants.GameScale;
            
            A = Triangle.Vertices[0] * KartConstants.GameScale;
            B = Triangle.Vertices[1] * KartConstants.GameScale;
            C = Triangle.Vertices[2] * KartConstants.GameScale;
            
            Max = Vector3.Max(A, Vector3.Max(B, C));
            Min = Vector3.Min(A, Vector3.Min(B, C));
            Extents = Vector3.Abs(Max - Min) / 2.0f;
            Radius = ((Max - Min).Length() + 0.001f) / 2.0f;
            
            ushort id = Prism.CollisionFlags;
            int specialFlag = (id >> 8);
            int attributeMaterial = (id & 0xFF);
            int materialIndex = attributeMaterial / 0x20;
            int attributeID = attributeMaterial - (materialIndex * 0x20);
            
            string type = MarioKartAttributeList[attributeID];
            IsWall = type.Contains("Wall");
        }
    };
}