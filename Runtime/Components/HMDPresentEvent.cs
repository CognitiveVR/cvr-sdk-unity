using UnityEngine;
using System.Collections;
using UnityEngine.XR;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

/// <summary>
/// Sends a Custom Event when a player removes or wears HMD
/// NOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/HMD Present Event")]
    public class HMDPresentEvent : AnalyticsComponentBase
    {
        InputDevice currentHmd;
        bool wasUserPresentPreviously;

        protected override void OnSessionBegin()
        {
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                if (!currentHmd.isValid)
                {
                    currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                    currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out wasUserPresentPreviously);
                }
                currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out wasUserPresentPreviously);
#if C3D_OCULUS
                OVRManager.HMDMounted += HandleHMDMounted;
                OVRManager.HMDUnmounted += HandleHMDUnmounted;
                Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
#endif
            }
        }

        private void Update()
        {
#if !C3D_OMNICEPT && !C3D_VIVEWAVE && !C3D_OCULUS
            if (!currentHmd.isValid)
            {
                currentHmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out wasUserPresentPreviously);
            }
            else
            {
                CheckUserPresence();
            }
#endif
        }

        void HandleHMDMounted()
        {
            CustomEvent.SendCustomEvent("c3d.User equipped headset", GameplayReferences.HMD.position);
        }

        void HandleHMDUnmounted()
        {
            CustomEvent.SendCustomEvent("c3d.User removed headset", GameplayReferences.HMD.position);
        }

        void CheckUserPresence()
        {
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                bool isUserCurrentlyPresent;
                if (currentHmd.TryGetFeatureValue(CommonUsages.userPresence, out isUserCurrentlyPresent))
                {
                    if (isUserCurrentlyPresent && !wasUserPresentPreviously) // put on headset after removing
                    {
                        CustomEvent.SendCustomEvent("c3d.User equipped headset", GameplayReferences.HMD.position);
                        wasUserPresentPreviously = true;
                    }
                    else if (!isUserCurrentlyPresent && wasUserPresentPreviously) // removing headset
                    {
                        CustomEvent.SendCustomEvent("c3d.User removed headset", GameplayReferences.HMD.position);
                        wasUserPresentPreviously = false;
                    }
                }
            }
        }


#if C3D_OCULUS
        private void Cognitive3D_Manager_OnPostSessionEnd()
        {
            OVRManager.HMDMounted -= HandleHMDMounted;
            OVRManager.HMDUnmounted -= HandleHMDUnmounted;
            Cognitive3D_Manager.OnPostSessionEnd -= Cognitive3D_Manager_OnPostSessionEnd;
        }
#endif

        public override string GetDescription()
        {
            return "Sends a Custom Event when a player removes or wears HMD";
        }
    }
}
