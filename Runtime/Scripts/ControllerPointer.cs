using UnityEngine;

//this is attached to a controller
//activates a IPointerFocus component. that component must be on the UI layer
//a line renderer is used to display the direction of the controller. the line renderer is transparent while it is not pointing at a IPointerFocus component

//TODO use inputfeature to automatically configure this

namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Internal/Controller Pointer")]
    public class ControllerPointer : MonoBehaviour, IControllerPointer
    {
        [HideInInspector]
        public Material DefaultPointerMat;

        [HideInInspector]
        public bool isRightHand;

        private bool focused;
        private Vector3[] pointsArray;
        private LineRenderer lr;
        private GameplayReferences.TrackingType currentTrackedDevice;
        private Transform controllerAnchor;
        private const float DEFAULT_LENGTH_FOR_LINE_RENDERER = 20;
        
        /// <summary>
        /// True if the button should be activated (trigger or pinch past threshold) <br/>
        /// Passed into SetPointerFocus
        /// </summary>
        private bool activation = false;

        /// <summary>
        /// True if button should fill to activate (like in HMDPointer) <br/>
        /// Set to true if hands; false if controller <br/>
        /// Passed into SetPointerFocus
        /// </summary>
        private bool fillActivate = true;

#if C3D_OCULUS
        private OVRHand hand;
#endif


        /// <summary>
        /// Creates and sets up a line renderer to visualize where user is pointing
        /// </summary>
        /// <param name="transform">The controller anchor where the ray starts</param>
        public void ConstructDefaultLineRenderer(Transform transform)
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.03f;
            lr.useWorldSpace = true;
            pointsArray = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 0, DEFAULT_LENGTH_FOR_LINE_RENDERER) };
            lr.SetPositions(pointsArray);
            if (DefaultPointerMat == null)
            {
                DefaultPointerMat = Resources.Load<Material>("ExitPollPointerLine");
            }
            lr.material = DefaultPointerMat;
            lr.textureMode = LineTextureMode.Tile;
            controllerAnchor = transform;
        }

        void Update()
        {
            Vector3 raycastStartPos = Vector3.zero;
            Vector3 raycastDir = Vector3.zero;
            currentTrackedDevice = GameplayReferences.GetCurrentTrackedDevice();

            // User using hands: set up to use hands
            if (currentTrackedDevice == GameplayReferences.TrackingType.Hand)
            {
                #if C3D_OCULUS
                    if (hand == null)
                    {
                        hand = controllerAnchor.GetComponentInChildren<OVRHand>();
                    }
                    if (hand != null)
                    {
                        pointsArray[0] = Cognitive3D_Manager.Instance.trackingSpace.TransformPoint(hand.PointerPose.position);
                        pointsArray[1] = Cognitive3D_Manager.Instance.trackingSpace.TransformPoint(hand.PointerPose.forward * DEFAULT_LENGTH_FOR_LINE_RENDERER);
                        raycastStartPos = pointsArray[0];
                        raycastDir = hand.PointerPose.forward;
                        lr.SetPositions(pointsArray);
                        activation = (hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) == 1) && (hand.HandConfidence == OVRHand.TrackingConfidence.High);
                        fillActivate = true;
                    }
                #endif
            }
            else if (currentTrackedDevice == GameplayReferences.TrackingType.Controller)
            {
                float currentControllerTrigger = isRightHand ? GameplayReferences.rightTriggerValue : GameplayReferences.leftTriggerValue;
                activation = currentControllerTrigger > 0.5;
                fillActivate = false;
                pointsArray[0] = controllerAnchor.position;
                pointsArray[1] = pointsArray[0] + controllerAnchor.forward * DEFAULT_LENGTH_FOR_LINE_RENDERER;
                raycastStartPos = controllerAnchor.position;
                raycastDir = controllerAnchor.forward;
                lr.SetPositions(pointsArray);
            }

            IPointerFocus button = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(raycastStartPos, raycastDir, out hit, DEFAULT_LENGTH_FOR_LINE_RENDERER, LayerMask.GetMask("UI"))) // hit a button
            {
                button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    button.SetPointerFocus(isRightHand, activation, fillActivate);
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