﻿using System.Collections;
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
        float Stiffness = 0.95f;

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

        //sets the curve to the target
        void Update()
        {
            Vector3 pos = _t.position;
            Vector3 forward = _t.forward;

            Transform target = null;

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(pos,forward, out hit, 10, LayerMask.GetMask("UI")))
            {
                var button = hit.collider.GetComponent<GazeButton>();
                if (button != null)
                {
                    //bend the line renderer over to here
                    target = button.transform;
                    button.SetFocus();
                }
            }

            if (target == null) //TODO straighten over time
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
                curve[2] = target.position;
                curve[3] = target.position;
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