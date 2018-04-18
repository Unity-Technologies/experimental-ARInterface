using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;

namespace UnityARInterface
{
	public partial class ARCoreInterface : ARInterface
	{
		public override List<HitTestResult> HitTest(Vector2 point, HitTestResultType type)
		{
			TrackableHitFlags hf = TrackableHitFlags.None;
			// translate HitTestResultType to TrackableHitFlags
			switch (type)
			{
				case HitTestResultType.FeaturePoint:
					hf = TrackableHitFlags.FeaturePoint | TrackableHitFlags.FeaturePointWithSurfaceNormal;
					break;
				case HitTestResultType.EstimatedHorizontalPlane:
					hf = TrackableHitFlags.PlaneWithinPolygon;
					break;
				case HitTestResultType.ExistingPlane:
					hf = TrackableHitFlags.PlaneWithinInfinity;
					break;
				case HitTestResultType.ExistingPlaneUsingExtent:
					hf = TrackableHitFlags.PlaneWithinBounds;
					break;
			}
			// call native HitTest
			List<HitTestResult> results = new List<HitTestResult>();
			TrackableHit hit;
			if (Frame.Raycast(point.x, point.y, hf, out hit))
			{
				HitTestResult result = new HitTestResult();
				result.distance = hit.Distance;
				result.position = hit.Pose.position;
				result.type = type;
				results.Add(result);
			}
			return results;
		}
	}
}