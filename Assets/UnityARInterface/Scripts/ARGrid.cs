using UnityEngine;
using System.Collections.Generic;

namespace UnityARInterface
{
	public class ARGrid : MonoBehaviour 
	{
        private static readonly Color[] k_PlaneColors = new Color[]
        {
            new Color(1.0f, 1.0f, 1.0f),
            new Color(0.956f, 0.262f, 0.211f),
            new Color(0.913f, 0.117f, 0.388f),
            new Color(0.611f, 0.152f, 0.654f),
            new Color(0.403f, 0.227f, 0.717f),
            new Color(0.247f, 0.317f, 0.709f),
            new Color(0.129f, 0.588f, 0.952f),
            new Color(0.011f, 0.662f, 0.956f),
            new Color(0f, 0.737f, 0.831f),
            new Color(0f, 0.588f, 0.533f),
            new Color(0.298f, 0.686f, 0.313f),
            new Color(0.545f, 0.764f, 0.290f),
            new Color(0.803f, 0.862f, 0.223f),
            new Color(1.0f, 0.921f, 0.231f),
            new Color(1.0f, 0.756f, 0.027f)
        };

        Mesh m_Mesh;
        MeshCollider m_MeshCollider;
        Renderer m_Renderer;

        public void Init(int planesCount, int planeLayer)
        {
            m_Mesh = GetComponentInChildren<MeshFilter>().mesh;

            m_Renderer = GetComponentInChildren<Renderer>();
            m_Renderer.material.SetColor("_GridColor", k_PlaneColors[planesCount % k_PlaneColors.Length]);
            m_Renderer.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));

            m_MeshCollider = GetComponentInChildren<MeshCollider>();
            m_MeshCollider.gameObject.layer = planeLayer;
        }

        public void UpdateMeshIfNeeded(BoundedPlane plane)
        {
            plane.GetBoundaryPolygon(ref plane.meshVertices);

            if (_AreVerticesListsEqual(new List<Vector3>(m_Mesh.vertices), plane.meshVertices))
            {
                return;
            }

            int planePolygonCount = plane.meshVertices.Count;

            // The following code converts a polygon to a mesh with two polygons, inner
            // polygon renders with 100% opacity and fade out to outter polygon with opacity 0%, as shown below.
            // The indices shown in the diagram are used in comments below.
            // _______________     0_______________1
            // |             |      |4___________5|
            // |             |      | |         | |
            // |             | =>   | |         | |
            // |             |      | |         | |
            // |             |      |7-----------6|
            // ---------------     3---------------2
            plane.meshColors.Clear();

            // Fill transparent color to vertices 0 to 3.
            for (int i = 0; i < planePolygonCount; ++i)
            {
                plane.meshColors.Add(Color.clear);
            }

            // Feather distance 0.2 meters.
            const float featherLength = 0.2f;

            // Feather scale over the distance between plane center and vertices.
            const float featherScale = 0.2f;

            // Add vertex 4 to 7.
            for (int i = 0; i < planePolygonCount; ++i)
            {
                Vector3 v = plane.meshVertices[i];

                // Vector from plane center to current point
                Vector3 d = v - plane.center;

                float scale = 1.0f - Mathf.Min(featherLength / d.magnitude, featherScale);
                plane.meshVertices.Add((scale * d) + plane.center);

                plane.meshColors.Add(Color.white);
            }

            plane.meshIndices.Clear();
            int firstOuterVertex = 0;
            int firstInnerVertex = planePolygonCount;

            // Generate triangle (4, 5, 6) and (4, 6, 7).
            for (int i = 0; i < planePolygonCount - 2; ++i)
            {
                plane.meshIndices.Add(firstInnerVertex);
                plane.meshIndices.Add(firstInnerVertex + i + 1);
                plane.meshIndices.Add(firstInnerVertex + i + 2);
            }

            // Generate triangle (0, 1, 4), (4, 1, 5), (5, 1, 2), (5, 2, 6), (6, 2, 3), (6, 3, 7)
            // (7, 3, 0), (7, 0, 4)
            for (int i = 0; i < planePolygonCount; ++i)
            {
                int outerVertex1 = firstOuterVertex + i;
                int outerVertex2 = firstOuterVertex + ((i + 1) % planePolygonCount);
                int innerVertex1 = firstInnerVertex + i;
                int innerVertex2 = firstInnerVertex + ((i + 1) % planePolygonCount);

                plane.meshIndices.Add(outerVertex1);
                plane.meshIndices.Add(outerVertex2);
                plane.meshIndices.Add(innerVertex1);

                plane.meshIndices.Add(innerVertex1);
                plane.meshIndices.Add(outerVertex2);
                plane.meshIndices.Add(innerVertex2);
            }

            m_Mesh.Clear();
            m_Mesh.SetVertices(plane.meshVertices);
            m_Mesh.SetIndices(plane.meshIndices.ToArray(), MeshTopology.Triangles, 0);
            m_Mesh.SetColors(plane.meshColors);

            m_MeshCollider.sharedMesh = m_Mesh;
        }

        private bool _AreVerticesListsEqual(List<Vector3> firstList, List<Vector3> secondList)
        {
            if (firstList.Count != secondList.Count)
            {
                return false;
            }

            for (int i = 0; i < firstList.Count; i++)
            {
                if (firstList[i] != secondList[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}