using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when a player removes or wears HMD
/// NOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD
/// </summary>

namespace CognitiveVR
{
    public class HMDPresentTracker : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
#if CVR_STEAMVR
            CognitiveVR_Manager.OnPoseEvent += CognitiveVR_Manager_OnPoseEvent;
#endif
        }

        string hmdpresentGUID;
#if CVR_STEAMVR
        void CognitiveVR_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                hmdpresentGUID = System.Guid.NewGuid().ToString();
                Instrumentation.Transaction("HMDPresent", hmdpresentGUID).setProperty("present", true).setProperty("starttime", Time.time).begin();
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                Util.logDebug("hmd removed");
                Instrumentation.Transaction("HMDPresent", hmdpresentGUID).setProperty("present", false).setProperty("endtime", Time.time - 10f).end();
            }
        }
#endif

        public static string GetDescription()
        {
            return "Sends transactions when a player removes or wears HMD\nNOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD!";
        }
    }
}