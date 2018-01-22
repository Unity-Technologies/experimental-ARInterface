using UnityEngine;
using System.Collections.Generic;

namespace UnityARInterface
{
    public class ARPlaneVisualizer : ARBase
    {

        [SerializeField]
        private GameObject m_PlanePrefab;

        [SerializeField]
        private bool m_ClearPlanesOnDisable;

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
            ClearPlanes();
        }

        protected virtual void CreateOrUpdateGameObject(BoundedPlane plane)
        {
            GameObject go;
            if (!m_Planes.TryGetValue(plane.id, out go))
            {
                go = Instantiate(m_PlanePrefab, GetRoot());

                m_Planes.Add(plane.id, go);

                go.GetComponent<ARGrid>().Init(m_Planes.Count, m_PlaneLayer);
            }

            go.GetComponent<ARGrid>().UpdateMeshIfNeeded(plane);
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

        public virtual void ClearPlanes()
        {
            if (m_ClearPlanesOnDisable)
            {
                foreach (KeyValuePair<string, GameObject> plane in m_Planes)
                {
                    Destroy(plane.Value);
                    m_Planes.Remove(plane.Key);
                }
            }
        }
    }
}
