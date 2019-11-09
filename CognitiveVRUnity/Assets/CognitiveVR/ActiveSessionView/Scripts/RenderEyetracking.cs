using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//draws saccade lines and fixation spheres for target camera

namespace CognitiveVR.ActiveSession
{
    public class RenderEyetracking : MonoBehaviour
    {
        public float FixationScale = 0.2f;
        public Mesh FixationMesh;
        public Material FixationMaterial;
        public Material SaccadeMaterial;
        public Camera TargetCamera;

        public int FixationRenderLayer = 3; //unnamed internal layer
        public Color FixationColor;

        CognitiveVR.FixationRecorder fixationRecorder;
        public float Width = 0.03f;
        bool displayFixations = false;

        private void Start()
        {
            TargetCamera = GetComponent<Camera>();
            fixationRecorder = FixationRecorder.Instance;
            FixationMaterial.color = FixationColor;

            if (Core.IsInitialized)
            {
                if (fixationRecorder != null)
                {
                    displayFixations = true;
                    FixationCore.OnFixationRecord += FixationCore_OnFixationRecord;
                }
            }
            else
            {
                Core.InitEvent += Core_InitEvent;
            }
        }

        private void Core_InitEvent(Error initError)
        {
            Core.InitEvent -= Core_InitEvent;

            fixationRecorder = FixationRecorder.Instance;
            if (initError == Error.None && fixationRecorder != null)
            {
                displayFixations = true;
                FixationCore.OnFixationRecord += FixationCore_OnFixationRecord;
            }
        }

        Queue<Fixation> fixationQueue = new Queue<Fixation>(50);
        private void FixationCore_OnFixationRecord(Fixation fixation)
        {
            if (fixationQueue.Count > 49)
                fixationQueue.Dequeue();
            fixationQueue.Enqueue(new Fixation(fixation));
        }

        void Update()
        {
            if (!displayFixations) { return; }

            Vector3 scale = Vector3.one * FixationScale;

            //on new fixation
            foreach (Fixation f in fixationQueue)
            {
                if (f.IsLocal && f.LocalTransform != null)
                {
                    Matrix4x4 m = Matrix4x4.TRS(f.LocalTransform.TransformPoint(f.LocalPosition), Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, TargetCamera);
                }
                else
                {
                    Matrix4x4 m = Matrix4x4.TRS(f.WorldPosition, Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, TargetCamera);
                }
            }
        }

        private void OnPostRender()
        {
            if (!displayFixations) { return; }

            //draw saccade lines
            SaccadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(TargetCamera.projectionMatrix);
            GL.modelview = TargetCamera.worldToCameraMatrix;

            GL.Begin(GL.QUADS);

            var forward = CognitiveVR.GameplayReferences.HMD.forward;
            var offsetDir = Vector3.one;

            int count = fixationRecorder.DisplayGazePoints.Length;
            try
            {
                Vector3 previousPoint;
                //start to current
                previousPoint = fixationRecorder.DisplayGazePoints[0];
                for (int i = 1; i < fixationRecorder.currentGazePoint; i++)
                {
                    Vector3 currentPoint = fixationRecorder.DisplayGazePoints[i];
                    GL.Vertex3(previousPoint.x - offsetDir.x * Width, previousPoint.y - offsetDir.y * Width, previousPoint.z - offsetDir.z * Width);
                    GL.Vertex3(previousPoint.x + offsetDir.x * Width, previousPoint.y + offsetDir.y * Width, previousPoint.z + offsetDir.z * Width);
                    GL.Vertex3(currentPoint.x + offsetDir.x * Width, currentPoint.y + offsetDir.y * Width, currentPoint.z + offsetDir.z * Width);
                    GL.Vertex3(currentPoint.x - offsetDir.x * Width, currentPoint.y - offsetDir.y * Width, currentPoint.z - offsetDir.z * Width);

                    previousPoint = currentPoint;
                }

                //current to end
                if (fixationRecorder.currentGazePoint == 0 || fixationRecorder.DisplayGazePointBufferFull)
                {
                    previousPoint = fixationRecorder.DisplayGazePoints[fixationRecorder.currentGazePoint];
                    //current gaze point to end, then start to current gaze point.
                    for (int i = fixationRecorder.currentGazePoint + 1; i < count; i++)
                    {
                        Vector3 currentPoint = fixationRecorder.DisplayGazePoints[i];
                        GL.Vertex3(previousPoint.x - offsetDir.x * Width, previousPoint.y - offsetDir.y * Width, previousPoint.z - offsetDir.z * Width);
                        GL.Vertex3(previousPoint.x + offsetDir.x * Width, previousPoint.y + offsetDir.y * Width, previousPoint.z + offsetDir.z * Width);
                        GL.Vertex3(currentPoint.x + offsetDir.x * Width, currentPoint.y + offsetDir.y * Width, currentPoint.z + offsetDir.z * Width);
                        GL.Vertex3(currentPoint.x - offsetDir.x * Width, currentPoint.y - offsetDir.y * Width, currentPoint.z - offsetDir.z * Width);
                        previousPoint = currentPoint;
                    }
                    //last point to first point
                    {
                        Vector3 currentPoint = fixationRecorder.DisplayGazePoints[0];
                        GL.Vertex3(previousPoint.x - offsetDir.x * Width, previousPoint.y - offsetDir.y * Width + 0.1f, previousPoint.z - offsetDir.z * Width);
                        GL.Vertex3(previousPoint.x + offsetDir.x * Width, previousPoint.y + offsetDir.y * Width + 0.1f, previousPoint.z + offsetDir.z * Width);
                        GL.Vertex3(currentPoint.x + offsetDir.x * Width, currentPoint.y + offsetDir.y * Width + 0.1f, currentPoint.z + offsetDir.z * Width);
                        GL.Vertex3(currentPoint.x - offsetDir.x * Width, currentPoint.y - offsetDir.y * Width + 0.1f, currentPoint.z - offsetDir.z * Width);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            GL.End();
            GL.PopMatrix();
        }

        void OnDestroy()
        {
            FixationCore.OnFixationRecord -= FixationCore_OnFixationRecord;
        }
    }
}