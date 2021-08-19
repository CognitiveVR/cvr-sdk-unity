using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;

//remaps the world position from HMD screen to what is displayed in unity on monitor
//draw texture in OnGUI it doesn't show up in HMD
//use smoothness to blend reticle position over frames (removes some jitter)

//BUG this only works correctly at a 16:9 aspect ratio
//TODO figure out the correct vector math. need to know the sub-region from the HMD image
//CONSIDER scaling fixation/reticle size based on screen resolution
//TODO render saccades

namespace CognitiveVR.ActiveSession
{
    public class FullscreenDisplay : MonoBehaviour
    {
        //ActiveSessionView Asv;
        public void Initialize(ActiveSessionView asv, Camera camera)
        {
            //Asv = asv;
            HMDCamera = camera;
#if CVR_XR //if openvr xr. windowsMR displays both eyes. CONSIDER solutions
        openVROffset = new Vector2(-0.05f,-0.11f);
#elif CVR_STEAMVR2 //if openvr desktop 2.0.5
        //openVROffset = new Vector2(0.083f,0.11f);
        openVROffset = new Vector2(0f,0f);
#endif
            Core.InitEvent += Core_InitEvent;
        }
        Camera HMDCamera;
        FixationRecorder fixationRecorder;

        public bool showReticle = true;
        public bool showFixations = true;
        public bool showSaccades = false;

#pragma warning disable 0414
        Vector2 openVROffset = new Vector2(0.083f, 0.11f);
#pragma warning restore 0414

        private void Core_InitEvent(Error initError)
        {
            if (FixationRecorder.Instance != null)
            {
                fixationRecorder = FixationRecorder.Instance;
                quadPositions = new Vector3[FixationRecorder.DisplayGazePointCount * 4];
                FixationCore.OnFixationRecord -= Fixation_OnFixationRecord;
                FixationCore.OnFixationRecord += Fixation_OnFixationRecord;
                //VectorMathThread = new System.Threading.Thread(CalculateVectors);
                //VectorMathThread.Start();
            }
        }

        private void Update()
        {
            if (fixationRecorder == null) { return; }
            UpdateSaccades();
        }

        public Canvas canvas;
        bool renderFullScreen = false;
        public void SetVisible(bool visible)
        {
            renderFullScreen = visible;
            canvas.enabled = visible;
        }

        void OnGUI()
        {
            if (!renderFullScreen) { return; }
            if (Event.current.type != EventType.Repaint) { return; }
            if (showReticle)
                Reticle_OnGUI();
            if (showSaccades)
                Saccade_OnGUI();
            if (showFixations)
                Fixation_OnGUI();
        }

#region Utilities

        Vector2 RemapToScreen(Vector2 input)
        {
            Vector2 output;
            //BUG this only works with openvr desktop and openvr xr
            output.x = Remap(input.x, 820 - 580, 820 + 580, 0, Screen.width);
            output.y = Remap(input.y, 950 - 300, 950 + 300, 0, Screen.height);

#if CVR_STEAMVR2 || CVR_XR
        output.x += openVROffset.x * Screen.width;
        output.y += openVROffset.y * Screen.height;
#endif
            return output;
        }

        void DrawTextureCentered(Texture texture, Vector2 size, Vector2 position, Color color)
        {
            Rect reticuleRect = new Rect(position.x - size.x / 2, Screen.height - position.y - size.y / 2, size.x, size.y);
            Graphics.DrawTexture(reticuleRect, texture, new Rect(0, 0, 1,1), 0, 0, 0, 0, color);
        }

        float Remap(float num, float low1, float high1, float low2, float high2)
        {
            return low2 + (num - low1) * (high2 - low2) / (high1 - low1);
        }

        Vector2 WorldToRemapScreen(Vector3 position)
        {
            var screenPoint = HMDCamera.WorldToScreenPoint(position);
            screenPoint = RemapToScreen(screenPoint);
            return screenPoint;
        }

#endregion

#region Reticle

        public Texture ReticleTexture;
        public Color ReticleColor = Color.white;
        public float ReticleSize = 64;
        public float Smoothness = 0.2f;
        Vector2 lastScreenPos;

        void Reticle_OnGUI()
        {
            var gazeRay = CognitiveVR.GazeHelper.GetCurrentWorldGazeRay();
            var point = gazeRay.GetPoint(10);
            var screenPoint = WorldToRemapScreen(point);

            //smooth
            screenPoint = Vector2.Lerp(lastScreenPos, screenPoint, Smoothness);
            lastScreenPos = screenPoint;

            DrawTextureCentered(ReticleTexture, new Vector2(ReticleSize, ReticleSize), screenPoint, ReticleColor);
        }
        #endregion

