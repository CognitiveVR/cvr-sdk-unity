using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//draws saccade lines and fixation spheres for target camera

namespace CognitiveVR.ActiveSession
{
    public class RenderEyetracking : MonoBehaviour
    {
        public int FixationRenderLayer = 3; //unnamed internal layer
        public int Mask = 8;
        
        public float FixationScale = 0.2f;
        public Mesh FixationMesh;
        public Material FixationMaterial;
        public Color FixationColor;
        
        public Material SaccadeMaterial;
        public float Width = 0.03f;

        CognitiveVR.FixationRecorder fixationRecorder;
        CognitiveVR.GazeBase gazeBase;
        Camera FixationCamera;
        Transform TargetCameraTransform;
        Camera FollowCamera;
        
        public float LerpPositionSpeed = 1.0f;
        public float LerpRotationSpeed = 1.0f;

        bool displayFixations = false;
        bool displayGaze = false;

        Queue<Fixation> fixationQueue = new Queue<Fixation>(50);

        public void Initialize(Camera followCamera)
        {
            FollowCamera = followCamera;
            TargetCameraTransform = followCamera.transform;
            FixationCamera = GetComponent<Camera>();
            fixationRecorder = FixationRecorder.Instance;
            gazeBase = FindObjectOfType<GazeBase>();
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
            FixationCamera.cullingMask = Mask;
#if CVR_FOVE
            //just fully render the camera to be drawn on canvas
            FixationCamera.clearFlags = CameraClearFlags.Skybox;
            FixationCamera.cullingMask = -1;
#endif
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
            else
            {
                if (gazeBase == null)
                    gazeBase = FindObjectOfType<GazeBase>();
                displayGaze = true;
            }
        }

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
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, FixationCamera);
                }
                else
                {
                    Matrix4x4 m = Matrix4x4.TRS(f.WorldPosition, Quaternion.identity, scale);
                    Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, FixationCamera);
                }
            }
        }

        void MatchTargetCamera()
        {
#if CVR_STEAMVR || CVR_STEAMVR2
            var vm = Valve.VR.OpenVR.System.GetProjectionMatrix(Valve.VR.EVREye.Eye_Left, FixationCamera.nearClipPlane, FixationCamera.farClipPlane);
            Matrix4x4 m = new Matrix4x4();
            m.m00 = vm.m0;
            m.m01 = vm.m1;
            m.m02 = vm.m2;
            m.m03 = vm.m3;
            m.m10 = vm.m4;
            m.m11 = vm.m5;
            m.m12 = vm.m6;
            m.m13 = vm.m7;
            m.m20 = vm.m8;
            m.m21 = vm.m9;
            m.m22 = vm.m10;
            m.m23 = vm.m11;
            m.m30 = vm.m12;
            m.m31 = vm.m13;
            m.m32 = vm.m14;
            m.m33 = vm.m15;

            FixationCamera.projectionMatrix = m;
#else
            FixationCamera.projectionMatrix = FollowCamera.projectionMatrix;
#endif
        }

        void LateUpdate()
        {
            if (!displayFixations && !displayGaze) { return; }
            if (TargetCameraTransform == null) { return; }
            MatchTargetCamera();
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, TargetCameraTransform.position, LerpPositionSpeed), Quaternion.Lerp(transform.rotation, TargetCameraTransform.rotation, LerpRotationSpeed));
        }

        private void OnPostRender()
        {
            if (!displayFixations && !displayGaze) { return; }

            //draw saccade lines
            SaccadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(FixationCamera.projectionMatrix);
            GL.modelview = FixationCamera.worldToCameraMatrix;

            var forward = CognitiveVR.GameplayReferences.HMD.forward;
            var offsetDir = Vector3.one * Width;

            if (displayFixations)
            {
                GL.Begin(GL.QUADS);
                int count = fixationRecorder.DisplayGazePoints.Length;
                try
                {
                    GazeBase.GazePoint previousPoint = fixationRecorder.DisplayGazePoints[0];
                    //start to current
                    for (int i = 1; i < fixationRecorder.currentGazePoint; i++)
                    {
                        if (previousPoint.IsLocal && previousPoint.Transform != null)
                        {
                            Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                            GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                            GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        }
                        else
                        {
                            GL.Vertex3(previousPoint.WorldPoint.x - offsetDir.x, previousPoint.WorldPoint.y - offsetDir.y, previousPoint.WorldPoint.z - offsetDir.z);
                            GL.Vertex3(previousPoint.WorldPoint.x + offsetDir.x, previousPoint.WorldPoint.y + offsetDir.y, previousPoint.WorldPoint.z + offsetDir.z);
                        }

                        GazeBase.GazePoint currentPoint = fixationRecorder.DisplayGazePoints[i];

                        if (previousPoint.IsLocal && previousPoint.Transform != null)
                        {
                            Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                            GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                            GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        }
                        else
                        {
                            GL.Vertex3(currentPoint.WorldPoint.x + offsetDir.x, currentPoint.WorldPoint.y + offsetDir.y, currentPoint.WorldPoint.z + offsetDir.z);
                            GL.Vertex3(currentPoint.WorldPoint.x - offsetDir.x, currentPoint.WorldPoint.y - offsetDir.y, currentPoint.WorldPoint.z - offsetDir.z);
                        }

                        previousPoint = currentPoint;
                    }

                    //current to end
                    if (fixationRecorder.currentGazePoint == 0 || fixationRecorder.DisplayGazePointBufferFull)
                    {
                        previousPoint = fixationRecorder.DisplayGazePoints[fixationRecorder.currentGazePoint];
                        //current gaze point to end, then start to current gaze point.
                        for (int i = fixationRecorder.currentGazePoint + 1; i < count; i++)
                        {
                            if (previousPoint.IsLocal && previousPoint.Transform != null)
                            {
                                Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                                GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                                GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                            }
                            else
                            {
                                GL.Vertex3(previousPoint.WorldPoint.x - offsetDir.x, previousPoint.WorldPoint.y - offsetDir.y, previousPoint.WorldPoint.z - offsetDir.z);
                                GL.Vertex3(previousPoint.WorldPoint.x + offsetDir.x, previousPoint.WorldPoint.y + offsetDir.y, previousPoint.WorldPoint.z + offsetDir.z);
                            }

                            GazeBase.GazePoint currentPoint = fixationRecorder.DisplayGazePoints[i];
                            if (currentPoint.IsLocal && currentPoint.Transform != null)
                            {
                                Vector3 transformposition = currentPoint.Transform.TransformPoint(currentPoint.LocalPoint);
                                GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                                GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                            }
                            else
                            {
                                GL.Vertex3(currentPoint.WorldPoint.x + offsetDir.x, currentPoint.WorldPoint.y + offsetDir.y, currentPoint.WorldPoint.z + offsetDir.z);
                                GL.Vertex3(currentPoint.WorldPoint.x - offsetDir.x, currentPoint.WorldPoint.y - offsetDir.y, currentPoint.WorldPoint.z - offsetDir.z);
                            }
                            previousPoint = currentPoint;
                        }
                        //last point to first point
                        {
                            if (previousPoint.IsLocal && previousPoint.Transform != null)
                            {
                                Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                                GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                                GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                            }
                            else
                            {
                                GL.Vertex3(previousPoint.WorldPoint.x - offsetDir.x, previousPoint.WorldPoint.y - offsetDir.y, previousPoint.WorldPoint.z - offsetDir.z);
                                GL.Vertex3(previousPoint.WorldPoint.x + offsetDir.x, previousPoint.WorldPoint.y + offsetDir.y, previousPoint.WorldPoint.z + offsetDir.z);
                            }
                            
                            GazeBase.GazePoint currentPoint = fixationRecorder.DisplayGazePoints[0];
                            if (currentPoint.IsLocal && currentPoint.Transform != null)
                            {
                                Vector3 transformposition = currentPoint.Transform.TransformPoint(currentPoint.LocalPoint);
                                GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                                GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                            }
                            else
                            {
                                GL.Vertex3(currentPoint.WorldPoint.x + offsetDir.x, currentPoint.WorldPoint.y + offsetDir.y, currentPoint.WorldPoint.z + offsetDir.z);
                                GL.Vertex3(currentPoint.WorldPoint.x - offsetDir.x, currentPoint.WorldPoint.y - offsetDir.y, currentPoint.WorldPoint.z - offsetDir.z);
                            }
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                GL.End();
            }
            else if (displayGaze)
            {
                GL.Begin(GL.QUADS);

                GazeBase.GazePoint previousPoint = gazeBase.DisplayGazePoints[0];
                //start to current
                for (int i = 1; i < gazeBase.currentGazePoint; i++)
                {
                    if (previousPoint.IsLocal && previousPoint.Transform != null)
                    {
                        Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                        GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                        GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                    }
                    else
                    {
                        GL.Vertex3(previousPoint.WorldPoint.x - offsetDir.x, previousPoint.WorldPoint.y - offsetDir.y, previousPoint.WorldPoint.z - offsetDir.z);
                        GL.Vertex3(previousPoint.WorldPoint.x + offsetDir.x, previousPoint.WorldPoint.y + offsetDir.y, previousPoint.WorldPoint.z + offsetDir.z);
                    }
                    if (gazeBase.DisplayGazePoints[i].IsLocal && gazeBase.DisplayGazePoints[i].Transform != null)
                    {
                        Vector3 transformposition = gazeBase.DisplayGazePoints[i].Transform.TransformPoint(gazeBase.DisplayGazePoints[i].LocalPoint);
                        GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                    }
                    else
                    {
                        GL.Vertex3(gazeBase.DisplayGazePoints[i].WorldPoint.x + offsetDir.x, gazeBase.DisplayGazePoints[i].WorldPoint.y + offsetDir.y, gazeBase.DisplayGazePoints[i].WorldPoint.z + offsetDir.z);
                        GL.Vertex3(gazeBase.DisplayGazePoints[i].WorldPoint.x - offsetDir.x, gazeBase.DisplayGazePoints[i].WorldPoint.y - offsetDir.y, gazeBase.DisplayGazePoints[i].WorldPoint.z - offsetDir.z);
                    }
                    previousPoint = gazeBase.DisplayGazePoints[i];
                }

                //current to end
                if (gazeBase.DisplayGazePointBufferFull)
                {
                    bool skipLastPoint = false;

                    previousPoint = gazeBase.DisplayGazePoints[gazeBase.currentGazePoint];
                    for (int i = gazeBase.currentGazePoint + 1; i < gazeBase.DisplayGazePoints.Length; i++)
                    {
                        if (previousPoint.IsLocal && previousPoint.Transform != null)
                        {
                            Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                            GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                            GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        }
                        else
                        {
                            GL.Vertex3(previousPoint.WorldPoint.x - offsetDir.x, previousPoint.WorldPoint.y - offsetDir.y, previousPoint.WorldPoint.z - offsetDir.z);
                            GL.Vertex3(previousPoint.WorldPoint.x + offsetDir.x, previousPoint.WorldPoint.y + offsetDir.y, previousPoint.WorldPoint.z + offsetDir.z);
                        }
                        if (gazeBase.DisplayGazePoints[i].IsLocal && gazeBase.DisplayGazePoints[i].Transform != null)
                        {
                            Vector3 transformposition = gazeBase.DisplayGazePoints[i].Transform.TransformPoint(gazeBase.DisplayGazePoints[i].LocalPoint);
                            GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                            GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        }
                        else
                        {
                            GL.Vertex3(gazeBase.DisplayGazePoints[i].WorldPoint.x + offsetDir.x, gazeBase.DisplayGazePoints[i].WorldPoint.y + offsetDir.y, gazeBase.DisplayGazePoints[i].WorldPoint.z + offsetDir.z);
                            GL.Vertex3(gazeBase.DisplayGazePoints[i].WorldPoint.x - offsetDir.x, gazeBase.DisplayGazePoints[i].WorldPoint.y - offsetDir.y, gazeBase.DisplayGazePoints[i].WorldPoint.z - offsetDir.z);
                        }
                        previousPoint = gazeBase.DisplayGazePoints[i];
                    }
                    if (gazeBase.currentGazePoint == 0)
                        skipLastPoint = true;

                    //last point to first point
                    if (!skipLastPoint)
                    {
                        if (previousPoint.IsLocal && previousPoint.Transform != null)
                        {
                            Vector3 transformposition = previousPoint.Transform.TransformPoint(previousPoint.LocalPoint);
                            GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                            GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        }
                        else
                        {
                            GL.Vertex3(previousPoint.WorldPoint.x - offsetDir.x, previousPoint.WorldPoint.y - offsetDir.y, previousPoint.WorldPoint.z - offsetDir.z);
                            GL.Vertex3(previousPoint.WorldPoint.x + offsetDir.x, previousPoint.WorldPoint.y + offsetDir.y, previousPoint.WorldPoint.z + offsetDir.z);
                        }
                        if (gazeBase.DisplayGazePoints[0].IsLocal && gazeBase.DisplayGazePoints[0].Transform != null)
                        {
                            Vector3 transformposition = gazeBase.DisplayGazePoints[0].Transform.TransformPoint(gazeBase.DisplayGazePoints[0].LocalPoint);
                            GL.Vertex3(transformposition.x - offsetDir.x, transformposition.y - offsetDir.y, transformposition.z - offsetDir.z);
                            GL.Vertex3(transformposition.x + offsetDir.x, transformposition.y + offsetDir.y, transformposition.z + offsetDir.z);
                        }
                        else
                        {
                            GL.Vertex3(gazeBase.DisplayGazePoints[0].WorldPoint.x + offsetDir.x, gazeBase.DisplayGazePoints[0].WorldPoint.y + offsetDir.y, gazeBase.DisplayGazePoints[0].WorldPoint.z + offsetDir.z);
                            GL.Vertex3(gazeBase.DisplayGazePoints[0].WorldPoint.x - offsetDir.x, gazeBase.DisplayGazePoints[0].WorldPoint.y - offsetDir.y, gazeBase.DisplayGazePoints[0].WorldPoint.z - offsetDir.z);
                        }
                    }
                }
                GL.End();
            }

            GL.PopMatrix();
        }

        void OnDestroy()
        {
            FixationCore.OnFixationRecord -= FixationCore_OnFixationRecord;
        }
    }
}