using System.Numerics;
using KclLibrary;
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
    
    private string[] MarioKartAttributeList =
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
        
        const bool doDumbShit = true;
        if (doDumbShit)
        {
            foreach (var prism in kclTriangles)
            {
                var section = new SlResourceMeshSection
                {
                    Roots = 1,
                    Type = 2
                };
                
                var triangle = prism.Triangle;
                
                Vector3 a = triangle.Vertices[0] * 0.1f;
                Vector3 b = triangle.Vertices[1] * 0.1f;
                Vector3 c = triangle.Vertices[2] * 0.1f;
                
                Vector3 min = Vector3.Min(a, Vector3.Min(b, c));
                Vector3 max = Vector3.Max(a, Vector3.Max(b, c));
                Vector3 center = (a + b + c) / 3.0f;

                section.Leafs.Add(new SlCollisionResourceLeafNode
                {
                    Data = new SlResourceMeshDataSingleTriangleFloat
                    {
                        A = a,
                        B = b,
                        C = c,

                        Center = center,
                        Min = min,
                        Max = max,

                        CollisionMaterialIndex = 0
                    }
                });
                
                ushort id = prism.Prism.CollisionFlags;
                int specialFlag = (id >> 8);
                int attributeMaterial = (id & 0xFF);
                int materialIndex = attributeMaterial / 0x20;
                int attributeID = attributeMaterial - (materialIndex * 0x20);
                string type = MarioKartAttributeList[attributeID];

                bool isWall = type.Contains("Wall");
                section.Branches.Add(new SlCollisionResourceBranchNode
                {
                    Flags = isWall ? 4 : 1,
                    Leaf = 0,
                    Next = -1,
                    Center = new Vector4(center, 0.0f),
                    Extents = new Vector4(Vector3.Max(Vector3.Abs(max - center), Vector3.Abs(min - center)) + Vector3.One, 0.0f),
                    // Extents = new Vector4((Vector3.Abs(d.Max - d.Min) / 2.0f) + Vector3.One, 0.0f)
                });
                
                if (isWall)
                    section.Leafs[0].Data.CollisionMaterialIndex = 1;
                
                _mesh.Sections.Add(section);
            }
        }
        else
        {
            Vector3 globalMin = _kcl.MinCoordinate;
            Vector3 globalMax = _kcl.MaxCoordinate;
            var boxSize = new Vector3(75.0f, 75.0f, 75.0f) * 20.0f;
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

                    if (TriangleBoxIntersect.TriBoxOverlap(triangle.Triangle, cubePosition + boxHalfSize, boxHalfSize))
                    {
                        usedTriangles.Add(triangle);
                        cubeTriangles.Add(triangle);
                    }
                }

                if (cubeTriangles.Count == 0) continue;

            
                AddCollisionSection(cubeTriangles, (cubePosition * 0.1f), boxHalfSize * 0.1f);

                Console.WriteLine($"[{x}:{y}:{z}]({cubePosition}) - {cubeTriangles.Count} triangles");
            }   
        }
        
        
        return _resource;
    }
    
    private void AddCollisionSection(List<PrismTriangle> prisms, Vector3 boxCenter, Vector3 boxHalfSize)
    {
        var section = new SlResourceMeshSection
        {
            Roots = 2
        };

        var root = new SlCollisionResourceBranchNode
        {
            First = 1,
            Next = prisms.Count,
            Flags = 5,
        };

        Vector3 gMax = new Vector3(float.NegativeInfinity);
        Vector3 gMin = new Vector3(float.PositiveInfinity);
        Vector3 running = Vector3.Zero;
        foreach (PrismTriangle prism in prisms)
        {
            var triangle = prism.Triangle;

            Vector3 a = triangle.Vertices[0] * 0.1f;
            Vector3 b = triangle.Vertices[1] * 0.1f;
            Vector3 c = triangle.Vertices[2] * 0.1f;
            
            running += a + b + c;

            Vector3 min = Vector3.Min(a, Vector3.Min(b, c));
            Vector3 max = Vector3.Max(a, Vector3.Max(b, c));
            Vector3 center = (a + b + c) / 3.0f;

            gMax = Vector3.Max(gMax, max);
            gMin = Vector3.Min(gMin, min);

            section.Leafs.Add(new SlCollisionResourceLeafNode
            {
                Data = new SlResourceMeshDataSingleTriangleFloat
                {
                    A = a,
                    B = b,
                    C = c,

                    Center = center,
                    Min = min,
                    Max = max,

                    CollisionMaterialIndex = 0
                }
            });
        }

        Vector3 cen = running / (prisms.Count * 3);
        root.Center = new Vector4(boxCenter, 0.0f);
        root.Extents = new Vector4(boxHalfSize * 2.0f, 0.0f);
        
        // root.Center = new Vector4(cen, 0.0f);
        // root.Extents = new Vector4(Vector3.Max(Vector3.Abs(gMax - cen), Vector3.Abs(gMin - cen)) + Vector3.One, 0.0f);
        // root.Extents = new Vector4(Vector3.Abs(gMax - gMin) / 2.0f, 0.0f);

        // root.Center = new Vector4(0.5f * (gMin + gMax), 0.0f);
        // root.Extents = new Vector4(0.5f * (gMax - gMin), 0.0f);
        section.Branches.Add(root);

        for (int i = 0; i < section.Leafs.Count; ++i)
        {
            SlResourceMeshDataSingleTriangleFloat d = section.Leafs[i].Data;

            ushort id = prisms[i].Prism.CollisionFlags;
            int specialFlag = (id >> 8);
            int attributeMaterial = (id & 0xFF);
            int materialIndex = attributeMaterial / 0x20;
            int attributeID = attributeMaterial - (materialIndex * 0x20);

            string type = MarioKartAttributeList[attributeID];
            var branch = new SlCollisionResourceBranchNode
            {
                Flags = type.Contains("Wall") ? 4 : 1,
                Leaf = i,
                Next = i + 2,
                Center = new Vector4(d.Center, 0.0f),
                Extents = new Vector4(Vector3.Max(Vector3.Abs(d.Max - d.Center), Vector3.Abs(d.Min - d.Center)) + Vector3.One, 0.0f),
                // Extents = new Vector4((Vector3.Abs(d.Max - d.Min) / 2.0f) + Vector3.One, 0.0f)
            };

            section.Branches.Add(branch);
        }

        section.Branches.Last().Next = -1;

        _mesh.Sections.Add(section);
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
    
    class PrismTriangle(KCLModel model, KclPrism prism)
    {
        public KclPrism Prism = prism;
        public Triangle Triangle = model.GetTriangle(prism);
    };
}