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

        /// <summary>
        /// Calculates pitch (angle of elevation/depression) of hmd/users neck
        /// Positive means looking up, negative means looking down
        /// TODO: include cos and tan to figure out if the person is looking more than 90 degrees up or down
        /// </summary>
        private void RecordPitch()
        {
            if (GameplayReferences.HMD == null) { return; }
            if (trackingSpace == null)
            {
                Debug.LogWarning("TrackingSpace not configured. Unable to record HMD Pitch");
                return;
            }

            // Start with quaternions to calculate rotation
            Quaternion hmdRotation = GameplayReferences.HMD.rotation;
            Quaternion trackingSpaceRotation = trackingSpace.transform.rotation;

            // Adjust rotations to "isolate" HMD rotation from trackingSpace rotation
            Quaternion adjustedRotation = Quaternion.Inverse(trackingSpaceRotation) * hmdRotation;

            float pitch = adjustedRotation.eulerAngles.x;
            
            // Take smaller angle of the explementary angles
            if (pitch > 180)
            {
                pitch -= 360;
            }

            // Adjusting to get positive when user looks up and negative when they look down
            pitch *= -1;

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
