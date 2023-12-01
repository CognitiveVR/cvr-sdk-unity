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
        private void RecordPitch()
        {
            if (GameplayReferences.HMD == null) { return; }
            if (trackingSpace == null)
            {
                Debug.LogWarning("TrackingSpace not configured. Unable to record HMD Pitch");
                return;
            }
            Vector3 forwardPoint = GameplayReferences.HMD.position + GameplayReferences.HMD.forward;
            
            // We are using trackingSpace for situations where user is not standing perpendicular to ground
            Vector3 opposite = forwardPoint + trackingSpace.up;

            // We need to get the angle between "straight ahead" and "HMD forward"
            // Sum of angles in triangle = 180; since right angled, the other angle is 90
            float pitch = 180 - 90 - Vector3.Angle(opposite, forwardPoint);
            SensorRecorder.RecordDataPoint("c3d.hmd.pitch", pitch);
        }

        //records yaw with 0 as the center and 180 as directly behind the player (from the starting position)
        private void RecordYaw()
        {
            if (GameplayReferences.HMD == null) { return; }
            float yaw = GameplayReferences.HMD.localRotation.eulerAngles.y;
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
