using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when the HMD collides with something in the game world
/// collision layers are set in CognitiveVR_Preferences
/// </summary>

namespace CognitiveVR
{
    public class HMDCollisionTracker : CognitiveVRAnalyticsComponent
    {
        string HMDGuid;
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.OnTick += CognitiveVR_Manager_OnTick;
        }

        private void CognitiveVR_Manager_OnTick()
        {
            if (CognitiveVR_Manager.HMD != null)
            {
                bool hit = Physics.CheckSphere(CognitiveVR_Manager.HMD.position, 0.25f, CognitiveVR_Preferences.Instance.CollisionLayerMask);
                if (hit && string.IsNullOrEmpty(HMDGuid))
                {
                    Util.logDebug("hmd collision");
                    HMDGuid = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("collision", HMDGuid).setProperty("device", "HMD").begin();
                }
                else if (!hit && !string.IsNullOrEmpty(HMDGuid))
                {
                    Instrumentation.Transaction("collision", HMDGuid).end();
                    HMDGuid = string.Empty;
                }
            }
        }
        public static string GetDescription()
        {
            return "Sends transactions if the HMD collides with something in the game world\nCollision layers are set in CognitiveVR_Preferences";
        }
    }
}