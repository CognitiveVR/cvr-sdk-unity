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
    public class HMDPresentEvent : AnalyticsComponentBase
    {
#if C3D_OCULUS

        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();
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

        public override bool GetWarning()
        {
#if C3D_OCULUS
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
            return "Sends transactions when a player removes or wears HMD";
        }
    }
}