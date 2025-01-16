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

        private ExitPollManager.PointerType pointerType;
        private ExitPollManager.PointerInputButton pointerInputButton;
        private GameObject pointerOverride;

#if C3D_OCULUS
        private List<OVRHand> hands = new List<OVRHand>();
        private OVRHand activeHand;
#endif

        internal void SetPointerType(ExitPollManager.PointerType pointer, ExitPollManager.PointerInputButton inputButton)
        {
            pointerType = pointer;
            pointerInputButton = inputButton;
        }

        internal void SetPointerType(ExitPollManager.PointerType pointer, GameObject customPointer = null)
        {
            pointerType = pointer;

            if (customPointer)
            {
                pointerOverride = customPointer;
            }
        }

        void Update()
        {
            if (pointerType == ExitPollManager.PointerType.ControllersAndHands)
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
            else // Custom pointer
            {
                HandleCustomPointer();
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
            // Determine which hand is active based on button press
            bool isRightHandActive = ExitPollUtil.GetButtonState(UnityEngine.XR.XRNode.RightHand, ExitPollUtil.GetButtonFeature(pointerInputButton));
            bool isLeftHandActive = ExitPollUtil.GetButtonState(UnityEngine.XR.XRNode.LeftHand, ExitPollUtil.GetButtonFeature(pointerInputButton));

            if (isRightHandActive && !isRightHand)
            {
                isRightHand = true; // Switch to right hand
            }
            else if (isLeftHandActive && isRightHand)
            {
                isRightHand = false; // Switch to left hand
            }

            // Set active controller based on which button was pressed
            UnityEngine.XR.XRNode activeController = isRightHand ? UnityEngine.XR.XRNode.RightHand : UnityEngine.XR.XRNode.LeftHand;
            Vector3 controllerPosition;
            Quaternion controllerRotation;
            GameplayReferences.TryGetControllerPosition(activeController, out controllerPosition);
            GameplayReferences.TryGetControllerRotation(activeController, out controllerRotation);

            Vector3 direction = controllerRotation * Vector3.forward;
            bool activation = ExitPollUtil.GetButtonState(activeController, ExitPollUtil.GetButtonFeature(pointerInputButton));

            UpdatePointer(controllerPosition, direction, activation, false);
        }

        private void HandleCustomPointer()
        {
            if (pointerOverride)
            {
                Vector3 direction = pointerOverride.transform.rotation * Vector3.forward;
                UpdatePointer(pointerOverride.transform.position, direction);
            }
        }

        private void UpdatePointer(Vector3 start, Vector3 direction)
        {
            Vector3 end = start + direction * DEFAULT_LENGTH_FOR_POINTER;

            if (Physics.Raycast(start, direction, out RaycastHit hit, DEFAULT_LENGTH_FOR_POINTER, LayerMask.GetMask("UI")))
            {
                PointerVisualizer.Instance.UpdatePointer(start, hit.point);
            }
            else
            {
                PointerVisualizer.Instance.UpdatePointer(start, end);
            }
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
