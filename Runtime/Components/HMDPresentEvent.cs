using UnityEngine;
using System.Collections;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// sends transactions when a player removes or wears HMD
/// NOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Present Event")]
    public class HMDPresentEvent : Cognitive3DAnalyticsComponent
    {
#if C3D_OCULUS

        public override void Cognitive3D_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.Cognitive3D_Init(initError);
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

#if C3D_STEAMVR

        public override void Cognitive3D_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.Cognitive3D_Init(initError);

            //Cognitive3D_Manager.PoseEvent += Cognitive3D_Manager_OnPoseEvent;
            //SteamVR_Events.System(Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionStarted).AddListener(OnDeviceActivated);
            //SteamVR_Events.System(Valve.VR.EVREventType.VREvent_TrackedDeviceUserInteractionEnded).AddListener(OnDeviceActivated);
        }

        private void OnDeviceActivated(VREvent_t arg0)
        {
            var activity = Valve.VR.OpenVR.System.GetTrackedDeviceActivityLevel(0);
            Debug.Log(activity);
        }

        void Cognitive3D_Manager_OnPoseEvent(Valve.VR.EVREventType evrevent)
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
            //Cognitive3D_Manager.PoseEvent -= Cognitive3D_Manager_OnPoseEvent;
        }
#endif

        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_STEAMVR
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_STEAMVR
            return "Sends transactions when a player removes or wears HMD. SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD!";
#else
            return "Sends transactions when a player removes or wears HMD";
#endif
        }
    }
}