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
        protected override void OnSessionBegin()
        {
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
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
            float pitch = Mathf.Asin(opposite / hypotenuse) * Mathf.Rad2Deg;
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
