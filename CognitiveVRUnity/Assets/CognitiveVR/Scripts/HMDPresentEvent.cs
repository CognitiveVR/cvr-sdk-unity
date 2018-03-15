using UnityEngine;
using System.Collections;

/// <summary>
/// sends transactions when a player removes or wears HMD
/// NOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD
/// </summary>

namespace CognitiveVR.Components
{
    public class HMDPresentEvent : CognitiveVRAnalyticsComponent
    {
        string hmdpresentGUID;
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
#if CVR_OCULUS
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
#elif CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_OnPoseEvent;
#endif
        }

        private void OVRManager_HMDMounted()
        {
            hmdpresentGUID = Util.GetUniqueId();
            new Transaction("cvr.hmdpresent").setProperty("present", true).setProperty("starttime", Time.time).begin();
        }

        private void OVRManager_HMDUnmounted()
        {
            new Transaction("cvr.hmdpresent").setProperty("present", false).setProperty("endtime", Time.time).end();
        }


#if CVR_STEAMVR
        void CognitiveVR_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                hmdpresentGUID = Util.GetUniqueId();
                Instrumentation.Transaction("cvr.hmdpresent", hmdpresentGUID).setProperty("present", true).setProperty("starttime", Time.time).begin();
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                Util.logDebug("hmd removed");
                Instrumentation.Transaction("cvr.hmdpresent", hmdpresentGUID).setProperty("present", false).setProperty("endtime", Time.time - 10f).end();
            }
        }
#endif

        public static bool GetWarning()
        {
#if !CVR_OCULUS && !CVR_STEAMVR
            return true;
#else
            return false;
#endif
        }

        public static string GetDescription()
        {
            return "Sends transactions when a player removes or wears HMD\nNOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD!";
        }

        void OnDestroy()
        {
#if CVR_STEAMVR
            CognitiveVR_Manager.PoseEvent -= CognitiveVR_Manager_OnPoseEvent;
#endif
#if CVR_OCULUS
            OVRManager.HMDMounted -= OVRManager_HMDMounted;
            OVRManager.HMDUnmounted -= OVRManager_HMDUnmounted;
#endif
        }
    }
}