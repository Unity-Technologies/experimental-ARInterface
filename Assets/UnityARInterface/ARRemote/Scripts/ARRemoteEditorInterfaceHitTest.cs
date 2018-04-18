using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
namespace UnityARInterface
{
	public partial class ARRemoteEditorInterface : ARInterface
	{
		public override List<HitTestResult> HitTest(Vector2 point, HitTestResultType type)
		{
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
#endif