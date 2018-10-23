using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when either the controller collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR.Components
{
    public class ControllerCollisionEvent : CognitiveVRAnalyticsComponent
    {
        bool LeftControllerColliding;
        bool RightControllerColliding;

        [DisplaySetting]
        public LayerMask CollisionLayerMask = 1;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            bool hit;

#if CVR_STEAMVR
            if (CognitiveVR_Manager.GetController(false) != null)
#endif
            {
                Vector3 pos = CognitiveVR_Manager.GetControllerPosition(false);

                hit = Physics.CheckSphere(pos, 0.1f, CollisionLayerMask);
                if (hit && !LeftControllerColliding)
                {
                    Util.logDebug("controller collision");
                    LeftControllerColliding = true;
                    new CustomEvent("cvr.collision").SetProperty("device", "left controller").SetProperty("state","begin").Send();
                }
                else if (!hit && LeftControllerColliding)
                {
                    new CustomEvent("cvr.collision").SetProperty("device", "left controller").SetProperty("state", "end").Send();
                    LeftControllerColliding = false;
                }
            }


#if CVR_STEAMVR
            if (CognitiveVR_Manager.GetController(true) != null)
#endif
            {
                Vector3 pos = CognitiveVR_Manager.GetControllerPosition(true);

                hit = Physics.CheckSphere(pos, 0.1f, CollisionLayerMask);
                if (hit && !RightControllerColliding)
                {
                    Util.logDebug("controller collision");
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

        public static string GetDescription()
        {
            return "Sends transactions when either controller collides in the game world\nCollision layers are set in CognitiveVR_Preferences\nRequires SteamVR or Oculus Touch controllers";
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