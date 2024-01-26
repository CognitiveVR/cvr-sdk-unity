using UnityEngine;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Hand Tracking")]
    public class HandTracking : AnalyticsComponentBase
    {
#if C3D_OCULUS
        /// <summary>
        /// Represents participant is using hands, controller, or neither
        /// </summary>
        private enum TrackingType
        {
            None = 0,
            Controller = 1,
            Hand = 2
        }

        private TrackingType lastTrackedDevice = TrackingType.None;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
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
                CaptureHandTrackingEvents();
            }
            else
            {
                Debug.LogWarning("Hand Tracking component is disabled. Please enable in inspector.");
            }
        }

        /// <summary>
        /// Gets the current tracked device i.e. hand or controller
        /// </summary>
        /// <returns> Enum representing whether user is using hand or controller or neither </returns>
        TrackingType GetCurrentTrackedDevice()
        {
            var currentOVRTrackedDevice = OVRInput.GetActiveController();
            if (currentOVRTrackedDevice == OVRInput.Controller.None)
            {
                return TrackingType.None;
            }
            else if (currentOVRTrackedDevice == OVRInput.Controller.Hands
                || currentOVRTrackedDevice == OVRInput.Controller.LHand
                || currentOVRTrackedDevice == OVRInput.Controller.RHand)
            {
                return TrackingType.Hand;
            }
            else
            {
                return TrackingType.Controller;
            }
        }

        /// <summary>
        /// Captures any change in input device from hand to controller to none or vice versa
        /// </summary>
        void CaptureHandTrackingEvents()
        {
            var currentTrackedDevice = GetCurrentTrackedDevice();
            if (lastTrackedDevice != currentTrackedDevice)
            {
                new CustomEvent("c3d.input.tracking.changed")
                    .SetProperty("Previously Tracking", lastTrackedDevice)
                    .SetProperty("Now Tracking", currentTrackedDevice)
                    .Send();
                lastTrackedDevice = currentTrackedDevice;
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
#if C3D_OCULUS
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Collects and sends data pertaining to Hand Tracking";
#else
            return "This component can only be used on the Oculus platform";
#endif
        }
#endregion

    }
}
