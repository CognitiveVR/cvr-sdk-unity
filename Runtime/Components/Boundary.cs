using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Boundary")]
    public class Boundary : AnalyticsComponentBase
    {
#if ((C3D_OCULUS || C3D_DEFAULT || C3D_VIVEWAVE || C3D_PICOXR) && !UNITY_EDITOR) || C3D_STEAMVR2
        /// <summary>
        /// Track whether the boundary has been initialized in this session
        /// </summary>
        private static bool boundaryInitializedThisSession = false;

        /// <summary>
        /// The previous list of coordinates (local to tracking space) describing the boundary <br/>
        /// Used for comparison to determine if the boundary changed
        /// </summary>
        Vector3[] previousBoundaryPoints = new Vector3[0];

        /// <summary>
        /// The current (this frame) list of coordinates (local to tracking space) describing the boundary
        /// </summary>
        Vector3[] currentBoundaryPoints = new Vector3[0];

        /// <summary>
        /// The previous position of the tracking space; used for comparison to detect "moved enough"
        /// </summary>
        Vector3 lastRecordedTrackingSpacePosition = new Vector3();
        
        /// <summary>
        /// The previous rotation of the tracking space; used for comparison to detect "rotated enough"
        /// </summary>
        Quaternion previousTrackingSpaceRotation = Quaternion.identity;

        /// <summary>
        /// A 10% overflow buffer added to string builder
        /// </summary>
        private readonly float NUM_BOUNDARY_POINTS_GRACE_FOR_STRINGBUILDER = 0.1f;

        /// <summary>
        /// The threshold for minimum position (in metres) change to re-record boundary points
        /// </summary>
        private readonly float TRACKING_SPACE_POSITION_THRESHOLD_IN_METRES = 0.01f;

        /// <summary>
        /// The threshold for minimum rotation (in degrees) change to re-record boundary points
        /// </summary>
        private readonly float TRACKING_SPACE_ROTATION_THRESHOLD_IN_DEGREES = 5f;

        private const float INITIALIZATION_DELAY_SECONDS = 1;

        // Start is called before the first frame update
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            boundaryInitializedThisSession = false; // Reset for new session
            StartCoroutine(InitializeBoundaryRecordWithDelay());
        }

        void OnLevelLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool didChangeSceneId)
        {
            if (didChangeSceneId && Cognitive3D_Manager.TrackingScene != null)
            {
                StartCoroutine(InitializeBoundaryRecordWithDelay());
            }
        }

        private IEnumerator InitializeBoundaryRecordWithDelay()
        {
            // Prevent race condition: claim initialization immediately before delay
            // If multiple coroutines start (e.g., from rapid session start + level load),
            // only the first one should register event handlers
            bool shouldInitialize = !boundaryInitializedThisSession;
            if (shouldInitialize)
            {
                boundaryInitializedThisSession = true;
            }

            yield return new WaitForSeconds(INITIALIZATION_DELAY_SECONDS);

            // Always record boundary shape and tracking space on scene load (including first scene)
            currentBoundaryPoints = BoundaryUtil.GetCurrentBoundaryPoints();
            previousBoundaryPoints = currentBoundaryPoints;

            // Only initialize once per session (on first scene)
            if (shouldInitialize)
            {
                // Always initialize the string builder, even if there are no boundary points
                int numPoints = (currentBoundaryPoints != null) ? currentBoundaryPoints.Length : 4; // Default to 4 if no boundary
                CoreInterface.InitializeBoundary(numPoints + (int)Mathf.Ceil(NUM_BOUNDARY_POINTS_GRACE_FOR_STRINGBUILDER * numPoints));

                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
                Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;
                Cognitive3D_Manager.OnLevelLoaded += OnLevelLoaded;
            }

            if (currentBoundaryPoints != null)
            {
                CoreInterface.RecordBoundaryShape(currentBoundaryPoints, Util.Timestamp(Time.frameCount));
            }

            // Record tracking space position and rotation
            if (BoundaryUtil.TryGetTrackingSpaceTransform(out var customTransform))
            {
                CoreInterface.RecordTrackingSpaceTransform(customTransform, Util.Timestamp(Time.frameCount));
                lastRecordedTrackingSpacePosition = customTransform.pos;
                previousTrackingSpaceRotation = customTransform.rot;
            }
        }

        /// <summary>
        /// Called 10 times per second; 10Hz
        /// </summary>
        private void Cognitive3D_Manager_OnTick()
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()  
            if (isActiveAndEnabled)
            {
                if (BoundaryUtil.TryGetTrackingSpaceTransform(out var customTransform))
                {
                    if (Vector3.SqrMagnitude(customTransform.pos - lastRecordedTrackingSpacePosition) > TRACKING_SPACE_POSITION_THRESHOLD_IN_METRES * TRACKING_SPACE_POSITION_THRESHOLD_IN_METRES ||
                        Math.Abs(Vector3.Angle(previousTrackingSpaceRotation.eulerAngles, customTransform.rot.eulerAngles)) > TRACKING_SPACE_ROTATION_THRESHOLD_IN_DEGREES)
                    {
                        CoreInterface.RecordTrackingSpaceTransform(customTransform, Util.Timestamp(Time.frameCount));
                        lastRecordedTrackingSpacePosition = customTransform.pos;
                        previousTrackingSpaceRotation = customTransform.rot;
                    }
                }

                currentBoundaryPoints = BoundaryUtil.GetCurrentBoundaryPoints();

                if (currentBoundaryPoints != null)
                {
                    if (BoundaryUtil.HasBoundaryChanged(previousBoundaryPoints, currentBoundaryPoints))
                    {         
                        previousBoundaryPoints = currentBoundaryPoints;
                        CoreInterface.RecordBoundaryShape(currentBoundaryPoints, Util.Timestamp(Time.frameCount));
                    }
                }
            }
        }

        internal static void RecordRecenterBoundary()
        {
            //TODO
            // FIX TRACKING AND BOUNDARIES AGAIN
            // If recenter, tracking space gets xz pos of camera, and y rotation of camera
            BoundaryUtil.TryGetTrackingSpaceTransform(out var trackingSpaceTransform);
            CustomTransform recenteredTransform = new CustomTransform(
                new Vector3(GameplayReferences.HMD.position.x, trackingSpaceTransform.pos.y, GameplayReferences.HMD.position.z),
                Quaternion.Euler(trackingSpaceTransform.rot.x, GameplayReferences.HMD.rotation.y, trackingSpaceTransform.rot.z));
            CoreInterface.RecordTrackingSpaceTransform(recenteredTransform, Util.Timestamp(Time.frameCount));
            CoreInterface.RecordBoundaryShape(BoundaryUtil.GetCurrentBoundaryPoints(), Util.Timestamp(Time.frameCount));
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_OnTick;
            boundaryInitializedThisSession = false; // Reset for next session
        }
#endif

#region Inspector Utils
        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_DEFAULT || C3D_VIVEWAVE || C3D_PICOXR || C3D_STEAMVR2
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS || C3D_DEFAULT || C3D_VIVEWAVE || C3D_PICOXR || C3D_STEAMVR2
            return "Records player boundary";
#else
            return "Current platform does not support this component. This component is only supported for Meta, HTC Wave, PicoXR, SteamVR and OpenXR.";
#endif
        }
#endregion
    }
}
