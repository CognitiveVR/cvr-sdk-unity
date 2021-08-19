using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when the HMD collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Collision Event")]
    public class HMDCollisionEvent : CognitiveVRAnalyticsComponent
    {
        public LayerMask CollisionLayerMask = 1;

        bool HMDColliding;
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            Core.TickEvent += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            if (GameplayReferences.HMD == null) { return; }

            bool hit = Physics.CheckSphere(GameplayReferences.HMD.position, 0.25f, CollisionLayerMask);
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
        public override string GetDescription()
        {
#if CVR_OCULUS || CVR_STEAMVR || CVR_STEAMVR2 || CVR_NEURABLE || CVR_VARJO || CVR_FOVE || CVR_PICOVR || CVR_PICOXR
            return "Sends transactions if the HMD collides with something in the game world";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if CVR_OCULUS || CVR_STEAMVR || CVR_STEAMVR2 || CVR_NEURABLE || CVR_VARJO || CVR_FOVE || CVR_PICOVR || CVR_PICOXR
            return false;
#else
            return true;
#endif
        }

        void OnDestroy()
        {
            Core.TickEvent -= CognitiveVR_Manager_OnTick;
        }
    }
}