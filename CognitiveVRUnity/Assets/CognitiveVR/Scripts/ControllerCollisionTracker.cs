using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when either the controller collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR
{
    public class ControllerCollisionTracker : CognitiveVRAnalyticsComponent
    {
        string controller0GUID;
        string controller1GUID;

        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.OnTick += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            bool hit;

            if (CognitiveVR_Manager.GetController(0) != null)
            {
                hit = Physics.CheckSphere(CognitiveVR_Manager.GetController(0).position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller0GUID))
                {
                    Util.logDebug("controller collision");
                    controller0GUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("collision", controller0GUID).setProperty("device", "controller 0").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller0GUID))
                {
                    Instrumentation.Transaction("collision", controller0GUID).end();
                    controller0GUID = string.Empty;
                }
            }

            if (CognitiveVR_Manager.GetController(1) != null)
            {
                hit = Physics.CheckSphere(CognitiveVR_Manager.GetController(1).position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(controller1GUID))
                {
                    Util.logDebug("controller collision");
                    controller1GUID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("collision", controller1GUID).setProperty("device", "controller 1").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(controller1GUID))
                {
                    Instrumentation.Transaction("collision", controller1GUID).end();
                    controller1GUID = string.Empty;
                }
            }
        }

        public static string GetDescription()
        {
            return "Sends transactions when either controller collides in the game world\nCollision layers are set in CognitiveVR_Preferences\nOnly SteamVR controllers are currently supported";
        }
    }
}