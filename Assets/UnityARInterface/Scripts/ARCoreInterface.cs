using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using System.Collections;
using GoogleARCoreInternal;

namespace UnityARInterface
{
	public class ARCoreInterface : ARInterface
	{
		private List<TrackedPlane> m_TrackedPlaneBuffer = new List<TrackedPlane>();
		private ScreenOrientation m_CachedScreenOrientation;
		private Dictionary<TrackedPlane, BoundedPlane> m_TrackedPlanes = new Dictionary<TrackedPlane, BoundedPlane>();
        private SessionManager m_SessionManager;
        private ARCoreSessionConfig m_ARCoreSessionConfig;

        private Matrix4x4 m_DisplayTransform = Matrix4x4.identity;

        public override bool IsSupported
        {
            get
            {
                if (m_SessionManager == null)
                    m_SessionManager = SessionManager.CreateSession();

                if (m_ARCoreSessionConfig == null)
                    m_ARCoreSessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

                return m_SessionManager.CheckSupported((m_ARCoreSessionConfig));
            }
        }

		public override IEnumerator StartService(Settings settings)
		{
            if(m_ARCoreSessionConfig == null)
                m_ARCoreSessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

            m_ARCoreSessionConfig.EnableLightEstimation = settings.enableLightEstimation;
            m_ARCoreSessionConfig.EnablePlaneFinding = settings.enablePlaneDetection;
            //Do we want to match framerate to the camera?
            m_ARCoreSessionConfig.MatchCameraFramerate = false;

            if (m_SessionManager == null)
			{
                m_SessionManager = SessionManager.CreateSession();
                if (!m_SessionManager.CheckSupported((m_ARCoreSessionConfig)))
                    yield break;
                
                Session.Initialize(m_SessionManager);

                if (Session.ConnectionState != SessionConnectionState.Uninitialized)
                {
                    ARDebug.LogError("Could not create an ARCore session.  The current Unity Editor may not support this " +
                        "version of ARCore.");
                    yield break;
                }
			}

            //This is an async task
            var task = Connect(m_ARCoreSessionConfig);
			yield return new WaitUntil (() => task.IsComplete);
			IsRunning = task.Result == SessionConnectionState.Connected;
		}

        /// <summary>
        /// Connects an ARSession.  Note that if user permissions are needed they will be requested and thus this is an
        /// asynchronous method.
        /// </summary>
        /// <param name="sessionConfig">The session configuration.</param>
        /// <returns>An {@link AsyncTask<T>} that completes when the connection has been made or failed. </returns>
        public AsyncTask<SessionConnectionState> Connect(ARCoreSessionConfig sessionConfig)
        {
            const string androidCameraPermissionName = "android.permission.CAMERA";

            if (m_SessionManager == null)
            {
                ARDebug.LogError("Cannot connect because ARCoreSession failed to initialize.");
                return new AsyncTask<SessionConnectionState>(SessionConnectionState.Uninitialized);
            }

            if (sessionConfig == null)
            {
                ARDebug.LogError("Unable to connect ARSession session due to missing ARSessionConfig.");
                m_SessionManager.ConnectionState = SessionConnectionState.MissingConfiguration;
                return new AsyncTask<SessionConnectionState>(Session.ConnectionState);
            }

            // We have already connected at least once.
            if (Session.ConnectionState != SessionConnectionState.Uninitialized)
            {
                ARDebug.LogError("Multiple attempts to connect to the ARSession.  Note that the ARSession connection " +
                    "spans the lifetime of the application and cannot be reconfigured.  This will change in future " +
                    "versions of ARCore.");
                return new AsyncTask<SessionConnectionState>(Session.ConnectionState);
            }

            // Create an asynchronous task for the potential permissions flow and service connection.
            Action<SessionConnectionState> onTaskComplete;
            var returnTask = new AsyncTask<SessionConnectionState>(out onTaskComplete);
            returnTask.ThenAction((connectionState) =>
            {
                m_SessionManager.ConnectionState = connectionState;
            });

            // Attempt service connection immediately if permissions are granted.
            if (AndroidPermissionsManager.IsPermissionGranted(androidCameraPermissionName))
            {
                _ResumeSession(sessionConfig, onTaskComplete);
                return returnTask;
            }

            // Request needed permissions and attempt service connection if granted.
            AndroidPermissionsManager.RequestPermission(androidCameraPermissionName).ThenAction((requestResult) =>
            {
                if (requestResult.IsAllGranted)
                {
                    _ResumeSession(sessionConfig, onTaskComplete);
                }
                else
                {
                    ARDebug.LogError("ARCore connection failed because a needed permission was rejected.");
                    onTaskComplete(SessionConnectionState.UserRejectedNeededPermission);
                }
            });

            return returnTask;
        }

        /// <summary>
        /// Connects to the ARCore service.
        /// </summary>
        /// <param name="sessionConfig">The session configuration to connect with.</param>
        /// <param name="onComplete">A callback for when the result of the connection attempt is known.</param>
        private void _ResumeSession(ARCoreSessionConfig sessionConfig, Action<SessionConnectionState> onComplete)
        {
            if (!m_SessionManager.CheckSupported(sessionConfig))
            {
                ARDebug.LogError("The requested ARCore session configuration is not supported.");
                onComplete(SessionConnectionState.InvalidConfiguration);
                return;
            }

            if (!m_SessionManager.SetConfiguration(sessionConfig))
            {
                ARDebug.LogError("ARCore connection failed because the current configuration is not supported.");
                onComplete(SessionConnectionState.InvalidConfiguration);
                return;
            }

            Frame.Initialize(m_SessionManager.FrameManager);

            // ArSession_resume needs to be called in the UI thread due to b/69682628.
            AsyncTask.PerformActionInUIThread(() =>
            {
                if (!m_SessionManager.Resume())
                {
                    onComplete(SessionConnectionState.ConnectToServiceFailed);
                }
                else
                {
                    onComplete(SessionConnectionState.Connected);
                }
            });
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
            if (m_SessionManager == null)
            {
                return;
            }

            AsyncTask.OnUpdate();

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
