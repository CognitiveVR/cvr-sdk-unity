using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when either the controller collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Controller Collision Event")]
    public class ControllerCollisionEvent : CognitiveVRAnalyticsComponent
    {
        bool LeftControllerColliding;
        bool RightControllerColliding;
        
        public LayerMask CollisionLayerMask = 1;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            Core.TickEvent += CognitiveVR_Manager_OnTick;
        }

        GameplayReferences.ControllerInfo tempInfo;
        private void CognitiveVR_Manager_OnTick()
        {
            bool hit;

            if (GameplayReferences.GetControllerInfo(false,out tempInfo))
            {
                if (tempInfo.connected && tempInfo.visible)
                {
                    hit = Physics.CheckSphere(tempInfo.transform.position, 0.1f, CollisionLayerMask);
                    if (hit && !LeftControllerColliding)
                    {
                        LeftControllerColliding = true;
                        new CustomEvent("cvr.collision").SetProperty("device", "left controller").SetProperty("state", "begin").Send();
                    }
                    else if (!hit && LeftControllerColliding)
                    {
                        new CustomEvent("cvr.collision").SetProperty("device", "left controller").SetProperty("state", "end").Send();
                        LeftControllerColliding = false;
                    }
                }
            }

            if (GameplayReferences.GetControllerInfo(true, out tempInfo))
            {
                if (tempInfo.connected && tempInfo.visible)
                {
                    hit = Physics.CheckSphere(tempInfo.transform.position, 0.1f, CollisionLayerMask);
                    if (hit && !RightControllerColliding)
                    {
                        RightControllerColliding = true;
                        new CustomEvent("cvr.collision").SetProperty("device", "right controller").SetProperty("state", "begin").Send();
                    }
                    else if (!hit && RightControllerColliding)
                    {
                        new CustomEvent("cvr.collision").SetProperty("device", "right controller").SetProperty("state", "end").Send();
                        RightControllerColliding = false;
                    }
                }
            }
        }

        public override string GetDescription()
        {
#if CVR_OCULUS || CVR_STEAMVR || CVR_STEAMVR2|| CVR_PICOVR || CVR_PICOXR
            return "Sends transactions when either controller collides in the game world";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if CVR_OCULUS || CVR_STEAMVR || CVR_STEAMVR2 || CVR_PICOVR || CVR_PICOXR
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