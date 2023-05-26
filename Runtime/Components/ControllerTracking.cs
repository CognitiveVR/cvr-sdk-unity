using UnityEngine.XR;
using UnityEngine;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Controller Tracking")]
    public class ControllerTracking : AnalyticsComponentBase
    {
        private readonly float ControllerTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;

        protected override void OnSessionBegin()
        {
#if XRPF
             if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                InputTracking.trackingLost += OnTrackingLost;
            }

            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnPreSessionEnd += Cleanup;
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

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            currentTime += deltaTime;
            if (currentTime > ControllerTrackingInterval)
            {
                ControllerTrackingIntervalEnd();
            }
        }

        void ControllerTrackingIntervalEnd()
        {
            Transform leftController;
            Transform rightController;
            if (GameplayReferences.GetControllerTransform(false, out leftController))
            {
                float leftControllerToHead = leftController.position.y - GameplayReferences.HMD.position.y;
                SensorRecorder.RecordDataPoint("Left Controller Elevation from Head", leftControllerToHead);
            }
            if (GameplayReferences.GetControllerTransform(true, out rightController))
            {
                float rightControllerToHead = rightController.position.y - GameplayReferences.HMD.position.y;
                SensorRecorder.RecordDataPoint("Right Controller Elevation from Head", rightControllerToHead);
            }
            currentTime = 0;
        }

        private void Cleanup()
        {
            InputTracking.trackingLost -= OnTrackingLost;
            Cognitive3D_Manager.OnPreSessionEnd -= Cleanup;
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        public override string GetDescription()
        {
            return "Sends events related to controllers such as tracking and height";
        }
    }
}