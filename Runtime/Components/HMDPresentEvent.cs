using UnityEngine;
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
    [DisallowMultipleComponent]
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
                Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
#if C3D_OCULUS
                OVRManager.HMDMounted += HandleHMDMounted;
                OVRManager.HMDUnmounted += HandleHMDUnmounted;
#endif
            }
        }


        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()  
            if (isActiveAndEnabled)
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
            else
            {
                Debug.LogWarning("HMD Present component is disabled. Please enable in inspector.");
            }
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

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
#if C3D_OCULUS
            OVRManager.HMDMounted -= HandleHMDMounted;
            OVRManager.HMDUnmounted -= HandleHMDUnmounted;
#endif
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        public override string GetDescription()
        {
            return "Sends a Custom Event when a player removes or wears HMD";
        }
    }
}
