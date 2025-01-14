using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    /// <summary>
    /// This is used to interact with exit poll buttons using controllers and/or hands <br/>
    /// Handles interaction via trigger and/or pinch and displays a line renderer showing where the user is pointing
    /// </summary>
    [AddComponentMenu("Cognitive3D/Internal/Pointer Input Handler")]
    public class PointerInputHandler : MonoBehaviour
    {
        /// <summary>
        /// Used as maximum distance for Raycast
        /// </summary>
        protected const float DEFAULT_LENGTH_FOR_POINTER = 20;

        /// <summary>
        /// True if right hand; false otherwise
        /// </summary>
        private bool isRightHand;

#if C3D_OCULUS
        private List<OVRHand> hands = new List<OVRHand>();
        private OVRHand activeHand;
#endif

        void Update()
        {
            var currentTrackedDevice = GameplayReferences.GetCurrentTrackedDevice();

            if (currentTrackedDevice == GameplayReferences.TrackingType.Hand)
            {
                HandleHandInput();
            }
            else if (currentTrackedDevice == GameplayReferences.TrackingType.Controller)
            {
                HandleControllerInput();
            }
        }

        private void HandleHandInput()
        {
#if C3D_OCULUS
            if (hands.Count == 0)
            {
                OVRHand[] foundHands = FindObjectsOfType<OVRHand>();
                hands.Clear();
                foreach (OVRHand hand in foundHands)
                {
                    hands.Add(hand);
                }
            }

            foreach (OVRHand hand in hands)
            {
                if (hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) >= 1.0f && hand.HandConfidence == OVRHand.TrackingConfidence.High)
                {
                    activeHand = hand;
                }
            }

            if (activeHand != null)
            {
                Vector3 start = GameplayReferences.HMD.transform.parent.TransformPoint(activeHand.PointerPose.position);
                Vector3 direction = GameplayReferences.HMD.transform.parent.transform.rotation * activeHand.PointerPose.rotation * Vector3.forward;
                UpdatePointer(start, direction, true, true);
            }
#endif
        }

        private void HandleControllerInput()
        {
            float rightTriggerValue = GameplayReferences.rightTriggerValue;
            float leftTriggerValue = GameplayReferences.leftTriggerValue;

            // Determine which controller is active based on trigger press
            if (rightTriggerValue > 0.5f)
            {
                isRightHand = true; // Switch to right controller
            }
            else if (leftTriggerValue > 0.5f)
            {
                isRightHand = false; // Switch to left controller
            }

            Vector3 controllerPosition;
            Quaternion controllerRotation;

            if (isRightHand)
            {
                GameplayReferences.TryGetControllerPosition(UnityEngine.XR.XRNode.RightHand, out controllerPosition);
                GameplayReferences.TryGetControllerRotation(UnityEngine.XR.XRNode.RightHand, out controllerRotation);
            }
            else
            {
                GameplayReferences.TryGetControllerPosition(UnityEngine.XR.XRNode.LeftHand, out controllerPosition);
                GameplayReferences.TryGetControllerRotation(UnityEngine.XR.XRNode.LeftHand, out controllerRotation);
            }

            Vector3 direction = controllerRotation * Vector3.forward;
            bool activation = (isRightHand ? rightTriggerValue : leftTriggerValue) > 0.5f;

            UpdatePointer(controllerPosition, direction, activation, false);
        }

        private void UpdatePointer(Vector3 start, Vector3 direction, bool activation, bool fillActivate)
        {
            Vector3 end = start + direction * DEFAULT_LENGTH_FOR_POINTER;

            if (Physics.Raycast(start, direction, out RaycastHit hit, DEFAULT_LENGTH_FOR_POINTER, LayerMask.GetMask("UI")))
            {
                IPointerFocus button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    button.SetPointerFocus(activation, fillActivate);
                }
                PointerVisualizer.Instance.UpdatePointer(start, hit.point);
            }
            else
            {
                PointerVisualizer.Instance.UpdatePointer(start, end);
            }
        }
    }
}
