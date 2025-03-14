using UnityEngine;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Hand Tracking")]
    public class HandTracking : AnalyticsComponentBase
    {
#if C3D_OCULUS || C3D_VIVEWAVE || C3D_PICOXR || C3D_DEFAULT
        internal delegate void onInputChanged(InputUtil.InputType currentTrackedDevice);
        internal static event onInputChanged OnInputChanged;
        private InputUtil.InputType lastTrackedDevice = InputUtil.InputType.None;
        private bool handsRegistered;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            lastTrackedDevice = InputUtil.GetCurrentTrackedDevice();
            Cognitive3D_Manager.SetSessionProperty("c3d.app.handtracking.enabled", true);
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;

            // Registering hands
            Cognitive3D_Manager.OnLevelLoaded += OnLevelLoaded;
            RegisterHands();
        }

        void OnLevelLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool didChangeSceneId)
        {
            if (didChangeSceneId && Cognitive3D_Manager.TrackingScene != null)
            {
                handsRegistered = false;

                // Registering new controllers
                RegisterHands();
            }
        }

        // Registers hands only if they are being used. If controllers are in use, hands will not be registered or recorded.
        void RegisterHands()
        {
            if (!Cognitive3D_Manager.autoInitializeInput || handsRegistered) return;
            
            // Hands
            var currentTrackedDevice = InputUtil.GetCurrentTrackedDevice();
            if (currentTrackedDevice == InputUtil.InputType.Hand)
            {
                DynamicManager.RegisterHand(UnityEngine.XR.XRNode.LeftHand, false);
                DynamicManager.RegisterHand(UnityEngine.XR.XRNode.RightHand, true);

                handsRegistered = true;

                DynamicManager.UpdateDynamicInputEnabledState(InputUtil.GetCurrentTrackedDevice());
            }
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                    var currentTrackedDevice = InputUtil.GetCurrentTrackedDevice();
                    CaptureHandTrackingEvents(currentTrackedDevice);
            }
            else
            {
                Debug.LogWarning("Hand Tracking component is disabled. Please enable in inspector.");
            }
        }

        /// <summary>
        /// Captures any change in input device from hand to controller to none or vice versa
        /// </summary>
        void CaptureHandTrackingEvents(InputUtil.InputType currentTrackedDevice)
        {
            if (lastTrackedDevice != currentTrackedDevice)
            {
                new CustomEvent("c3d.input.tracking.changed")
                    .SetProperty("Previously Tracking", lastTrackedDevice)
                    .SetProperty("Now Tracking", currentTrackedDevice)
                    .Send();
                lastTrackedDevice = currentTrackedDevice;

                OnInputChanged?.Invoke(currentTrackedDevice);

                // Register hands if they are now being tracked
                if (currentTrackedDevice == InputUtil.InputType.Hand && !handsRegistered)
                {
                    RegisterHands();
                }
                else
                {
                    DynamicManager.UpdateDynamicInputEnabledState(currentTrackedDevice);
                }
            }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;
        }
#endif

        #region Inspector Utils
        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_VIVEWAVE || C3D_PICOXR || C3D_DEFAULT
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS || C3D_VIVEWAVE || C3D_PICOXR || C3D_DEFAULT
            return "Collects and sends data pertaining to Hand Tracking";
#else
            return "This component is compatible with Oculus, Wave (HTC Vive), PicoXR, and OpenXR platforms";
#endif
        }
#endregion

    }
}
