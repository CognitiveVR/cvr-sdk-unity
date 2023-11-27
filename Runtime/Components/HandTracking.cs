using UnityEngine;

namespace Cognitive3D.Components
{
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
            lastTrackedDevice = GetCurrentTrackedDevice();
            Cognitive3D_Manager.SetSessionProperty("c3d.app.handtracking.enabled", true);
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            CaptureHandTrackingEvents();
            SensorRecorder.RecordDataPoint("c3d.input.device", (int)GetCurrentTrackedDevice());
        }

        /// <summary>
        /// Gets the current tracked device i.e. hand or controller
        /// </summary>
        /// <returns> Enum representing whether user is using hand or controller or neither </returns>
        TrackingType GetCurrentTrackedDevice()
        {
            if (OVRInput.GetActiveController() == OVRInput.Controller.None)
            {
                return TrackingType.None;
            }
            else if (OVRInput.GetActiveController() == OVRInput.Controller.Hands
                || OVRInput.GetActiveController() == OVRInput.Controller.LHand
                || OVRInput.GetActiveController() == OVRInput.Controller.RHand)
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
            if (lastTrackedDevice != GetCurrentTrackedDevice())
            {
                new CustomEvent("c3d.input.tracking.device.changed.from." + lastTrackedDevice.ToString() + ".to." + GetCurrentTrackedDevice()).Send();
                lastTrackedDevice = GetCurrentTrackedDevice();
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
