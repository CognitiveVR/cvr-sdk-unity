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
        
        /// <summary>
        /// Defines which controller input (e.g., trigger, primary button) activates the pointer interaction.
        /// This value is used to detect input during exit poll sessions.
        /// </summary>
        private ExitPollManager.PointerInputButton pointerInputButton;

        /// <summary>
        /// Local position offset applied to the controller pointer. 
        /// Use this to adjust the starting position of the pointer relative to the controller.
        /// </summary>
        public static Vector3 PointerPosOffset;

        /// <summary>
        /// Local rotation offset (in degrees) applied to the controller pointer direction. 
        /// Useful for fine-tuning the pointer's forward angle when it doesn't align with the controller's default forward direction.
        /// </summary>
        public static Vector3 PointerRotOffset;

#if C3D_OCULUS
        private List<OVRHand> hands = new List<OVRHand>();
        private OVRHand activeHand;
#endif

        internal void SetPointerType(ExitPollManager.PointerInputButton inputButton)
        {
            pointerInputButton = inputButton;
        }

        void Update()
        {
            var currentTrackedDevice = InputUtil.GetCurrentTrackedDevice();

            if (currentTrackedDevice == InputUtil.InputType.Hand)
            {
                HandleHandInput();
            }
            else if (currentTrackedDevice == InputUtil.InputType.Controller)
            {
                HandleControllerInput();
            }
        }

        private void HandleHandInput()
        {
#if C3D_OCULUS
            if (hands.Count == 0)
            {
                OVRHand[] foundHands = FindObjectsByType<OVRHand>(FindObjectsSortMode.None);
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
            InputUtil.TryGetControllerPosition(activeController, out controllerPosition);
            InputUtil.TryGetControllerRotation(activeController, out controllerRotation);

            // Apply position offset (relative to controller)
            Vector3 position = controllerPosition + controllerRotation * PointerPosOffset;
            // Apply rotation offset
            Vector3 direction = controllerRotation * Quaternion.Euler(PointerRotOffset) * Vector3.forward;
            bool activation = ExitPollUtil.GetButtonState(activeController, ExitPollUtil.GetButtonFeature(pointerInputButton));

            UpdatePointer(position, direction, activation, false);
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

                if (gameObject.GetComponent<PointerVisualizer>())
                {
                    gameObject.GetComponent<PointerVisualizer>().UpdatePointer(start, hit.point);
                }
            }
            else
            {
                if (gameObject.GetComponent<PointerVisualizer>())
                {
                    gameObject.GetComponent<PointerVisualizer>().UpdatePointer(start, end);
                }
            }
        }
    }
}
