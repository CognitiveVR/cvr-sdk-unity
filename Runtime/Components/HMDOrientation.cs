using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Records yaw and orientation of the HMD for internal comfort and immersion calculations
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMDOrientation")]
    public class HMDOrientation : AnalyticsComponentBase
    {
        Transform trackingSpace = null;

        protected override void OnSessionBegin()
        {
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.Instance.TryGetTrackingSpace(out trackingSpace);
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            RecordPitch();
            RecordYaw();
        }

        //TODO include cos and tan to figure out if the person is looking more than 90 degrees up or down
        //TODO handle situations where tracking space isn't along the x/z plane
        private void RecordPitch()
        {
            if (GameplayReferences.HMD == null) { return; }
            Vector3 forwardPoint = GameplayReferences.HMD.position + GameplayReferences.HMD.forward;
            float opposite = GameplayReferences.HMD.position.y - forwardPoint.y;
            float hypotenuse = 1f;
            float pitch = -Mathf.Asin(opposite / hypotenuse) * Mathf.Rad2Deg;
            SensorRecorder.RecordDataPoint("c3d.hmd.pitch", pitch);
        }

        /// <summary>
        /// Calculates yaw of hmd/users neck
        /// Positive means looking right, negative means looking left
        /// </summary>
        private void RecordYaw()
        {
            if (GameplayReferences.HMD == null) { return; }
            if (trackingSpace == null)
            {
                Debug.LogWarning("TrackingSpace not configured. Unable to record HMD Yaw");
                return;
            }

            // Start with quaternions to calculate rotation
            Quaternion hmdRotation = GameplayReferences.HMD.rotation;
            Quaternion trackingSpaceRotation = trackingSpace.transform.rotation;

            // Adjust rotations to "isolate" HMD rotation from trackingSpace rotation
            Quaternion adjustedRotation = Quaternion.Inverse(trackingSpaceRotation) * hmdRotation;

            float yaw = adjustedRotation.eulerAngles.y;

            // Take smaller angle of the explementary angles
            if (yaw > 180)
            {
                yaw -= 360;
            }

            SensorRecorder.RecordDataPoint("c3d.hmd.yaw", yaw);
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        public override string GetDescription()
        {
            return "Records orientation of the HMD as different sensors. Primarily for internal calculations";
        }

        public override bool GetWarning()
        {
            return false;
        }
    }
}
