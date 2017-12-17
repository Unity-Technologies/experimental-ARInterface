using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using System.Collections;

namespace UnityARInterface
{
	public class ARCoreInterface : ARInterface
	{
		private List<TrackedPlane> m_TrackedPlaneBuffer = new List<TrackedPlane>();
		private ScreenOrientation m_CachedScreenOrientation;
		private Dictionary<TrackedPlane, BoundedPlane> m_TrackedPlanes = new Dictionary<TrackedPlane, BoundedPlane>();
		private ARCoreSession m_Session;
		private Matrix4x4 m_DisplayTransform = Matrix4x4.identity;

		public override IEnumerator StartService(Settings settings)
		{
			if (m_Session == null)
			{
				var sessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

				sessionConfig.EnableLightEstimation = settings.enableLightEstimation;
				sessionConfig.EnablePlaneFinding = settings.enablePlaneDetection;
				//Do we want to match framerate to the camera?
				sessionConfig.MatchCameraFramerate = false;

				var gameObject = new GameObject("Session Manager");

				// Deactivate the GameObject before adding the SessionComponent
				// or else the Awake method will be called before we have set
				// the session config.
				gameObject.SetActive(false);
				m_Session = gameObject.AddComponent<ARCoreSession>();
				m_Session.ConnectOnAwake = false;
				m_Session.SessionConfig = sessionConfig;

				gameObject.SetActive(true);
			}
			//This is an async task
			var task = m_Session.Connect();
			yield return new WaitUntil (() => task.IsComplete);
			IsRunning = task.Result == SessionConnectionState.Connected;
		}

		public override void StopService()
		{
            Frame.Destroy();
            Session.Destroy();
			IsRunning = false;
			return;
		}

		public override bool TryGetUnscaledPose(ref Pose pose)
		{
			if (Frame.TrackingState != TrackingState.Tracking)
				return false;

			pose = Frame.Pose;
			return true;
		}

		public override bool TryGetCameraImage(ref CameraImage cameraImage)
		{
			if (Frame.TrackingState != TrackingState.Tracking)
				return false;

			//return false;
			//TODO:
			throw new NotImplementedException("TryGetCameraImage is not yet implemented for ARCore");
		}

		public override bool TryGetPointCloud(ref PointCloud pointCloud)
		{
			//Return false if not tracking
			if (Frame.TrackingState != TrackingState.Tracking)
				return false;

			//Check if points are available
			if (Frame.PointCloud.PointCount == 0)
				return false;

			if (pointCloud.points == null)
				pointCloud.points = new List<Vector3>();

			pointCloud.points.Clear();

			// Fill in the data to draw the point cloud.
			for (int i = 0; i < Frame.PointCloud.PointCount; i++)
			{
				pointCloud.points.Add(Frame.PointCloud.GetPoint(i));
			}

			return true;
		}

		public override LightEstimate GetLightEstimate()
		{
			if (Session.ConnectionState == SessionConnectionState.Connected && Frame.LightEstimate.State == LightEstimateState.Valid)
			{
				return new LightEstimate()
				{
					capabilities = LightEstimateCapabilities.AmbientIntensity,
					ambientIntensity = Frame.LightEstimate.PixelIntensity
				};
			}
			else
			{
				// Zero initialized means capabilities will be None
				return new LightEstimate();
			}
		}

		public override Matrix4x4 GetDisplayTransform()
		{
			return m_DisplayTransform;
		}

		private void CalculateDisplayTransform()
		{
			var cosTheta = 1f;
			var sinTheta = 0f;

			switch (Screen.orientation)
			{
			case ScreenOrientation.Portrait:
				cosTheta = 0f;
				sinTheta = -1f;
				break;
			case ScreenOrientation.PortraitUpsideDown:
				cosTheta = 0f;
				sinTheta = 1f;
				break;
			case ScreenOrientation.LandscapeLeft:
				cosTheta = 1f;
				sinTheta = 0f;
				break;
			case ScreenOrientation.LandscapeRight:
				cosTheta = -1f;
				sinTheta = 0f;
				break;
			}

			m_DisplayTransform.m00 = cosTheta;
			m_DisplayTransform.m01 = sinTheta;
			m_DisplayTransform.m10 = sinTheta;
			m_DisplayTransform.m11 = -cosTheta;
		}

		public override void SetupCamera(Camera camera)
		{
			ARCoreBackgroundRenderer arCoreBackgroundRenderer = camera.gameObject.AddComponent<ARCoreBackgroundRenderer>();
			//Disable the background and re-enable it to trigger the ARBackground setup
			arCoreBackgroundRenderer.enabled = false;
			Material backgroundMaterial = Resources.Load("ARBackground", typeof(Material)) as Material;
			arCoreBackgroundRenderer.BackgroundMaterial = backgroundMaterial;
			arCoreBackgroundRenderer.enabled = true;
		}

		public override void UpdateCamera(Camera camera)
		{
			if (Screen.orientation == m_CachedScreenOrientation)
				return;

			CalculateDisplayTransform ();
			m_CachedScreenOrientation = Screen.orientation;

			//ARCoreBackgroundRenderer will take care of 
			//setting the projection matrix for the camera
		}

		public override void Update()
		{
			if (Frame.TrackingState != TrackingState.Tracking)
				return;

			//This is not efficient, as it updates planes even if they didnt change
			Frame.GetPlanes(m_TrackedPlaneBuffer, TrackableQueryFilter.All);

			foreach (var trackedPlane in m_TrackedPlaneBuffer)
			{
				BoundedPlane boundedPlane;
				if (m_TrackedPlanes.TryGetValue(trackedPlane, out boundedPlane))
				{
					if (trackedPlane.SubsumedBy == null)
					{
						OnPlaneUpdated(boundedPlane);
					}
					else
					{
						OnPlaneRemoved(boundedPlane);
						m_TrackedPlanes.Remove(trackedPlane);
					}
				}
				else
				{
					boundedPlane = new BoundedPlane()
					{
						id = Guid.NewGuid().ToString(),
						center = trackedPlane.Position,
						rotation = trackedPlane.Rotation,
						extents = new Vector2(trackedPlane.ExtentX, trackedPlane.ExtentZ)
					};

					m_TrackedPlanes.Add(trackedPlane, boundedPlane);
					OnPlaneAdded(boundedPlane);
				}
			}

			// Check for planes that were removed from the tracked plane list
			List<TrackedPlane> planesToRemove = new List<TrackedPlane>();
			foreach (var kvp in m_TrackedPlanes)
			{
				var trackedPlane = kvp.Key;

				if (!m_TrackedPlaneBuffer.Exists(x => x == trackedPlane))
				{
					OnPlaneRemoved(kvp.Value);

					// Add to list here to avoid mutating the dictionary
					// while iterating over it.
					planesToRemove.Add(trackedPlane);
				}
			}

			foreach (var plane in planesToRemove)
			{
				m_TrackedPlanes.Remove(plane);
			}
		}
	}
}
