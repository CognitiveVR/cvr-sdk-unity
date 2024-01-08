using UnityEngine.XR;
using UnityEngine;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Controller Tracking")]
    public class ControllerTracking : AnalyticsComponentBase
    {
        private readonly float ControllerTrackingInterval = 1;

        /// <summary>
        /// Counts up the deltatime to determine when the interval ends
        /// </summary>
        private float currentTime;

        /// <summary>
        /// Angle between left controller and hmd; used as a property for custom event
        /// </summary>
        float leftAngle;

        /// <summary>
        /// Angle between right controller and hmd; used as a property for custom event
        /// </summary>
        float rightAngle;

        /// <summary>
        /// Vector from left controller to HMD
        /// </summary>
        Vector3 leftControllerToHMD;

        /// <summary>
        /// Vector from right controller to HMD
        /// </summary>
        Vector3 rightControllerToHMD;

        /// <summary>
        /// How long to wait before sending "left controller tracking lost" events
        /// </summary>
        private const float LEFT_TRACKING_COOLDOWN_TIME_IN_SECONDS = 5;

        /// <summary>
        /// How long to wait before sending "right controller tracking lost" events
        /// </summary>
        private const float RIGHT_TRACKING_COOLDOWN_TIME_IN_SECONDS = 5;

        /// <summary>
        /// Whether right controller tracking is in cooldown
        /// </summary>
        bool inRightCooldown;

        /// <summary>
        /// Whether right controller tracking is in cooldown
        /// </summary>
        bool inLeftCooldown;

        /// <summary>
        /// Internal clock to measure how long has elapsed since last left controller lost tracking event
        /// </summary>
        float leftCooldownTimer;

        /// <summary>
        /// Internal clock to measure how long has elapsed since last right controller lost tracking event
        /// </summary>
        float rightCooldownTimer;

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
        
        /// <summary>
        /// Fucntion to execute when xrNodeState.nodeType loses tracking
        /// We have a cooldown timer to prevent multiple back-to-back events from being sent
        /// </summary>
        /// <param name="xrNodeState">The state of the device</param>
        public void OnTrackingLost(XRNodeState xrNodeState)
        {
            if (xrNodeState.nodeType == XRNode.LeftHand && !inLeftCooldown)
            {
                new CustomEvent("c3d.Left Controller Lost tracking")
                    .SetProperty("Angle from HMD", leftAngle)
                    .SetProperty("Height from HMD", leftControllerToHMD.y)
                    .Send();
                inLeftCooldown = true;
                leftCooldownTimer = 0;
            }

            if (xrNodeState.nodeType == XRNode.RightHand && !inRightCooldown)
            {
                new CustomEvent("c3d.Right Controller Lost tracking")
                    .SetProperty("Angle from HMD", rightAngle)
                    .SetProperty("Height from HMD", rightControllerToHMD.y)
                    .Send();
                inRightCooldown = true;
                rightCooldownTimer = 0;
            }
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()  
            if (isActiveAndEnabled)
            {
                UpdateCooldownClock(deltaTime);

                Transform rightControllerTransform;
                Transform leftControllerTransform;
                bool wasRightControllerFound = GameplayReferences.GetControllerTransform(false, out leftControllerTransform);
                bool wasLeftControllerFound = GameplayReferences.GetControllerTransform(true, out rightControllerTransform);
                leftControllerToHMD = leftControllerTransform.position - GameplayReferences.HMD.position;
                rightControllerToHMD = rightControllerTransform.position - GameplayReferences.HMD.position;
                leftAngle = Vector3.Angle(leftControllerToHMD, GameplayReferences.HMD.forward);
                rightAngle = Vector3.Angle(rightControllerToHMD, GameplayReferences.HMD.forward);

                currentTime += deltaTime;
                if (currentTime > ControllerTrackingInterval)
                {
                    if (wasLeftControllerFound)
                    {
                        SensorRecorder.RecordDataPoint("c3d.controller.left.height.fromHMD", leftControllerToHMD.y);
                    }

                    if (wasRightControllerFound)
                    {
                        SensorRecorder.RecordDataPoint("c3d.controller.right.height.fromHMD", rightControllerToHMD.y);
                    }
                    currentTime = 0;
                }
            }
            else
            {
                Debug.LogWarning("Controller Tracking component is disabled. Please enable in inspector.");
            }
        }

        /// <summary>
        /// Increments cooldown timer and checks if cooldown time has elapsed
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        private void UpdateCooldownClock(float deltaTime)
        {
            if (inRightCooldown) { rightCooldownTimer += deltaTime; }
            if (rightCooldownTimer >= RIGHT_TRACKING_COOLDOWN_TIME_IN_SECONDS) { inRightCooldown = false; }
            if (inLeftCooldown) { leftCooldownTimer += deltaTime; }
            if (leftCooldownTimer >= LEFT_TRACKING_COOLDOWN_TIME_IN_SECONDS) { inLeftCooldown = false; }
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