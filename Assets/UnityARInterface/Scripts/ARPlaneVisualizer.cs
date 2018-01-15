using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
    public class ARPlaneVisualizer : ARBase
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

        [SerializeField]
        private GameObject m_PlanePrefab;

        [SerializeField]
        private int m_PlaneLayer;

        public int planeLayer { get { return m_PlaneLayer; } }

        private Dictionary<string, GameObject> m_Planes = new Dictionary<string, GameObject>();

        void OnEnable()
        {
            m_PlaneLayer = LayerMask.NameToLayer ("ARGameObject");
            ARInterface.planeAdded += PlaneAddedHandler;
            ARInterface.planeUpdated += PlaneUpdatedHandler;
            ARInterface.planeRemoved += PlaneRemovedHandler;
        }

        void OnDisable()
        {
            ARInterface.planeAdded -= PlaneAddedHandler;
            ARInterface.planeUpdated -= PlaneUpdatedHandler;
            ARInterface.planeRemoved -= PlaneRemovedHandler;
        }

        protected virtual void CreateOrUpdateGameObject(BoundedPlane plane)
        {
            GameObject go;
            if (!m_Planes.TryGetValue(plane.id, out go))
            {
                go = Instantiate(m_PlanePrefab, GetRoot());


                // Make sure we can pick them later
                foreach (var collider in go.GetComponentsInChildren<Collider>())
                    collider.gameObject.layer = m_PlaneLayer;

                m_Planes.Add(plane.id, go);

                Renderer rend = go.GetComponentInChildren<Renderer>();
                rend.material.SetColor("_GridColor", k_PlaneColors[m_Planes.Count % k_PlaneColors.Length]);
                rend.material.SetFloat("_UvRotation", Random.Range(0.0f, 360.0f));

                // Is setting the mesh to null really necessary? Wouldn't there be a more optimal method?
                Mesh mesh = go.GetComponentInChildren<MeshFilter>().mesh;
                go.GetComponentInChildren<MeshCollider>().sharedMesh = null;
                go.GetComponentInChildren<MeshCollider>().sharedMesh = mesh;
            }

            UpdateMeshIfNeeded(go, plane);
        }

        protected virtual void PlaneAddedHandler(BoundedPlane plane)
        {
            if (m_PlanePrefab)
                CreateOrUpdateGameObject(plane);
        }

        protected virtual void PlaneUpdatedHandler(BoundedPlane plane)
        {
            if (m_PlanePrefab)
                CreateOrUpdateGameObject(plane);
        }

        protected virtual void PlaneRemovedHandler(BoundedPlane plane)
        {
            GameObject go;
            if (m_Planes.TryGetValue(plane.id, out go))
            {
                Destroy(go);
                m_Planes.Remove(plane.id);
            }
        }

        private void UpdateMeshIfNeeded(GameObject go, BoundedPlane plane)
        {
#if UNITY_EDITOR || UNITY_IOS
            plane.meshVertices = new List<Vector3>(plane.quad);
#elif UNITY_ANDROID
            plane.trackedPlane.GetBoundaryPolygon(plane.meshVertices);
#endif

            if (_AreVerticesListsEqual(plane.previousFrameMeshVertices, plane.meshVertices))
            {
                return;
            }

            plane.previousFrameMeshVertices.Clear();
            plane.previousFrameMeshVertices.AddRange(plane.meshVertices);

            //m_PlaneCenter = m_TrackedPlane.Position;

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

            Mesh mesh = go.GetComponentInChildren<MeshFilter>().mesh;
            mesh.Clear();
            mesh.SetVertices(plane.meshVertices);
            mesh.SetIndices(plane.meshIndices.ToArray(), MeshTopology.Triangles, 0);
            mesh.SetColors(plane.meshColors);

            // Is setting the mesh to null really necessary? Wouldn't there be a more optimal method?
            MeshCollider collider = go.GetComponentInChildren<MeshCollider>();
            collider.sharedMesh = null;
            collider.sharedMesh = mesh;
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
