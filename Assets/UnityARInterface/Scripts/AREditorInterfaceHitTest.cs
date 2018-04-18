using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
	public partial class AREditorInterface : ARInterface
	{
		private Camera m_CachedCamera;
		
		public override List<HitTestResult> HitTest(Vector2 point, HitTestResultType type)
		{
			if (!m_CachedCamera)
			{
				m_CachedCamera = Camera.main;
			}
			Ray ray = m_CachedCamera.ScreenPointToRay(point);
			int mask = 1 << LayerMask.NameToLayer("ARGameObject");
			List<HitTestResult> results = new List<HitTestResult>();
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, float.MaxValue, mask))
			{
				HitTestResult result = new HitTestResult();
				result.position = hit.point;
				result.distance = Vector3.Distance(result.position, m_CachedCamera.transform.position);
				result.type = HitTestResultType.ExistingPlaneUsingExtent;
			}
			return results;
		}
		
	}
}