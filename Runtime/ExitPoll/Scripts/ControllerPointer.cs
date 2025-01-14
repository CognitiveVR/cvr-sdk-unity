using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Cognitive3D
{
    /// <summary>
    /// This is used to interact with exit poll buttons using controllers and/or hands <br/>
    /// Handles interaction via trigger and/or pinch and displays a line renderer showing where the user is pointing
    /// </summary>
    [AddComponentMenu("Cognitive3D/Internal/Controller Pointer")]
    public class ControllerPointer : MonoBehaviour, IControllerPointer
    {
        /// <summary>
        /// True if right hand; false otherwise
        /// </summary>
        [HideInInspector]
        public bool isRightHand;

        /// <summary>
        /// The default material for the line renderer
        /// </summary>
        public Material PointerLineMaterial;

        /// <summary>
        /// True if the raycast is hitting the button; false otherwise
        /// </summary>
        private bool focused;
        
        /// <summary>
        /// 
        /// </summary>
        private Vector3[] pointsArray;
        
        /// <summary>
        /// A reference to the line renderer component
        /// </summary>
        private LineRenderer lr;
        
        /// <summary>
        /// A reference to the controller anchor this pointer will start from
        /// </summary>
        private Transform controllerAnchor;
        
        /// <summary>
        /// The default length for the visual line renderer <br/>
        /// Also used as maximum distance for Raycast
        /// </summary>
        private const float DEFAULT_LENGTH_FOR_LINE_RENDERER = 20;

        /// <summary>
        /// True if button should fill to activate (like in HMDPointer) <br/>
        /// Set to true if hands; false if controller <br/>
        /// Passed into SetPointerFocus
        /// </summary>
        private bool fillActivate = true;

        /// <summary>
        /// Creates and sets up a line renderer to visualize where user is pointing
        /// </summary>
        /// <param name="transform">The controller anchor where the ray starts</param>
        public void ConstructDefaultLineRenderer(Transform transform)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.03f;
            lr.useWorldSpace = true;
            pointsArray = new [] { new Vector3(0, 0, 0), new Vector3(0, 0, DEFAULT_LENGTH_FOR_LINE_RENDERER) };
            lr.SetPositions(pointsArray);
            if (PointerLineMaterial == null)
            {
                PointerLineMaterial = Resources.Load<Material>("ExitPollPointerLine");
            }
            lr.material = PointerLineMaterial;
            lr.textureMode = LineTextureMode.Tile;
            controllerAnchor = transform;
        }

#if C3D_OCULUS
        List<OVRHand> hands = new List<OVRHand>();
        OVRHand activeHand;
#endif

        void Update()
        {
            Vector3 raycastStartPos = Vector3.zero;
            Vector3 raycastDir = Vector3.zero;
            // True if the button should be activated (trigger or pinch past threshold) <br/>
            // Passed into SetPointerFocus
            bool activation = false;
            //A reference to the current tracked device(hand, controller, or none)
            var currentTrackedDevice = GameplayReferences.GetCurrentTrackedDevice();

            // User using hands: set up to use hands
            if (currentTrackedDevice == GameplayReferences.TrackingType.Hand)
            {
#if C3D_OCULUS
                if (hands.Count() == 0)
                {
                    hands = FindObjectsOfType<OVRHand>().ToList();
                }
                else
                {
                    foreach (OVRHand hand in hands)
                    {
                        if (hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) >= 1.0f && hand.HandConfidence == OVRHand.TrackingConfidence.High)
                        {
                            activeHand = hand;
                        }
                    }

                    if (activeHand != null)
                    {
                        pointsArray[0] = OVRManager.instance.transform.TransformPoint(activeHand.PointerPose.position);
                        pointsArray[1] = (OVRManager.instance.transform.rotation * activeHand.PointerPose.rotation) * Vector3.forward * DEFAULT_LENGTH_FOR_LINE_RENDERER;
                        raycastStartPos = pointsArray[0];
                        raycastDir = pointsArray[1];
                        pointsArray[1] += pointsArray[0]; // Adjusting line renderer end position for rigs that move
                        lr.SetPositions(pointsArray);
                        activation = (activeHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) == 1) && (activeHand.HandConfidence == OVRHand.TrackingConfidence.High);
                        fillActivate = true;
                    }
                }
#endif
            }
            // User using controller
            else if (currentTrackedDevice == GameplayReferences.TrackingType.Controller)
            {
                // Check trigger values for both controllers
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

                // Set activation and other properties based on the active controller
                float currentControllerTrigger = isRightHand ? rightTriggerValue : leftTriggerValue;
                activation = currentControllerTrigger > 0.5f;
                fillActivate = false;

                Vector3 controllerPosition;
                Quaternion controllerRotation;
                Vector3 controllerForward;
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

                controllerForward = controllerRotation * Vector3.forward;
                pointsArray[0] = controllerPosition;
                pointsArray[1] = pointsArray[0] + controllerForward * DEFAULT_LENGTH_FOR_LINE_RENDERER;
                raycastStartPos = controllerPosition;
                raycastDir = controllerForward;
                lr.SetPositions(pointsArray);
            }

            IPointerFocus button = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(raycastStartPos, raycastDir, out hit, DEFAULT_LENGTH_FOR_LINE_RENDERER, LayerMask.GetMask("UI"))) // hit a button
            {
                button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    button.SetPointerFocus(activation, fillActivate);
                    Vector3[] hitPointsArray = { pointsArray[0], hit.point };
                    lr.SetPositions(hitPointsArray);
                    focused = true;
                    return;
                }
            }
            
            // Transition from focused to not focused
            if (focused)
            {
                ResetLineRenderer();
                focused = false;
            }
        }

        /// <summary>
        /// Resets line renderer to default start and end positions
        /// </summary>
        private void ResetLineRenderer()
        {
            lr.SetPositions(pointsArray);
        }
    }
}