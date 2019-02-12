using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//displays completed fixation points using particles

#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace CognitiveVR
{
    public class FixationVisualizer : MonoBehaviour
    {
        FixationRecorder fixationRecorder;

        [Header("Visualization")]
        public Mesh FixationMesh;
        public Material FixationMaterial;

        public Material FixationMaterialDiscard;
        public Material FixationMaterialRange;
        public Material FixationMaterialSleep;
        public Material FixationMaterialTransform;

        public void SetTarget(FixationRecorder target)
        {
            fixationRecorder = target;
        }

        //1 record old eye capture to fixation
        //2 check if that fixation ended
        //3 queue new(current) eye capture
        void Update()
        {
            foreach (var v in fixationRecorder.VISFixationEnds["discard"])
            {
                Vector3 scale = Vector3.one * v.DebugScale;
                Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                Graphics.DrawMesh(FixationMesh, m, FixationMaterialDiscard, 0);
            }
            foreach (var v in fixationRecorder.VISFixationEnds["out of range"])
            {
                Vector3 scale = Vector3.one * v.DebugScale;
                Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                Graphics.DrawMesh(FixationMesh, m, FixationMaterialRange, 0);
            }
            foreach (var v in fixationRecorder.VISFixationEnds["microsleep"])
            {
                Vector3 scale = Vector3.one * v.DebugScale;
                Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                Graphics.DrawMesh(FixationMesh, m, FixationMaterialSleep, 0);
            }
            foreach (var v in fixationRecorder.VISFixationEnds["off transform"])
            {
                Vector3 scale = Vector3.one * v.DebugScale;
                Matrix4x4 m = Matrix4x4.TRS(v.WorldPosition, Quaternion.identity, scale);
                Graphics.DrawMesh(FixationMesh, m, FixationMaterialTransform, 0);
            }
        }
    }
}
#endif