using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class FixationVisualizer : MonoBehaviour
    {

        public bool Visualize;
        FixationRecorder fr;

        [Header("Visualization")]
        public Mesh FixationMesh;
        public Material FixationMaterial;

        public Material FixationMaterialDiscard;
        public Material FixationMaterialRange;
        public Material FixationMaterialSleep;
        public Material FixationMaterialTransform;

        // Use this for initialization
        void Start()
        {
            fr = FindObjectOfType<FixationRecorder>();

        }

        //1 record old eye capture to fixation
        //2 check if that fixation ended
        //3 queue new(current) eye capture
        void Update()
        {
            ////visualization
            if (Visualize)
            {
                foreach (var v in fr.VISFixationEnds["discard"])
                {
                    Vector3 scale = Vector3.one * v.DebugScale;
                    Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterialDiscard, 0);
                }
                foreach (var v in fr.VISFixationEnds["out of range"])
                {
                    Vector3 scale = Vector3.one * v.DebugScale;
                    Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterialRange, 0);
                }
                foreach (var v in fr.VISFixationEnds["microsleep"])
                {
                    Vector3 scale = Vector3.one * v.DebugScale;
                    Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterialSleep, 0);
                }
                foreach (var v in fr.VISFixationEnds["off transform"])
                {
                    Vector3 scale = Vector3.one * v.DebugScale;
                    Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterialTransform, 0);
                }
            }
        }
    }
}