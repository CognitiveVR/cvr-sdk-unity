using UnityEngine.XR;

namespace Cognitive3D.Components
{
    public class ControllerTracking : AnalyticsComponentBase
    {
        InputDevice rightController;
        InputDevice leftController;

        protected override void OnSessionBegin()
        {
            if (rightController.isValid == false || leftController.isValid == false)
            {
                rightController = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                leftController = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
#if XRPF
                if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
                {
                    InputTracking.trackingLost += OnTrackingLost;
                }
                Cognitive3D_Manager.OnPreSessionEnd += Cleanup;
            }
        }

        public void OnTrackingLost(XRNodeState xrNodeState)
        {
            if (!xrNodeState.tracked)
            {
                if (xrNodeState.nodeType == XRNode.RightHand)
                {
                    new CustomEvent("c3d.Right Controller Lost tracking").Send();
                }
                if (xrNodeState.nodeType == XRNode.LeftHand)
                {
                    new CustomEvent("c3d.Left Controller Lost tracking").Send();
                }
            }
        }

        private void Cleanup()
        {
            InputTracking.trackingLost -= OnTrackingLost;
            Cognitive3D_Manager.OnPreSessionEnd -= Cleanup;
        }

        public override string GetDescription()
        {
            return "Sends events when either controller loses tracking";
        }
    }

}