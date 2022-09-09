using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when either the controller collides with something in the game world
/// collision layers are set in Cognitive3D_Preferences
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Controller Collision Event")]
    public class ControllerCollisionEvent : AnalyticsComponentBase
    {
        bool LeftControllerColliding;
        bool RightControllerColliding;
        
        public LayerMask CollisionLayerMask = 1;

        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();
            Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;
        }

        Transform tempInfo;
        private void Cognitive3D_Manager_OnTick()
        {
            bool hit;

            if (GameplayReferences.IsInputDeviceValid(UnityEngine.XR.XRNode.LeftHand))
            {
                if (GameplayReferences.GetControllerTransform(false,out tempInfo))
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
            if (GameplayReferences.IsInputDeviceValid(UnityEngine.XR.XRNode.RightHand))
            {
                if (GameplayReferences.GetControllerTransform(true, out tempInfo))
                {
                    hit = Physics.CheckSphere(tempInfo.transform.position, 0.1f, CollisionLayerMask);
                    if (hit && !LeftControllerColliding)
                    {
                        LeftControllerColliding = true;
                        new CustomEvent("cvr.collision").SetProperty("device", "right controller").SetProperty("state", "begin").Send();
                    }
                    else if (!hit && LeftControllerColliding)
                    {
                        new CustomEvent("cvr.collision").SetProperty("device", "right controller").SetProperty("state", "end").Send();
                        LeftControllerColliding = false;
                    }
                }
            }
        }

        public override string GetDescription()
        {
#if C3D_OCULUS || C3D_STEAMVR || C3D_STEAMVR2|| C3D_PICOVR || C3D_PICOXR
            return "Sends transactions when either controller collides in the game world";
#else
            return "Current platform does not support this component";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_STEAMVR || C3D_STEAMVR2 || C3D_PICOVR || C3D_PICOXR
            return false;
#else
            return true;
#endif
        }

        void OnDestroy()
        {
            Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_OnTick;
        }
    }
}