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
        private bool focused;
        public bool DisplayLineRenderer = true;
        public LineRenderer LineRendererOverride;
        Vector3[] pointsArray;

        LineRenderer lr;
        public bool isRightHand;

        public LineRenderer ConstructDefaultLineRenderer()
        {
            lr = gameObject.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.widthMultiplier = 0.03f;
            pointsArray = new Vector3[] { new Vector3(0, 0, 0), new Vector3(0, 0, 20) };
            lr.SetPositions(pointsArray);
            if (DefaultPointerMat == null)
            {
                DefaultPointerMat = Resources.Load<Material>("ExitPollPointerLine");
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
                    button.SetPointerFocus(isRightHand);
                    lr.material = FocusPointerMat;
                    lr.textureMode = LineTextureMode.Tile;
                    Vector3[] hitPointsArray = { new Vector3(0, 0, 0), new Vector3(0, 0, hit.distance) };
                    lr.SetPositions(hitPointsArray);
                    focused = true;
                }
                else
                {
                    if (focused)
                    {
                        ResetLineRenderer();
                        focused = false;
                    }
                }
            }
            else
            {
                if (focused)
                {
                    ResetLineRenderer();
                    focused = false;
                }
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