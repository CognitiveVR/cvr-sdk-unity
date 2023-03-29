using UnityEngine.XR;
using UnityEngine;

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Controller Tracking")]
    public class ControllerTracking : AnalyticsComponentBase
    {
        Transform trackingSpace;
        Transform leftController;
        Transform rightController;
        GameObject ovrCameraRig;
        OVRManager ovrManager;

        [ClampSetting(1f, 5f)]
        [Tooltip("Number of seconds used to average to determine framerate. Lower means more smaller samples and more detail")]
        public float FramerateTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
        //the number of frames in the interval
        private int intervalFrameCount;

        protected override void OnSessionBegin()
        {
#if XRPF
             if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                InputTracking.trackingLost += OnTrackingLost;
            }
#if C3D_OCULUS
            ovrCameraRig = GameObject.Find("OVRCameraRig");
            if (ovrCameraRig != null)
            {
                ovrManager = ovrCameraRig.GetComponent<OVRManager>();
            }
#endif
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
            intervalFrameCount++;
            currentTime += deltaTime;
#if C3D_OCULUS
            rightController = GameplayReferences.controllerTransforms[0];
            leftController = GameplayReferences.controllerTransforms[1];
            if (currentTime > FramerateTrackingInterval)
            {
                ControllerTrackingIntervalEnd(trackingSpace, leftController, rightController);
            }
#endif
        }

        void ControllerTrackingIntervalEnd(Transform space, Transform leftController, Transform rightController)
        {
            float rightControllerToHead = 0.0f;
            float leftControllerToHead = 0.0f;
            if (ovrManager != null)
            {
                switch (ovrManager.trackingOriginType)
                {
                    case OVRManager.TrackingOrigin.EyeLevel:
                        leftControllerToHead = leftController.localPosition.y;
                        rightControllerToHead = rightController.localPosition.y;
                        break;
                    case OVRManager.TrackingOrigin.FloorLevel:
                    case OVRManager.TrackingOrigin.Stage:     // "stage" is the equivalent of "floor" in OpenXR
                        leftControllerToHead = leftController.position.y - GameplayReferences.HMD.position.y;
                        rightControllerToHead = rightController.position.y - GameplayReferences.HMD.position.y;
                        break;
                }
            }
            SensorRecorder.RecordDataPoint("Left Controller Distance from Head", leftControllerToHead);
            SensorRecorder.RecordDataPoint("Right Controller Distance from Head", rightControllerToHead);
            intervalFrameCount = 0;
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