        #region Fixations

        public Texture fixationTexture;
        List<Fixation> fixations = new List<Fixation>(64);
        public float fixationSize = 32;
        public Color FixationColor = Color.white;
        public int NumberOfFixationsToDisplay = 10;

        private void Fixation_OnFixationRecord(Fixation fixation)
        {
            if (fixations.Count > 63)
                fixations.RemoveAt(0);
            fixations.Add(new Fixation(fixation));
        }

        void Fixation_OnGUI()
        {
            //hold more than shown in collection. CONSIDER adding controls to show more fixations
            //show most recent fixations - the last part of the collection

            int displayed = 0;
            for (int i = fixations.Count - 1; i >= 0; i--)
            {
                if (displayed >= NumberOfFixationsToDisplay) { break; }
                if (fixations[i] == null) { continue; }

                displayed++;
                if (fixations[i].IsLocal && fixations[i].DynamicTransform != null)
                {
                    var screenPoint = WorldToRemapScreen(fixations[i].DynamicTransform.TransformPoint(fixations[i].LocalPosition));
                    DrawTextureCentered(fixationTexture, new Vector2(fixationSize, fixationSize), screenPoint, FixationColor);
                }
                else
                {
                    var screenPoint = WorldToRemapScreen(fixations[i].WorldPosition);
                    DrawTextureCentered(fixationTexture, new Vector2(fixationSize, fixationSize), screenPoint, FixationColor);
                }
            }
        }

        #endregion

        #region Saccades

        public Color SaccadeColor;
        public float SaccadeWidth;
        public float SaccadeTimespan;


        Vector3 hmdforward;
        Matrix4x4 m4proj;
        Matrix4x4 m4world;
        int pixelwidth;
        int pixelheight;
        float lineWidth = 0.2f;
        Vector3[] quadPositions;
        System.Threading.Thread VectorMathThread;
        bool displaySaccades = true;
        bool threaded = true;
#pragma warning disable 0414
        int quadPositionCount = 0;
        Material SaccadeMaterial;
        Texture saccadeTexture;
        float gazePointSize = 16;
#pragma warning restore 0414

        private void UpdateSaccades()
        {
            hmdforward = CognitiveVR.GameplayReferences.HMD.forward;
            m4proj = HMDCamera.projectionMatrix;
            m4world = HMDCamera.worldToCameraMatrix;
            pixelwidth = HMDCamera.pixelWidth;
            pixelheight = HMDCamera.pixelHeight;
        }

        void CalculateVectors()
        {
            Vector3 zero = Vector3.zero;
            while (true)
            {
                int j = 0;

                Vector3 b1 = zero; //inner corner
                Vector3 b2 = zero; //outer corner

                if (displaySaccades)
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
            //var firstDirectionPerp = Vector3.Cross(firstDir, hmdforward).normalized * lineWidth / 2f; //hmd forward

            //line segment BC
            //var secondDirectionPerp = Vector3.Cross(secondDir, hmdforward).normalized * lineWidth / 2f; //hmd forward

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

        private void Saccade_OnGUI()
        {
            if (!displaySaccades) { return; }

            //TODO remap to screen
            /*

            if (fixationRecorder == null) { return; }
            if (fixationRecorder.SaccadeScreenPoints == null) { return; }

            foreach (var v in fixationRecorder.SaccadeScreenPoints)
            {
                DrawTextureCentered(saccadeTexture, new Vector2(gazePointSize, gazePointSize), v,Color.white);
            }

            //draw gl quads

            SaccadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.modelview = HMDCamera.worldToCameraMatrix;
            
            GL.Begin(GL.QUADS);
            
            for (int i = 0; i < quadPositionCount; i++)
            {
                //i think these are world position??
                var v2 = WorldToRemapScreen(quadPositions[i]);
                GL.Vertex3(v2.x, v2.y, 0);
                //have worldspace points of saccades
                //have list of positions to draw thick line using GL
            }
            
            GL.End();
            GL.PopMatrix();*/
        }
#endregion

        private void OnDestroy()
        {
            Core.InitEvent -= Core_InitEvent;
            FixationCore.OnFixationRecord -= Fixation_OnFixationRecord;
        }
    }
}