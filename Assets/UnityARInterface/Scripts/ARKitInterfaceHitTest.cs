using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;

namespace UnityARInterface
{
	public partial class ARKitInterface : ARInterface
	{
		public override List<HitTestResult> HitTest(Vector2 point, HitTestResultType type)
		{
			ARHitTestResultType rt = ARHitTestResultType.ARHitTestResultTypeFeaturePoint;
			// translate HitTestResultType to ARHitTestResultType
			switch (type)
			{
				case HitTestResultType.FeaturePoint:
					rt = ARHitTestResultType.ARHitTestResultTypeFeaturePoint;
					break;
				case HitTestResultType.EstimatedHorizontalPlane:
					rt = ARHitTestResultType.ARHitTestResultTypeHorizontalPlane;
					break;
				case HitTestResultType.ExistingPlane:
					rt = ARHitTestResultType.ARHitTestResultTypeExistingPlane;
					break;
				case HitTestResultType.ExistingPlaneUsingExtent:
					rt = ARHitTestResultType.ARHitTestResultTypeExistingPlaneUsingExtent;
					break;
			}
			// call native HitTest
			ARPoint ap = new ARPoint {x = point.x, y = point.y};
			List<ARHitTestResult> hitTestResults = nativeInterface.HitTest(ap, rt);
			List<HitTestResult> results = new List<HitTestResult>();
			foreach (ARHitTestResult hitTestResult in hitTestResults)
			{
				HitTestResult result = new HitTestResult();
				result.distance = hitTestResult.distance;
				result.position = UnityARMatrixOps.GetPosition(hitTestResult.worldTransform);
				result.type = type;
				results.Add(result);
			}
			return results;
		}
	}
}