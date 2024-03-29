﻿using UnityEngine;

/// <summary>
/// Sends a Custom Event when the HMD collides with something in the game world
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/HMD Collision Event")]
    public class HMDCollisionEvent : AnalyticsComponentBase
    {
        /// <summary>
        /// The layer mask to check for HMD collisions
        /// </summary>
        public LayerMask CollisionLayerMask = 1;

        bool HMDColliding;
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;
        }

        private void Cognitive3D_Manager_OnTick()
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
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
            else
            {
                Debug.LogWarning("HMD Collision component is disabled. Please enable in inspector.");
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