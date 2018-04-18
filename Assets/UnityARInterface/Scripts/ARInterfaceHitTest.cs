using System.Collections.Generic;
using UnityEngine;

namespace UnityARInterface
{
	public abstract partial class ARInterface
	{
		public enum HitTestResultType
		{
			// Result type from intersecting the nearest feature point.
			FeaturePoint,
			
			// Result type from detecting and intersecting a new horizontal plane.
			EstimatedHorizontalPlane,
			
			// Result type from intersecting with an existing plane anchor.
			ExistingPlane,
			
			// Result type from intersecting with an existing plane anchor, taking into account the plane's extent.
			ExistingPlaneUsingExtent,
		}
		
		public struct HitTestResult
		{
			// The type of the hit-test result.
			public HitTestResultType type;
			
			// The distance from the camera to the intersection in meters.
			public double distance;
			
			// The location where hit test the object in Unity world coordinates.
			public Vector3 position;
		}
		
		public abstract List<HitTestResult> HitTest(Vector2 point, HitTestResultType type);
	}
}