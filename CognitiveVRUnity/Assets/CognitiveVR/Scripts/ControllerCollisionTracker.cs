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
            bool hit = Physics.CheckSphere(CognitiveVR_Manager.GetController(0).position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
            if (hit)
            {
                Util.logDebug("controller collision");
                Instrumentation.Transaction("collision").setProperty("device", "controller 0").beginAndEnd();
            }

            hit = Physics.CheckSphere(CognitiveVR_Manager.GetController(1).position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
            if (hit)
            {
                Util.logDebug("controller collision");
                Instrumentation.Transaction("collision").setProperty("device", "controller 1").beginAndEnd();
            }
        }

        public static string GetDescription()
        {
            return "Sends transactions when either controller collides in the game world\nCollision layers are set in CognitiveVR_Preferences";
        }
    }
}