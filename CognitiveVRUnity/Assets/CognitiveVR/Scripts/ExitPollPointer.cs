using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//construct a curve between position and target
//sample points along line
//set line renderer

namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Internal/Exit Poll Pointer")]
    public class ExitPollPointer : MonoBehaviour
    {
        //public Transform Target { get; set; }


        static Material DefaultPointerMat;
        public Material PointerMaterialOverride;
        [Tooltip("If no LineRenderer is set, a default one will be created as a child")]
        public LineRenderer LineRendererOverride;


        [Tooltip("Controls how fast the curve bends to the target")]
        float ForwardPower = 2;

        [Tooltip("Higher requires more accurate pointing.\nLower allows more flexibility after initial point")]
        [Range(0.1f, 1f)]
        float Stiffness = 0.98f;

        Transform _t;
        Vector3[] sampledPoints;
        Vector3[] curve = new Vector3[4] { Vector3.zero, Vector3.forward * 1, Vector3.forward * 2, Vector3.forward * 3 };

        [Tooltip("How many points along the curve to sample. Can lead to a smoother line renderer")]
        public int SampleResolution = 10;

        private void Start()
        {
            if (_t != null) return;
            sampledPoints = new Vector3[SampleResolution + 1];
            _t = transform;
            if (LineRendererOverride == null)
                LineRendererOverride = ConstructDefaultLineRenderer();
            LineRendererOverride.positionCount = SampleResolution;
        }

        private LineRenderer ConstructDefaultLineRenderer()
        {
            GameObject go = new GameObject("LineRenderer");
            go.transform.parent = _t;
            var lr = go.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.05f;
            lr.useWorldSpace = true;
            if (DefaultPointerMat == null)
            {
                DefaultPointerMat = Resources.Load<Material>("ExitPollPointerLine");
            }
            if (PointerMaterialOverride == null)
            {
                lr.material = DefaultPointerMat;
                lr.textureMode = LineTextureMode.Tile;
            }
            else
            {
                lr.material = PointerMaterialOverride;
            }
            return lr;
        }

        Transform lastTarget = null;
        GazeButton lastButton = null;
        float lastHasButtonTime = 10;

        //sets the curve to the target
        void Update()
        {
            Vector3 pos = _t.position;
            Vector3 forward = _t.forward;
            
            bool hitButton = false;
            bool hasAnyButton = false;

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(pos,forward, out hit, 10, LayerMask.GetMask("UI"))) //hit a button
            {
                var button = hit.collider.GetComponent<GazeButton>();
                if (button != null)
                {
                    hitButton = true;
                    hasAnyButton = true;
                    //bend the line renderer over to here
                    lastTarget = button.transform;
                    lastButton = button;
                    curve[0] = pos;
                    curve[1] = pos + forward * ForwardPower;
                    curve[2] = lastTarget.position;
                    curve[3] = lastTarget.position;
                    button.SetFocus();
                }
            }

            if (hitButton)
            {
                //everything is set above
            }
            else if (lastTarget != null && lastButton != null) //direction roughly towards a previous button
            {
                if (Vector3.Dot(forward,(lastTarget.position - pos).normalized)>Stiffness)
                {
                    hasAnyButton = true;
                    //still in direction
                    curve[0] = pos;
                    curve[1] = pos + forward * ForwardPower;
                    curve[2] = lastTarget.position;
                    curve[3] = lastTarget.position;
                    lastButton.SetFocus();
                }
                else
                {
                    lastTarget = null;
                    lastButton = null;
                    lastHasButtonTime = 0;
                }
            }
            
            if (!hasAnyButton)
            {
                lastHasButtonTime += Time.deltaTime * 2;

                curve[0] = pos;
                curve[1] = pos + forward * ForwardPower;

                //lerp
                curve[2] = Vector3.Lerp(curve[2], pos + forward * ForwardPower, lastHasButtonTime * 2);
                curve[3] = Vector3.Lerp(curve[3], pos + forward * ForwardPower * 2, lastHasButtonTime);

                //snap
                //curve[2] = pos + forward * ForwardPower;
                //curve[3] = pos + forward * ForwardPower * 2;
            }

            LineRendererOverride.SetPositions(EvaluatePoints(SampleResolution));
        }

        //sample points along the curve
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

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 5);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.right * 0.3f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.up * 0.3f);

            if (sampledPoints == null) { return; }
            for (int j = 0; j < sampledPoints.Length - 1; j++)
            {
                Gizmos.DrawLine(sampledPoints[j], sampledPoints[j + 1]);
            }
        }
    }
}