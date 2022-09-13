using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when the HMD collides with something in the game world
/// collision layers are set in Cognitive3D_Preferences
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Collision Event")]
    public class HMDCollisionEvent : AnalyticsComponentBase
    {
        public LayerMask CollisionLayerMask = 1;

        bool HMDColliding;
        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();
            Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;
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
            return "Sends event if the HMD collides with something in the game world";
        }

        public override bool GetWarning()
        {
            return false;
        }

        void OnDestroy()
        {
            Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_OnTick;
        }
    }
}