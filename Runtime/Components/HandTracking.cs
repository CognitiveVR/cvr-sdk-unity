using UnityEngine;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Hand Tracking")]
    public class HandTracking : AnalyticsComponentBase
    {
        internal delegate void onInputChanged(GameplayReferences.TrackingType currentTrackedDevice);
        internal static event onInputChanged OnInputChanged;
        
#if C3D_OCULUS || C3D_VIVEWAVE || C3D_PICOXR || C3D_DEFAULT
        private GameplayReferences.TrackingType lastTrackedDevice = GameplayReferences.TrackingType.None;

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

                OnInputChanged?.Invoke(currentTrackedDevice);
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
            return "This component is compatible with Oculus, Wave (Vive), PicoXR, and OpenXR platforms";
#endif
        }
#endregion

    }
}
