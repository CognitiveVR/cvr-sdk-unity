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
        Material DefaultPointerMat;
        Material FocusPointerMat;

        public bool DisplayLineRenderer = true;
        public LineRenderer LineRendererOverride;
        readonly Vector3[] pointsArray = { new Vector3(0, 0, 0), new Vector3(0, 0, 20) };

        [Tooltip("When added to a controller, this offset is applied on start")]
        public Vector3 LocalPositionOffset = new Vector3(0, 0, 0);

        LineRenderer lr;
        private void Start()
        {
            transform.localPosition = LocalPositionOffset;
            if (LineRendererOverride == null && DisplayLineRenderer)
            {
                LineRendererOverride = ConstructDefaultLineRenderer();
                LineRendererOverride.positionCount = 2;
            }
        }

        private LineRenderer ConstructDefaultLineRenderer()
        {
            GameObject go = new GameObject("LineRenderer");
            lr = go.AddComponent<LineRenderer>();
            go.transform.parent = transform;
            lr.transform.localPosition = new Vector3(0, 0, 0);
            lr.SetPositions(pointsArray);
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.03f;
            if (DefaultPointerMat == null)
            {
                DefaultPointerMat = Resources.Load<Material>("ExitPollPointerLine");
            }
            if (FocusPointerMat == null)
            {
                FocusPointerMat = Resources.Load<Material>("ExitPollPointerLine_Focus");
            }
            lr.material = DefaultPointerMat;
            lr.textureMode = LineTextureMode.Tile;
            return lr;
        }

      void Update()
        {
            Vector3 pos = transform.position;
            Vector3 forward = transform.forward;
            IPointerFocus button = null;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(pos, forward, out hit, 20, LayerMask.GetMask("UI"))) //hit a button
            {
                button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    button.SetPointerFocus();
                    lr.material = FocusPointerMat;
                    lr.textureMode = LineTextureMode.Tile;
                    float distance = (hit.point - pos).z;
                    Vector3[] hitPointsArray = { new Vector3(0, 0, 0), new Vector3(0, 0, distance) };
                    lr.SetPositions(hitPointsArray);
                }
                else
                {
                    lr.material = DefaultPointerMat;
                    lr.textureMode = LineTextureMode.Tile;
                    lr.SetPositions(pointsArray);
                }
            }
            else
            {
                lr.material = DefaultPointerMat;
                lr.textureMode = LineTextureMode.Tile;
                lr.SetPositions(pointsArray);
            }
        }
    }
}