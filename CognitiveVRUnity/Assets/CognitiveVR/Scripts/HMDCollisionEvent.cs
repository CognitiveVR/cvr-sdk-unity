using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when the HMD collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR.Components
{
    public class HMDCollisionEvent : CognitiveVRAnalyticsComponent
    {
        [DisplaySetting]
        public LayerMask CollisionLayerMask = 1;

        bool HMDColliding;
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }

            bool hit = Physics.CheckSphere(CognitiveVR_Manager.HMD.position, 0.25f, CollisionLayerMask);
            if (hit && !HMDColliding)
            {
                Util.logDebug("hmd collision");
                HMDColliding = true;
                new CustomEvent("cvr.collision").SetProperty("device", "HMD").SetProperty("state", "begin").Send();
            }
            else if (!hit && HMDColliding)
            {
                new CustomEvent("cvr.collision").SetProperty("device", "HMD").SetProperty("state", "end").Send();
                HMDColliding = false;
            }
        }
        public static string GetDescription()
        {
            return "Sends transactions if the HMD collides with something in the game world\nCollision layers are set in CognitiveVR_Preferences";
        }

        public static bool GetWarning()
        {
#if (!CVR_OCULUS && !CVR_STEAMVR) || UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_OnTick;
        }
    }
}