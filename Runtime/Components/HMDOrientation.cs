using UnityEngine;

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

            // We want to calculate pitch in reference to trackingSpace to eliminate any discrepancy from custom rig setup
            // We want the angle on yz-plane only, but we need to take x-component into account for when you look to side or corner
            // Looking down will give us a negative angle, and looking up will give us a positive angle
            Vector2 trackingSpaceForwardYZ = new Vector2(trackingSpace.forward.y, trackingSpace.forward.z);
            Vector2 hmdForwardXZ = new Vector2(GameplayReferences.HMD.forward.x, GameplayReferences.HMD.forward.z);
            float hmdForwardXZMagnitude = Vector2.SqrMagnitude(hmdForwardXZ);
            Vector2 hmdPitchVector = new Vector2(GameplayReferences.HMD.forward.y, hmdForwardXZMagnitude);
            float pitch = Vector2.SignedAngle(hmdPitchVector, trackingSpaceForwardYZ);

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
