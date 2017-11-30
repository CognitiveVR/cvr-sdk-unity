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
        string controller0GUID;
        string controller1GUID;

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
                if (hit && string.IsNullOrEmpty(controller0GUID))
                {
                    Util.logDebug("controller collision");
                    controller0GUID = Util.GetUniqueId();
                    Instrumentation.Transaction("cvr.collision", controller0GUID).setProperty("device", "left controller").setProperty("state","begin").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller0GUID))
                {
                    Instrumentation.Transaction("cvr.collision", controller0GUID).setProperty("device", "left controller").setProperty("state", "end").end();
                    controller0GUID = string.Empty;
                }
            }


#if CVR_STEAMVR
            if (CognitiveVR_Manager.GetController(true) != null)
#endif
            {
                Vector3 pos = CognitiveVR_Manager.GetControllerPosition(true);

                hit = Physics.CheckSphere(pos, 0.1f, CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller1GUID))
                {
                    Util.logDebug("controller collision");
                    controller1GUID = Util.GetUniqueId();
                    Instrumentation.Transaction("cvr.collision", controller1GUID).setProperty("device", "right controller").setProperty("state", "begin").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller1GUID))
                {
                    Instrumentation.Transaction("cvr.collision", controller1GUID).setProperty("device", "right controller").setProperty("state", "end").end();
                    controller1GUID = string.Empty;
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