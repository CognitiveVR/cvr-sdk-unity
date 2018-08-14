using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//construct a curve between position and target
//sample points along line
//set line renderer

//the actual functionality is on gaze buttons and microphone buttons
namespace CognitiveVR
{
    public class ExitPollPointer : MonoBehaviour
    {
        public Transform Target { get; set; }

        [Tooltip("Controls how fast the curve bends to the target")]
        public float ForwardPower = 2;

        [Tooltip("Higher requires more accurate pointing.\nLower allows more flexibility after initial point")]
        [Range(0.1f, 1f)]
        public float Stiffness = 0.95f;

        Transform _t;
        Vector3[] sampledPoints;
        Vector3[] curve = new Vector3[4] { Vector3.zero, Vector3.forward * 1, Vector3.forward * 2, Vector3.forward * 3 };

        [Tooltip("If no LineRenderer is set, a default one will be created as a child")]
        public LineRenderer LineRenderer;

        [Tooltip("How many points along the curve to sample. Can lead to a smoother line renderer")]
        public int SampleResolution = 10;

        private bool visible = false;
        private void Start()
        {
            if (_t != null) return;
            sampledPoints = new Vector3[SampleResolution + 1];
            _t = transform;
            if (LineRenderer == null)
                LineRenderer = ConstructDefaultLineRenderer();
            LineRenderer.positionCount = SampleResolution;
            SetVisible(false);
        }

        //controls visibility. set when exitpoll begins and completes
        public void SetVisible(bool visible)
        {
            if (_t == null) Start();
            this.visible = visible;
            LineRenderer.enabled = visible;
        }

        static Material PointerMat;

        private LineRenderer ConstructDefaultLineRenderer()
        {
            GameObject go = new GameObject("LineRenderer");
            go.transform.parent = _t;
            var lr = go.AddComponent<LineRenderer>();
            lr.widthMultiplier = 0.05f;
            lr.useWorldSpace = true;
            if (PointerMat == null)
            {
                PointerMat = Resources.Load<Material>("ExitPollPointerLine");
            }
            if (PointerMat != null)
                lr.material = PointerMat;
            else
                lr.material = new Material(Shader.Find("Standard"));
            lr.textureMode = LineTextureMode.Tile;
            return lr;
        }

        //sets the curve to the target
        void Update()
        {
            if (!visible) { return; }
            Vector3 pos = _t.position;
            Vector3 forward = _t.forward;
            if (Target == null) //TODO straighten over time
            {
                curve[0] = pos;
                curve[1] = pos + forward * ForwardPower;
                curve[2] = pos + forward * ForwardPower;
                curve[3] = pos + forward * ForwardPower * 2;
            }
            else
            {
                curve[0] = pos;
                curve[1] = pos + forward * ForwardPower;
                curve[2] = Target.position;
                curve[3] = Target.position;
            }

            LineRenderer.SetPositions(EvaluatePoints(SampleResolution));
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

                //sampledPoints[i] = 
                //curve[0] * (omNormalDistSqr * omNormalDist) +
                //curve[1] * (3f * omNormalDistSqr * normalDist) +
                //curve[2] * (3f * omNormalDist * normalDistSqr) +
                //curve[3] * (normalDistSqr * normalDist);
            }
            return sampledPoints;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 100);

            if (sampledPoints == null) { return; }
            for (int j = 0; j < sampledPoints.Length - 1; j++)
            {
                Gizmos.DrawLine(sampledPoints[j], sampledPoints[j + 1]);
            }
        }
    }
}