using UnityEngine;
using System.Collections;
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// sends transactions when a player removes or wears HMD
/// NOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD
/// </summary>

namespace CognitiveVR.Components
{
    public class HMDPresentEvent : CognitiveVRAnalyticsComponent
    {
#if CVR_OCULUS

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            OVRManager.HMDMounted += OVRManager_HMDMounted;
            OVRManager.HMDUnmounted += OVRManager_HMDUnmounted;
        }

        private void OVRManager_HMDMounted()
        {
            new CustomEvent("cvr.hmdpresent").SetProperty("present", true).SetProperty("starttime", Time.time).Send();
        }

        private void OVRManager_HMDUnmounted()
        {
            new CustomEvent("cvr.hmdpresent").SetProperty("present", false).SetProperty("endtime", Time.time).Send();
        }

        void OnDestroy()
        {
            OVRManager.HMDMounted -= OVRManager_HMDMounted;
            OVRManager.HMDUnmounted -= OVRManager_HMDUnmounted;
        }
#endif

#if CVR_STEAMVR

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);

            //CognitiveVR_Manager.PoseEvent += CognitiveVR_Manager_OnPoseEvent;
            //SteamVR_Events.System(Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted).AddListener(OnDeviceActivated);
            //SteamVR_Events.System(Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded).AddListener(OnDeviceActivated);
        }

        private void OnDeviceActivated(VREvent_t arg0)
        {
            var activity = Valve.VR.OpenVR.System.GetTrackedDeviceActivityLevel(0);
            Debug.Log(activity);
        }

        void CognitiveVR_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
        {
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted)
            {
                new CustomEvent("cvr.hmdpresent").SetProperty("present", true).SetProperty("starttime", Time.time).Send();
            }
            if (evrevent == Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded)
            {
                new CustomEvent("cvr.hmdpresent").SetProperty("present", false).SetProperty("endtime", Time.time - 10f).Send();
            }
        }

        void OnDestroy()
        {
            //CognitiveVR_Manager.PoseEvent -= CognitiveVR_Manager_OnPoseEvent;
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
    }
}