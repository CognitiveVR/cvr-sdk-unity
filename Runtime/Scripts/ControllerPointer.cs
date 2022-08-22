using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this is attached to a controller
//activates a IPointerFocus component. that component must be on the UI layer
//line color lerps to transparent is not pointing at IPointerFocus component. this uses _TintColor

namespace CognitiveVR
{
    public class ControllerPointer : MonoBehaviour, IControllerPointer
    {
        static Material DefaultPointerMat;
        public static bool ForceLineVisible;

        public bool DisplayLineRenderer = true;
        public LineRenderer LineRendererOverride;

        //pointer curved line rendering
        float ForwardPower = 2;
        Vector3[] sampledPoints;
        Vector3[] curve = new Vector3[4] { Vector3.zero, Vector3.forward * 1, Vector3.forward * 2, Vector3.forward * 3 };

        [Tooltip("How many points along the curve to sample. Can lead to a smoother line renderer")]
        public int SampleResolution = 10;
        [Tooltip("The angle from this transform that should indicate 'forward'")]
        public Vector3 Angle = new Vector3(0, 0, 1);
        [Tooltip("When added to a controller, this offset is applied on start")]
        public Vector3 LocalPositionOffset = new Vector3(0, 0, 0);
        [Tooltip("If true, requires the HMD to be roughly pointed at a button to set focus")]
        public bool RequireHMDParallel = true;

        private void Start()
        {
            sampledPoints = new Vector3[SampleResolution + 1];
            transform.localPosition = LocalPositionOffset;
            if (LineRendererOverride == null && DisplayLineRenderer)
            {
                LineRendererOverride = ConstructDefaultLineRenderer();
                LineRendererOverride.positionCount = 5;
            }
        }

        private LineRenderer ConstructDefaultLineRenderer()
        {
            GameObject go = new GameObject("LineRenderer");
            go.transform.parent = transform;
            var lr = go.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.03f;
            lr.useWorldSpace = true;
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
            Vector3 dir = transform.TransformDirection(Angle);
            if (UpdateDrawLine(dir) != null || ForceLineVisible)
            {
                if (LineRendererOverride != null)
                    LineRendererOverride.material.SetColor("_TintColor", new Color(1, 1, 1, 0.16f));
            }
            else
            {
                if (LineRendererOverride != null)
                {
                    var c = LineRendererOverride.material.GetColor("_TintColor");
                    LineRendererOverride.material.SetColor("_TintColor", Color.Lerp(c, new Color(1, 1, 1, 0.0f), 0.1f)); //fade line out over time
                }
            }
        }

        IPointerFocus UpdateDrawLine(Vector3 direction)
        {
            Vector3 pos = transform.position;
            Vector3 forward = direction;

            bool hasAnyButton = false;
            IPointerFocus button = null;

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(pos, forward, out hit, 20, LayerMask.GetMask("UI"))) //hit a button
            {
                button = hit.collider.GetComponent<IPointerFocus>();
                if (button != null)
                {
                    bool validFocusButton = true;
                    if (RequireHMDParallel)
                    {
                        Vector3 dir = (button.GetPosition() - CognitiveVR.GameplayReferences.HMD.position).normalized;
                        float d = Vector3.Dot(CognitiveVR.GameplayReferences.HMD.forward, dir);
                        if (d < 0.87f)
                            validFocusButton = false;
                    }

                    if (validFocusButton)
                    {
                        hasAnyButton = true;
                        //bend the line renderer over to here
                        curve[0] = pos;
                        curve[1] = pos + forward * ForwardPower;
                        curve[2] = button.GetPosition();
                        curve[3] = button.GetPosition();
                        button.SetPointerFocus();
                    }
                    else
                    {
                        button = null;
                    }
                }
            }

            if (!hasAnyButton || ForceLineVisible)
            {
                //lerp pointer curve forward over time
                curve[0] = pos;
                curve[1] = pos + forward * ForwardPower;
                curve[2] = Vector3.Lerp(curve[2], pos + forward * ForwardPower, 0.5f);
                curve[3] = Vector3.Lerp(curve[3], pos + forward * ForwardPower * 2, 0.5f);
            }

            if (LineRendererOverride != null)
                LineRendererOverride.SetPositions(EvaluatePoints(SampleResolution));
            return button;
        }

        private Vector3[] EvaluatePoints(int sectionCount)
        {
            for (int i = 0; i <= sectionCount; i++)
            {
                float normalDist = i / (float)sectionCount;

                float omNormalDist = 1f - normalDist;
                float omNormalDistSqr = omNormalDist * omNormalDist;
                float normalDistSqr = normalDist * normalDist;

                sampledPoints[i] = new Vector3(curve[0].x * (omNormalDistSqr * omNormalDist) +
                    curve[1].x * (3f * omNormalDistSqr * normalDist) +
                    curve[2].x * (3f * omNormalDist * normalDistSqr) +
                    curve[3].x * (normalDistSqr * normalDist),
                    curve[0].y * (omNormalDistSqr * omNormalDist) +
                    curve[1].y * (3f * omNormalDistSqr * normalDist) +
                    curve[2].y * (3f * omNormalDist * normalDistSqr) +
                    curve[3].y * (normalDistSqr * normalDist),
                    curve[0].z * (omNormalDistSqr * omNormalDist) +
                    curve[1].z * (3f * omNormalDistSqr * normalDist) +
                    curve[2].z * (3f * omNormalDist * normalDistSqr) +
                    curve[3].z * (normalDistSqr * normalDist));
            }
            return sampledPoints;
        }
    }
}