using UnityEngine;

/// <summary>
/// Records yaw and orientation of the HMD for internal comfort and immersion calculations
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/HMDOrientation")]
    public class HMDOrientation : AnalyticsComponentBase
    {
        private readonly float HMDOrientationInterval = 1;
        private float currentTime;

        protected override void OnSessionBegin()
        {
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                currentTime += Time.deltaTime;
                if (currentTime > HMDOrientationInterval)
                {
                    currentTime = 0;
                    if (GameplayReferences.HMD == null || Cognitive3D_Manager.Instance.trackingSpace == null)
                    {
                        Util.LogOnce("TrackingSpace and/or HMD not configured correctly. Unable to record HMD Orientation.", LogType.Warning);
                        return;
                    }
                    RecordPitch();
                    RecordYaw();
                }
            }
            else
            {
                Debug.LogWarning("HMD Orientation component is disabled. Please enable in inspector.");
            }
        }

        /// <summary>
        /// Calculates pitch (angle of elevation/depression) of hmd/users neck
        /// Positive means looking up, negative means looking down
        /// TODO: include cos and tan to figure out if the person is looking more than 90 degrees up or down
        /// </summary>
        private void RecordPitch()
        {
            // Start with quaternions to calculate rotation
            Quaternion hmdRotation = GameplayReferences.HMD.rotation;
            Quaternion trackingSpaceRotation = Cognitive3D_Manager.Instance.trackingSpace.transform.rotation;

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

        /// <summary>
        /// Calculates yaw of hmd/users neck
        /// Positive means looking right, negative means looking left
        /// </summary>
        private void RecordYaw()
        {
            // Start with quaternions to calculate rotation
            Quaternion hmdRotation = GameplayReferences.HMD.rotation;
            Quaternion trackingSpaceRotation = Cognitive3D_Manager.Instance.trackingSpace.transform.rotation;

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
