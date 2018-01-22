﻿using UnityEngine;
using System.Collections.Generic;

namespace UnityARInterface
{
    public struct BoundedPlane
    {
        // ARCore reference to the plane
        // Kept to pull vertices containing a more detailed plane polygon
        // than the quad used by ARKit and AREditor
        public GoogleARCore.TrackedPlane trackedPlane;

        public string id;
        public Vector3 center;
        public Vector2 extents;
        public Quaternion rotation;

        // Mesh data
        public List<Vector3> meshVertices;
        public List<Color> meshColors;
        public List<int> meshIndices;
        public void GetBoundaryPolygon(ref List<Vector3> boundaryPolygonPoints)
        {
#if UNITY_EDITOR || UNITY_IOS
            boundaryPolygonPoints = new List<Vector3>(quad);
#elif UNITY_ANDROID
            trackedPlane.GetBoundaryPolygon(boundaryPolygonPoints);
#endif
        }

        public Vector3 normal { get { return rotation * Vector3.up; } }
        public Plane plane { get { return new Plane(normal, center); } }
        public float width
        {
            get { return extents.x; }
            set { extents.x = value; }
        }
        public float height
        {
            get { return extents.y; }
            set { extents.y = value; }
        }

        public Vector3[] quad
        {
            get
            {
                Vector3[] points = new Vector3[4];
                var right = rotation * Vector3.right * extents.x / 2;
                var forward = rotation * Vector3.forward * extents.y / 2;

                points[3] = center + right - forward;
                points[2] = center + right + forward;
                points[1] = center + -right + forward;
                points[0] = center + -right - forward;

                return points;
            }
        }

        public BoundedPlane(string newId, Vector3 newCenter, 
                Quaternion newRotation, Vector2 newExtents)
        {
            id = newId;
            center = newCenter;
            rotation = newRotation;
            extents = newExtents;
            meshVertices = new List<Vector3>();
            meshColors = new List<Color>();
            meshIndices = new List<int>();

            trackedPlane = null;
        }

        public BoundedPlane(string newId, Vector3 newCenter, 
                Quaternion newRotation, Vector2 newExtents, 
                GoogleARCore.TrackedPlane newPlane)
        {
            id = newId;
            center = newCenter;
            rotation = newRotation;
            extents = newExtents;
            meshVertices = new List<Vector3>();
            meshColors = new List<Color>();
            meshIndices = new List<int>();

            trackedPlane = newPlane;
        }
    }
}
