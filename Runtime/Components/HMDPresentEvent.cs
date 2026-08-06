using UnityEngine;
#if UNITY_ANDROID && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Sends a Custom Event when a player removes or wears the HMD.
/// NOTE - SteamVR proximity sensor seems to have a delay of 10 seconds when removing the HMD.
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/HMD Present Event")]
    public class HMDPresentEvent : AnalyticsComponentBase
    {
        const float ProximityWornThreshold = 1f;

        UnityEngine.XR.InputDevice currentHmd;

        // Assume the user is wearing the headset at session start so we don't emit an "equipped" event on the first frame
        bool wasUserPresentPreviously = true;
        double? removedTimestamp;

        /// <summary>
        /// True when hardware data may be recorded. Always true unless the XR Privacy
        /// Framework is present and has withheld consent
        /// </summary>
        bool IsHardwareDataAllowed
        {
            get
            {
#if XRPF
                return XRPF.PrivacyFramework.Agreement.IsAgreementComplete
                    && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed;
#else
                return true;
#endif
            }
        }

        protected override void OnSessionBegin()
        {
            if (!IsHardwareDataAllowed) { return; }

            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;

#if C3D_OCULUS
            OVRManager.HMDMounted += HeadsetEquipped;
            OVRManager.HMDUnmounted += HeadsetRemoved;
#else
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
#endif
        }

#if !C3D_OCULUS
        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            if (!isActiveAndEnabled) { return; }

            if (!TryGetUserPresence(out bool isUserPresent)) { return; }

            if (isUserPresent && !wasUserPresentPreviously) // put the headset back on
            {
                HeadsetEquipped();
            }
            else if (!isUserPresent && wasUserPresentPreviously) // took the headset off
            {
                HeadsetRemoved();
            }

            wasUserPresentPreviously = isUserPresent;
        }

        bool TryGetUserPresence(out bool isUserPresent)
        {
            isUserPresent = false;
            if (!IsHardwareDataAllowed) { return false; }

#if UNITY_ANDROID && ENABLE_INPUT_SYSTEM
            var sensor = ProximitySensor.current;
            if (sensor == null) { return false; }

            if (!sensor.enabled)
            {
                InputSystem.EnableDevice(sensor);
            }

            isUserPresent = sensor.distance.ReadValue() < ProximityWornThreshold;
            return true;
#else
            if (!currentHmd.isValid)
            {
                currentHmd = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
            }

            return currentHmd.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out isUserPresent);
#endif
        }
#endif

        void HeadsetEquipped()
        {
            var equippedEvent = new CustomEvent("c3d.User equipped headset");

            if (removedTimestamp.HasValue)
            {
                double secondsRemoved = Util.Timestamp() - removedTimestamp.Value;
                if (secondsRemoved > 0)
                {
                    equippedEvent.SetProperty("Seconds headset was removed", secondsRemoved);
                }
                removedTimestamp = null;
            }

            equippedEvent.Send(GameplayReferences.HMD.position);
        }

        void HeadsetRemoved()
        {
            removedTimestamp = Util.Timestamp();
            CustomEvent.SendCustomEvent("c3d.User removed headset", GameplayReferences.HMD.position);
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
#if C3D_OCULUS
            OVRManager.HMDMounted -= HeadsetEquipped;
            OVRManager.HMDUnmounted -= HeadsetRemoved;
#else
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
#endif
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        public override string GetDescription()
        {
            return "Sends a Custom Event when a player removes or wears HMD";
        }
    }
}
