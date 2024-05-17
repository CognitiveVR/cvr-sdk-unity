using UnityEngine;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Hand Tracking")]
    public class HandTracking : AnalyticsComponentBase
    {
        private GameplayReferences.TrackingType lastTrackedDevice = GameplayReferences.TrackingType.None;

        /// <summary>
        /// Captures any change in input device from hand to controller to none or vice versa
        /// </summary>
        void CaptureHandTrackingEvents(GameplayReferences.TrackingType currentTrackedDevice)
        {
            if (lastTrackedDevice != currentTrackedDevice)
            {
                new CustomEvent("c3d.input.tracking.changed")
                    .SetProperty("Previously Tracking", lastTrackedDevice)
                    .SetProperty("Now Tracking", currentTrackedDevice)
                    .Send();
                lastTrackedDevice = currentTrackedDevice;
            }
        }

#if C3D_MAGICLEAP2
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            // HAND_TRACKING is a normal permission, so we don't request it at runtime. It is auto-granted if included in the app manifest.
            // If it's missing from the manifest, the permission is not available.
            if (UnityEngine.XR.MagicLeap.MLPermissions.CheckPermission(UnityEngine.XR.MagicLeap.MLPermission.HandTracking).IsOk)
            {
                //Start Hand Tracking
                UnityEngine.XR.MagicLeap.InputSubsystem.Extensions.MLHandTracking.StartTracking();
                GameplayReferences.handTrackingEnabled = true;
                Cognitive3D_Manager.SetSessionProperty("c3d.app.handtracking.enabled", true);
                Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            }
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                var currentHandTrackingType = GetMagicLeapTrackingType();
                CaptureHandTrackingEvents(currentHandTrackingType);
            }
            else
            {
                Debug.LogWarning("Hand Tracking component is disabled. Please enable in inspector.");
            }
        }

        UnityEngine.XR.InputDevice leftHandDevice;
        UnityEngine.XR.InputDevice rightHandDevice;
        UnityEngine.XR.InputDevice controllerDevice;
        GameplayReferences.TrackingType GetMagicLeapTrackingType()
        {
            if (UnityEngine.XR.MagicLeap.MLPermissions.CheckPermission(UnityEngine.XR.MagicLeap.MLPermission.HandTracking).IsOk)
            {
                if (!leftHandDevice.isValid || !rightHandDevice.isValid)
                {
                    leftHandDevice = UnityEngine.XR.MagicLeap.InputSubsystem.Utils.FindMagicLeapDevice(UnityEngine.XR.InputDeviceCharacteristics.HandTracking | UnityEngine.XR.InputDeviceCharacteristics.Left);
                    rightHandDevice = UnityEngine.XR.MagicLeap.InputSubsystem.Utils.FindMagicLeapDevice(UnityEngine.XR.InputDeviceCharacteristics.HandTracking | UnityEngine.XR.InputDeviceCharacteristics.Right);
                }
                if (leftHandDevice.isValid || rightHandDevice.isValid)
                {
                    return GameplayReferences.TrackingType.Hand;
                }
            }

            if (!controllerDevice.isValid)
            {
                controllerDevice = UnityEngine.XR.MagicLeap.InputSubsystem.Utils.FindMagicLeapDevice(UnityEngine.XR.InputDeviceCharacteristics.Controller);
            }
            if (controllerDevice.isValid)
            {
                return GameplayReferences.TrackingType.Controller;
            }
            return GameplayReferences.TrackingType.None;
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

#if C3D_OCULUS
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            lastTrackedDevice = GameplayReferences.GetCurrentTrackedDevice();
            Cognitive3D_Manager.SetSessionProperty("c3d.app.handtracking.enabled", true);
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                var currentTrackedDevice = GameplayReferences.GetCurrentTrackedDevice();
                CaptureHandTrackingEvents(currentTrackedDevice);
            }
            else
            {
                Debug.LogWarning("Hand Tracking component is disabled. Please enable in inspector.");
            }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

        #region Inspector Utils
        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_MAGICLEAP2
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS || C3D_MAGICLEAP2
            return "Records the type of input device (as Hand or Controller) and when this devices changes. Records that this app supports handtracking";
#else
            return "This component can only be used on the Oculus platform";
#endif
        }
#endregion

    }
}
