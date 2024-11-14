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
#if (C3D_OCULUS || C3D_DEFAULT) && UNITY_ANDROID && !UNITY_EDITOR
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
        /// A reference to the tracking space of the player's rig
        /// </summary>
        static Transform trackingSpace = null;

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
            StartCoroutine(InitializeBoundaryRecordWithDelay());
        }

        private IEnumerator InitializeBoundaryRecordWithDelay()
        {
            yield return new WaitForSeconds(INITIALIZATION_DELAY_SECONDS);

            // The rest of your original OnSessionBegin code
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;

            // Get initial values of boundary and tracking space
            currentBoundaryPoints = BoundaryUtil.GetCurrentBoundaryPoints();
            previousBoundaryPoints = currentBoundaryPoints; // since there is no "previous"

            // Initialize the string builder to an appropriate size based on boundary points
            if (currentBoundaryPoints != null)
            {
                CoreInterface.InitializeBoundary(currentBoundaryPoints.Length + (int)Mathf.Ceil(NUM_BOUNDARY_POINTS_GRACE_FOR_STRINGBUILDER * currentBoundaryPoints.Length));
                // Record initial boundary shape
                CoreInterface.RecordBoundaryShape(currentBoundaryPoints, Util.Timestamp(Time.frameCount));
            }

            // Record initial tracking space position and rotation
            trackingSpace = Cognitive3D_Manager.Instance.trackingSpace;
            if (trackingSpace)
            {
                CustomTransform customTransform = new CustomTransform(trackingSpace.position, trackingSpace.rotation);
                CoreInterface.RecordTrackingSpaceTransform(customTransform, Util.Timestamp(Time.frameCount));
                lastRecordedTrackingSpacePosition = trackingSpace.position;
                previousTrackingSpaceRotation = trackingSpace.rotation;
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
                trackingSpace = Cognitive3D_Manager.Instance?.trackingSpace;

                if (trackingSpace != null)
                {
                    if (Vector3.SqrMagnitude(trackingSpace.position - lastRecordedTrackingSpacePosition) > TRACKING_SPACE_POSITION_THRESHOLD_IN_METRES * TRACKING_SPACE_POSITION_THRESHOLD_IN_METRES
                        || Math.Abs(Vector3.Angle(previousTrackingSpaceRotation.eulerAngles, trackingSpace.rotation.eulerAngles)) > TRACKING_SPACE_ROTATION_THRESHOLD_IN_DEGREES) // if tracking space moved enough
                    {                     
                        CustomTransform customTransform = new CustomTransform(trackingSpace.position, trackingSpace.rotation);
                        CoreInterface.RecordTrackingSpaceTransform(customTransform, Util.Timestamp(Time.frameCount));
                        
                        lastRecordedTrackingSpacePosition = trackingSpace.position;
                        previousTrackingSpaceRotation = trackingSpace.rotation;
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
            CustomTransform recenteredTransform = new CustomTransform(
                new Vector3(GameplayReferences.HMD.position.x, trackingSpace.position.y, GameplayReferences.HMD.position.z),
                Quaternion.Euler(trackingSpace.rotation.x, GameplayReferences.HMD.rotation.y, trackingSpace.rotation.z));
            CoreInterface.RecordTrackingSpaceTransform(recenteredTransform, Util.Timestamp(Time.frameCount));
            CoreInterface.RecordBoundaryShape(BoundaryUtil.GetCurrentBoundaryPoints(), Util.Timestamp(Time.frameCount));
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_OnTick;
        }
#endif

#region Inspector Utils
        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_DEFAULT
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS || C3D_DEFAULT
            return "Records player boundary";
#else
            return "Current platform does not support this component. This component is only supported for Meta and OpenXR.";
#endif
        }
#endregion
    }
}
