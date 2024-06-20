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

#if C3D_OCULUS
       private OVRHand hand;
#endif

        public LineRenderer ConstructDefaultLineRenderer(Transform attachTransform)
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
            controllerAnchor = attachTransform;
            return lr;
        }

        void Update()
        {
            bool activation = false;
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
                        lr.SetPositions(pointsArray);
                        activation = (hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) == 1) && (hand.HandConfidence == OVRHand.TrackingConfidence.High);
                    }
                #endif
            }
            else if (currentTrackedDevice == GameplayReferences.TrackingType.Controller)
            {
                float currentControllerTrigger = isRightHand ? GameplayReferences.rightTriggerValue : GameplayReferences.leftTriggerValue;
                activation = currentControllerTrigger > 0.5;
                pointsArray[0] = controllerAnchor.position;
                pointsArray[1] = pointsArray[0] + controllerAnchor.forward * DEFAULT_LENGTH_FOR_LINE_RENDERER;
                lr.SetPositions(pointsArray);
            }

            Vector3 pos = transform.position;
            Vector3 forward = transform.forward;
            IPointerFocus button = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(pos, forward, out hit, 20, LayerMask.GetMask("UI"))) //hit a button
            {
                button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    button.SetPointerFocus(isRightHand, activation);
                    Vector3[] hitPointsArray = { new Vector3(0, 0, 0), new Vector3(0, 0, hit.distance) };
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

        private void ResetLineRenderer()
        {
            lr.material = DefaultPointerMat;
            lr.textureMode = LineTextureMode.Tile;
            lr.SetPositions(pointsArray);
        }
    }
}