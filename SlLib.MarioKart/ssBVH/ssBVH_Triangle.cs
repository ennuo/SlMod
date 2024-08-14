using System;
using System.Collections.Generic;
using KclLibrary;
using OpenTK;

using SimpleScene;
using SlLib.MarioKart;


namespace SimpleScene.Util.ssBVH
{
    public class SSTriangleBVHNodeAdaptor : SSBVHNodeAdaptor<CollisionImporter.PrismTriangle>
    {
        protected ssBVH<CollisionImporter.PrismTriangle> _bvh;
        protected Dictionary <CollisionImporter.PrismTriangle, ssBVHNode<CollisionImporter.PrismTriangle>> _sphereToLeafMap 
            = new Dictionary <CollisionImporter.PrismTriangle, ssBVHNode<CollisionImporter.PrismTriangle>>();
        public ssBVH<CollisionImporter.PrismTriangle> BVH { get { return _bvh; } }

        public void setBVH(ssBVH<CollisionImporter.PrismTriangle> bvh)
        {
            _bvh = bvh;
        }

        public Vector3 objectpos(CollisionImporter.PrismTriangle triangle)
        {
            System.Numerics.Vector3 center = triangle.Center;
            return new Vector3(center.X, center.Y, center.Z);
        }

        public float radius(CollisionImporter.PrismTriangle triangle)
        {
            return triangle.Radius;
        }

        public void checkMap(CollisionImporter.PrismTriangle sphere) 
        {
            if (!_sphereToLeafMap.ContainsKey (sphere)) {
                throw new Exception("missing map for a shuffled child");
            }
        }

        public void unmapObject(CollisionImporter.PrismTriangle sphere) 
        {
            _sphereToLeafMap.Remove(sphere);
        }

        public void mapObjectToBVHLeaf(CollisionImporter.PrismTriangle sphere, ssBVHNode<CollisionImporter.PrismTriangle> leaf) 
        {  
            _sphereToLeafMap[sphere] = leaf;
        }

        public ssBVHNode<CollisionImporter.PrismTriangle> getLeaf(CollisionImporter.PrismTriangle sphere)
        {
            return _sphereToLeafMap [sphere];
        }
    }

    public class SSTriangleBVH : ssBVH<CollisionImporter.PrismTriangle>
    {
        public SSTriangleBVH(int maxSpheresPerLeaf=1)
            : base(new SSTriangleBVHNodeAdaptor(),
                new List<CollisionImporter.PrismTriangle>(),
                maxSpheresPerLeaf)
        {
        }
    }
}