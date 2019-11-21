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

        CognitiveVR.FixationRecorder fixationRecorder;
        CognitiveVR.GazeBase gazeBase;
        Camera FixationCamera;
        Transform TargetCameraTransform;
        Camera FollowCamera;

        bool displayFixations = false;
        bool displayGaze = false;

        Queue<Fixation> fixationQueue = new Queue<Fixation>(50);



        Vector3 hmdforward;
        Matrix4x4 m4proj;
        Matrix4x4 m4world;
        int pixelwidth;
        int pixelheight;
        public float lineWidth = 0.2f;
        System.Threading.Thread VectorMathThread;

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
                    if (threaded)
                    {
                        VectorMathThread = new System.Threading.Thread(CalculateVectors);
                        VectorMathThread.Start();
                    }
                }
                else
                {
                    displayGaze = true;
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
                if (threaded)
                {
                    VectorMathThread = new System.Threading.Thread(CalculateVectors);
                    VectorMathThread.Start();
                }
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
            if (!displayFixations && !displayGaze) { return; }

            hmdforward = CognitiveVR.GameplayReferences.HMD.forward;

            m4proj = FixationCamera.projectionMatrix;
            m4world = FixationCamera.worldToCameraMatrix;
            pixelwidth = FixationCamera.pixelWidth;
            pixelheight = FixationCamera.pixelHeight;

            if (displayFixations)
            {
                Vector3 scale = Vector3.one * FixationScale;
                //on new fixation
                foreach (Fixation f in fixationQueue)
                {
                    if (f.IsLocal && f.LocalTransform != null)
                    {
                        Matrix4x4 m = Matrix4x4.TRS(f.LocalTransform.TransformPoint(f.LocalPosition), Quaternion.identity, scale * f.MaxRadius);
                        Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, FixationCamera);
                    }
                    else
                    {
                        Matrix4x4 m = Matrix4x4.TRS(f.WorldPosition, Quaternion.identity, scale * f.MaxRadius);
                        Graphics.DrawMesh(FixationMesh, m, FixationMaterial, FixationRenderLayer, FixationCamera);
                    }
                }

                //update list of points
                Color magenta = Color.magenta;
                int count = fixationRecorder.DisplayGazePoints.Count;
                for (int i = 0; i < count; i++)
                {
                    ThreadGazePoint temp = fixationRecorder.DisplayGazePoints[i];

                    if (!threaded && fixationRecorder.DisplayGazePoints[i - 1] != null)
                        Debug.DrawLine(fixationRecorder.DisplayGazePoints[i - 1].WorldPoint, temp.WorldPoint, magenta);
                    if (!temp.IsLocal) { continue; }
                    if (temp.Transform == null) { temp.IsLocal = false; continue; }
                    temp.TransformMatrix = temp.Transform.localToWorldMatrix;
                }
            }
            else if (displayGaze)
            {
                //update list of points
                Color magenta = Color.magenta;
                int count = gazeBase.DisplayGazePoints.Count;
                for (int i = 0; i < count; i++)
                {
                    ThreadGazePoint temp = gazeBase.DisplayGazePoints[i];

                    if (!threaded && gazeBase.DisplayGazePoints[i - 1] != null)
                        Debug.DrawLine(gazeBase.DisplayGazePoints[i - 1].WorldPoint, temp.WorldPoint, magenta);
                    if (!temp.IsLocal) { continue; }
                    if (temp.Transform == null) { temp.IsLocal = false; continue; }
                    temp.TransformMatrix = temp.Transform.localToWorldMatrix;
                }
            }
        }

        bool threaded = true;
        void CalculateVectors()
        {
            Vector3 zero = Vector3.zero;
            while (true)
            {
                int j = 0;

                Vector3 b1 = zero; //inner corner
                Vector3 b2 = zero; //outer corner

                if (displayFixations)
                {
                    for (int i = 0; i < fixationRecorder.DisplayGazePoints.Count; i++)
                    {
                        int prevIndex = i - 1;
                        int currentIndex = i;
                        int nextIndex = i + 1;

                        //draw everything
                        Vector3 previousPoint = zero;
                        if (fixationRecorder.DisplayGazePoints[prevIndex] != null)
                        {

                            if (fixationRecorder.DisplayGazePoints[prevIndex].IsLocal)
                            {
                                //do math from transform matrix
                                previousPoint = fixationRecorder.DisplayGazePoints[prevIndex].TransformMatrix.MultiplyPoint(fixationRecorder.DisplayGazePoints[prevIndex].LocalPoint);
                            }
                            else
                            {
                                previousPoint = fixationRecorder.DisplayGazePoints[prevIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            previousPoint = fixationRecorder.DisplayGazePoints[currentIndex].WorldPoint + Vector3.down * 0.01f;
                        }

                        Vector3 currentPoint = zero;
                        if (fixationRecorder.DisplayGazePoints[currentIndex].IsLocal)
                        {
                            //do math from transform matrix
                            currentPoint = fixationRecorder.DisplayGazePoints[currentIndex].TransformMatrix.MultiplyPoint(fixationRecorder.DisplayGazePoints[currentIndex].LocalPoint);
                        }
                        else
                        {
                            currentPoint = fixationRecorder.DisplayGazePoints[currentIndex].WorldPoint;
                        }

                        Vector3 nextPoint = zero;
                        if (fixationRecorder.DisplayGazePoints[nextIndex] != null)
                        {
                            if (fixationRecorder.DisplayGazePoints[nextIndex].IsLocal)
                            {
                                //do math from transform matrix
                                nextPoint = fixationRecorder.DisplayGazePoints[nextIndex].TransformMatrix.MultiplyPoint(fixationRecorder.DisplayGazePoints[nextIndex].LocalPoint);
                            }
                            else
                            {
                                nextPoint = fixationRecorder.DisplayGazePoints[nextIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            nextPoint = fixationRecorder.DisplayGazePoints[currentIndex].WorldPoint + Vector3.up * 0.01f;
                        }

                        if (i == 0)
                        {
                            b1 = currentPoint;
                            b2 = currentPoint;
                        }
                        CalculateQuadPoints(previousPoint, currentPoint, nextPoint, ref b1, ref b2, ref j);
                    }
                }
                else if (displayGaze)
                {
                    for (int i = 0; i < gazeBase.DisplayGazePoints.Count; i++)
                    {
                        int prevIndex = i - 1;
                        int currentIndex = i;
                        int nextIndex = i + 1;

                        //draw everything
                        Vector3 previousPoint = zero;
                        if (gazeBase.DisplayGazePoints[prevIndex] != null)
                        {

                            if (gazeBase.DisplayGazePoints[prevIndex].IsLocal)
                            {
                                //do math from transform matrix
                                previousPoint = gazeBase.DisplayGazePoints[prevIndex].TransformMatrix.MultiplyPoint(gazeBase.DisplayGazePoints[prevIndex].LocalPoint);
                            }
                            else
                            {
                                previousPoint = gazeBase.DisplayGazePoints[prevIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            previousPoint = gazeBase.DisplayGazePoints[currentIndex].WorldPoint + Vector3.down * 0.01f;
                        }

                        Vector3 currentPoint = zero;
                        if (gazeBase.DisplayGazePoints[currentIndex].IsLocal)
                        {
                            //do math from transform matrix
                            currentPoint = gazeBase.DisplayGazePoints[currentIndex].TransformMatrix.MultiplyPoint(gazeBase.DisplayGazePoints[currentIndex].LocalPoint);
                        }
                        else
                        {
                            currentPoint = gazeBase.DisplayGazePoints[currentIndex].WorldPoint;
                        }

                        Vector3 nextPoint = zero;
                        if (gazeBase.DisplayGazePoints[nextIndex] != null)
                        {
                            if (gazeBase.DisplayGazePoints[nextIndex].IsLocal)
                            {
                                //do math from transform matrix
                                nextPoint = gazeBase.DisplayGazePoints[nextIndex].TransformMatrix.MultiplyPoint(gazeBase.DisplayGazePoints[nextIndex].LocalPoint);
                            }
                            else
                            {
                                nextPoint = gazeBase.DisplayGazePoints[nextIndex].WorldPoint;
                            }
                        }
                        else
                        {
                            nextPoint = gazeBase.DisplayGazePoints[currentIndex].WorldPoint + Vector3.up * 0.01f;
                        }

                        if (i == 0)
                        {
                            b1 = currentPoint;
                            b2 = currentPoint;
                        }
                        CalculateQuadPoints(previousPoint, currentPoint, nextPoint, ref b1, ref b2, ref j);
                    }
                }
                quadPositionCount = j;
                if (threaded)
                {
                    System.Threading.Thread.Sleep(10);
                }
                else
                {
                    break;
                }
            }
        }

        private void CalculateQuadPoints(Vector3 previousPoint, Vector3 currentPoint, Vector3 nextPoint, ref Vector3 b1, ref Vector3 b2, ref int j)
        {

            //screenspace stuff
            Vector3 mathscreenprevious = manualWorldToScreenPoint(previousPoint, m4proj, m4world, pixelwidth, pixelheight);
            Vector3 mathscreencurrent = manualWorldToScreenPoint(currentPoint, m4proj, m4world, pixelwidth, pixelheight);
            Vector3 mathscreennext = manualWorldToScreenPoint(nextPoint, m4proj, m4world, pixelwidth, pixelheight);
            var screenDirection = (mathscreencurrent - mathscreenprevious).normalized;
            Vector3 screencross = Vector3.Cross(screenDirection, hmdforward); //used to fallback with very acute corners

            Vector3 firstDir = (mathscreenprevious - mathscreencurrent).normalized;
            Vector3 secondDir = (mathscreennext - mathscreencurrent).normalized;

            //line segment AB
            var firstDirectionPerp = Vector3.Cross(firstDir, hmdforward).normalized * lineWidth / 2f; //hmd forward

            //line segment BC
            var secondDirectionPerp = Vector3.Cross(secondDir, hmdforward).normalized * lineWidth / 2f; //hmd forward

            //line angle ABC
            float lineAngleABC = SignedAngle(firstDir, secondDir, hmdforward);
            float complementaryAngleABC = 90 - lineAngleABC;


            //outer corner
            float hypotenuse = (lineWidth / 2f) / Mathf.Cos(complementaryAngleABC * Mathf.Deg2Rad);
            Vector3 lowerCorner = currentPoint + -secondDir.normalized * hypotenuse - firstDir * hypotenuse;

            //inner corner
            Vector3 upperCorner = currentPoint + secondDir.normalized * hypotenuse + firstDir * hypotenuse;

            //quad corner points from previous line segment
            quadPositions[j] = b1;
            j++;
            quadPositions[j] = b2;
            j++;

            if ((lineAngleABC < 40 && lineAngleABC > -40) || lineAngleABC > 160 || lineAngleABC < -160)
            {
                quadPositions[j] = currentPoint - screencross * 0.5f * lineWidth;
                j++;
                quadPositions[j] = currentPoint + screencross * 0.5f * lineWidth;
                j++;

                b1 = currentPoint + screencross * 0.5f * lineWidth;
                b2 = currentPoint - screencross * 0.5f * lineWidth;
                //Debug.DrawLine(b2, b1, Color.red);
            }
            else
            {
                b1 = upperCorner;
                b2 = lowerCorner;
                quadPositions[j] = b2;
                j++;
                quadPositions[j] = b1;
                j++;
                //Debug.DrawLine(b2, b1, Color.green);
            }
        }

        //this doesn't exist in unity 5.6
        public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsignedAngle = Vector3.Angle(from, to);

            float cross_x = from.y * to.z - from.z * to.y;
            float cross_y = from.z * to.x - from.x * to.z;
            float cross_z = from.x * to.y - from.y * to.x;
            float sign = Mathf.Sign(axis.x * cross_x + axis.y * cross_y + axis.z * cross_z);
            return unsignedAngle * sign;
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
            if (!displayFixations) { return; }
            if (TargetCameraTransform == null) { return; }
            MatchTargetCamera();
            transform.SetPositionAndRotation(TargetCameraTransform.position, TargetCameraTransform.rotation);
        }
        
        //the output of the thread
        Vector3[] quadPositions = new Vector3[4096 * 4];
        int quadPositionCount = 0;


        Vector3 manualWorldToScreenPoint(Vector3 wp, Matrix4x4 camproj, Matrix4x4 worldtocam, int width, int height)
        {
            // calculate view-projection matrix
            Matrix4x4 mat = camproj * worldtocam;

            // multiply world point by VP matrix
            Vector4 temp = mat * new Vector4(wp.x, wp.y, wp.z, 1f);

            if (temp.w == 0f)
            {
                // point is exactly on camera focus point, screen point is undefined
                // unity handles this by returning 0,0,0
                return Vector3.zero;
            }
            else
            {
                // convert x and y from clip space to window coordinates
                temp.x = (temp.x / temp.w + 1f) * .5f * width;
                temp.y = (temp.y / temp.w + 1f) * .5f * height;
                return new Vector3(temp.x, temp.y, wp.z);
            }
        }

        private void OnPostRender()
        {
            if (!displayFixations && !displayGaze) { return; }

            SaccadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(FixationCamera.projectionMatrix);
            GL.modelview = FixationCamera.worldToCameraMatrix;

            GL.Begin(GL.QUADS);

            for (int i = 0; i < quadPositionCount; i++)
            {
                GL.Vertex3(quadPositions[i].x, quadPositions[i].y, quadPositions[i].z);
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