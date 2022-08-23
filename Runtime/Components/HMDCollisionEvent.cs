using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when the HMD collides with something in the game world
/// collision layers are set in Cognitive3D_Preferences
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Collision Event")]
    public class HMDCollisionEvent : Cognitive3DAnalyticsComponent
    {
        public LayerMask CollisionLayerMask = 1;

        bool HMDColliding;
        public override void Cognitive3D_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.Cognitive3D_Init(initError);
            Core.TickEvent += Cognitive3D_Manager_OnTick;
        }

        private void Cognitive3D_Manager_OnTick()
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
#if C3D_OCULUS || C3D_STEAMVR || C3D_STEAMVR2 || C3D_NEURABLE || C3D_VARJO || C3D_FOVE || C3D_PICOVR || C3D_PICOXR
            return "Sends transactions if the HMD collides with something in the game world";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_STEAMVR || C3D_STEAMVR2 || C3D_NEURABLE || C3D_VARJO || C3D_FOVE || C3D_PICOVR || C3D_PICOXR
            return false;
#else
            return true;
#endif
        }

        void OnDestroy()
        {
            Core.TickEvent -= Cognitive3D_Manager_OnTick;
        }
    }
}