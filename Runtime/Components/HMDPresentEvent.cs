using UnityEngine;
using System.Collections;
using UnityEngine.XR;
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
        InputDevice currentHmd;
        bool wasUserPresentLastFrame;
        private void Update()
        {
            if (!currentHmd.isValid)
            {
                currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out wasUserPresentLastFrame);
            }
            else
            {
                CheckUserPresence();
            }
        }

        void CheckUserPresence()
        {
            bool isUserCurrentlyPresent;
            if (currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out isUserCurrentlyPresent))
            {
                if (isUserCurrentlyPresent && !wasUserPresentLastFrame) // put on headset after removing
                {
                    CustomEvent.SendCustomEvent("cvr.Headset reworn by user", GameplayReferences.HMD.position);
                    wasUserPresentLastFrame = true;
                }
                else if (!isUserCurrentlyPresent && wasUserPresentLastFrame) // removing headset
                {
                    CustomEvent.SendCustomEvent("cvr.Headset removed by user", GameplayReferences.HMD.position);
                    wasUserPresentLastFrame = false;
                }
            }
        }

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