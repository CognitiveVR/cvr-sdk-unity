using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//displays completed fixation points using particles

//#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Testing/Fixation Visualizer")]
    [System.Obsolete("Obsolete - Use Active Session View instead")]
    public class FixationVisualizer : MonoBehaviour
    {
        FixationRecorder fixationRecorder;

        [Header("Visualization")]
        public Mesh FixationMesh;
        //public Material FixationMaterial;

        public Material FixationMaterialDiscard;
        public Material FixationMaterialRange;
        public Material FixationMaterialSleep;
        public Material FixationMaterialTransform;
        public Material FixationMaterialDynamic;

        public void SetTarget(FixationRecorder target)
        {
            fixationRecorder = target;
        }

        Dictionary<string, DynamicObject> dynamicObjects = new Dictionary<string, DynamicObject>();

        public bool display = true;

        //1 record old eye capture to fixation
        //2 check if that fixation ended
        //3 queue new(current) eye capture
        void Update()
        {
            if (!display) { return; }
            if (fixationRecorder == null){return;}

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
                Material mat = FixationMaterialRange;

                if (v.IsLocal)
                {
                    DynamicObject dyn;
                    if (dynamicObjects.TryGetValue(v.DynamicObjectId, out dyn))
                    {

                    }
                    else
                    {
                        var all = FindObjectsOfType<DynamicObject>();
                        foreach(var d in all)
                        {
                            if (dynamicObjects.ContainsKey(d.DataId)) { continue; }
                            else dynamicObjects.Add(d.DataId, d);
                        }
                        if (dynamicObjects.TryGetValue(v.DynamicObjectId, out dyn))
                        {
                            
                        }
                    }
                    if (dyn != null)
                    {
                        scale = Vector3.one * v.DebugScale;
                        var worldPos = dyn.transform.TransformPoint(v.LocalPosition);

                        m = Matrix4x4.TRS(worldPos, Quaternion.identity, scale);
                        mat = FixationMaterialDynamic;
                    }
                }
                
                Graphics.DrawMesh(FixationMesh, m, mat, 0);
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
//#endif