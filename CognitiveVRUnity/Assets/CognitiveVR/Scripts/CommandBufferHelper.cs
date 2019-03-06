using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace CognitiveVR
{
    public class CommandBufferHelper : MonoBehaviour
    {
        RenderTexture temp;
        Texture2D readTexture;
        RenderTexture rt;
        Camera cam;

        CommandGaze gaze;

        bool SupportsGPUReadback = false;

        public delegate void PostRenderCommandCallback(Ray ray, Vector3 viewportVector, Vector3 worldHitPoint);
        PostRenderCommandCallback onPostRenderCommand;
        public void Initialize(RenderTexture src_rt, Camera src_cam, PostRenderCommandCallback postcallback, CommandGaze gaze)
        {

#if UNITY_2018_2_OR_NEWER
            if (SystemInfo.supportsAsyncGPUReadback)
            {
                SupportsGPUReadback = true;
                debugtex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            }
#endif

            this.gaze = gaze;
            cam = src_cam;
            rt = src_rt;

            onPostRenderCommand = postcallback;
#if SRP_LW3_0_0
            readTexture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += RenderPipeline_beginCameraRendering;
#else
            readTexture = new Texture2D(rt.width, rt.width, TextureFormat.RGBAFloat, false);
#endif
            temp = new RenderTexture(rt.width, rt.height, 0, RenderTextureFormat.ARGBFloat);
            enabled = false;

#if CVR_FOVE
        //fove does it's own rendering stuff and doesn't render singlepass side by side to a texture
        rect = new Rect(0, 0, rt.width, rt.height);
#else
            if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1 && UnityEngine.VR.VRSettings.enabled) //ie singlepass
            {
                //steam renders this side by side with mask
                //oculus renders side by side full size
                rect = new Rect(0, 0, rt.width / 2, rt.height);
            }
            else
            {
                rect = new Rect(0, 0, rt.width, rt.height);
            }
#endif
        }

        float depthR;
        Rect rect;

        Vector3 ViewportGazePoint;
        Vector3 ReProjectedWorldGazeDirection;
        Vector3 OriginalWorldGazeDirection;
        Vector3 InitialCameraForward;
        Vector3 InitialPosition;

        //the vector2(0-1) viewport position of gaze, and the worldspace ray from the camera in the gaze direction
        public void Begin(Vector3 viewportGazePoint, Ray viewportray)
        {
            enabled = true;

            InitialPosition = transform.position;
            InitialCameraForward = transform.forward;
            ReProjectedWorldGazeDirection = GetReProjectedGazeDirection(viewportGazePoint);
            OriginalWorldGazeDirection = gaze.GetWorldGazeDirection().normalized;
            ViewportGazePoint = viewportGazePoint;
        }

#if SRP_LW3_0_0
    private void RenderPipeline_beginCameraRendering(Camera obj)
    {
            if (obj.name != "Main Camera") { return; }

            RenderTexture.active = rt;
            readTexture.ReadPixels(rect, 0, 0);
            depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
            depthR *= cam.farClipPlane;
            enabled = false;

            onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR);
        }

        private void OnDestroy()
        {
            UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering -= RenderPipeline_beginCameraRendering;
        }
#else
#endif

        Vector3 GetReProjectedGazeDirection(Vector2 viewportGazePoint)
        {
            Ray worldGazeRay = new Ray();

            bool usingOculusProjection = false;
            bool usingOpenVRProjection = false;
#if CVR_OCULUS //the plugin, not the builtin package. ARGH UNITY WHY ARE YOU MAKING THIS SO HARD
            usingOculusProjection = true;
#elif CVR_STEAMVR || CVR_STEAMVR2 || CVR_PUPIL || CVR_TOBIIVR || CVR_NEURABLE
            usingOpenVRProjection = true;
#endif
            usingOculusProjection = false;
            usingOpenVRProjection = true;

            if (usingOpenVRProjection || usingOculusProjection)
            {
                Matrix4x4 matrix = new Matrix4x4();
                if (usingOculusProjection)
                {
                    //var pm = Valve.VR.OpenVR.System.GetProjectionMatrix(Valve.VR.EVREye.Eye_Right, cam.nearClipPlane, cam.farClipPlane);
                    var pm = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

                    matrix.m00 = pm.m00;
                    matrix.m01 = pm.m01;
                    matrix.m02 = pm.m02;
                    matrix.m03 = pm.m03;
                    matrix.m10 = pm.m10;
                    matrix.m11 = pm.m11;
                    matrix.m12 = pm.m12;
                    matrix.m13 = pm.m13;
                    matrix.m20 = pm.m20 / 2f;
                    matrix.m21 = (pm.m21 / 2f) * -1f;
                    matrix.m22 = pm.m22;
                    matrix.m23 = pm.m23;
                    matrix.m30 = pm.m30;
                    matrix.m31 = pm.m31;
                    matrix.m32 = pm.m32;
                    matrix.m33 = pm.m33;
                }
                else if (usingOpenVRProjection) //OpenVR
                {
                    //with tobiivr eye tracking, this seems correct UNLESS THE PLAYER IS ROTATING THEIR HEAD AS THEY GAZE OFF CENTER

                    var pm = Valve.VR.OpenVR.System.GetProjectionMatrix(Valve.VR.EVREye.Eye_Right, cam.nearClipPlane, cam.farClipPlane);

                    matrix.m00 = pm.m0;
                    matrix.m01 = pm.m1;
                    matrix.m02 = pm.m2;
                    matrix.m03 = pm.m3;
                    matrix.m10 = pm.m4;
                    matrix.m11 = pm.m5;
                    matrix.m12 = pm.m6;
                    matrix.m13 = pm.m7;
                    matrix.m20 = pm.m8;
                    matrix.m21 = pm.m9;
                    matrix.m22 = pm.m10 / 2f;
                    matrix.m23 = pm.m11;
                    matrix.m30 = pm.m12;
                    matrix.m31 = pm.m13;
                    matrix.m32 = pm.m14 / 2f;
                    matrix.m33 = pm.m15;
                }

                var rawray = cam.ViewportPointToRay(viewportGazePoint);
                Debug.DrawRay(rawray.origin, rawray.direction*100, Color.green, 1f);

                var savedProjection = cam.projectionMatrix;

                //set to oculus or openvr projection
                cam.projectionMatrix = matrix;

                //project viewport into world
                worldGazeRay = cam.ViewportPointToRay(viewportGazePoint);
                Debug.DrawRay(worldGazeRay.origin, worldGazeRay.direction, Color.red, 1f);

                //reset camera projection
                cam.projectionMatrix = savedProjection;
            }

            //save ray, whether or not it was reprojected using oculus/openvr projections
            //ViewportRay = viewportray;
            return worldGazeRay.direction;
        }

        private void OnPreRender()
        {
            if (SupportsGPUReadback)
            {
#if UNITY_2018_2_OR_NEWER
                //will use async read request
                //UnityEngine.Rendering.AsyncGPUReadback.Request(rt, callback: AsyncDone);
                enabled = false;
                var x = (int)((ViewportGazePoint.x) * rect.width);
                var y = (int)((ViewportGazePoint.y) * rect.height);

                if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1) //singlepass??
                {
                    //x = (int)((ViewportGazePoint.x + offsetx*-1) * rect.width);
                }


                //Vector4 center = new Vector4(0, 0, -cam.nearClipPlane, 1);
                //var proj = cam.projectionMatrix * center;
                //Debug.Log("proj " + proj);

                //Debug.DrawRay(transform.position, transform.forward * 10, Color.magenta, 0.1f);

                //cam.projectionMatrix = proj;


                //UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBAFloat, AsyncDone); //TEXTURE
                if (x < 0|| y < 0 || x > rect.width || y>rect.height)
                {
                    Debug.LogError(x + " " + y + " out of bounds!");
                }
                else
                    UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, x, 1, y, 1, 0, 1, TextureFormat.RGBAFloat, AsyncDone); //PIXEL
#else
            }
            else
            {

                //Debug.Log(Vector3.Angle(InitialCameraForward, transform.forward) + " degrees changed");

                //ViewportGazePoint = gaze.GetViewportGazePoint();

                //Vector2 ViewportGazePoint = gaze.GetViewportGazePoint();

                //Vector3 worldGazeDirection = GetReProjectedGazeDirection(ViewportGazePoint);

                //need this blit to temp. setting rt as RenderTexture.active crashes Unity with access violation
                Graphics.Blit(rt, temp);
                RenderTexture.active = temp;
                readTexture.ReadPixels(rect, 0, 0);
                depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
                //Debug.Log("pixel " + (int)(ViewportGazePoint.x * rect.width) + " " + (int)(ViewportGazePoint.y * rect.height));

                //depthR *= cam.farClipPlane;
                enabled = false;
                
                

                var wg = OriginalWorldGazeDirection;
                float actualFarClipDepth = GetAdjustedDistance(cam.farClipPlane, wg, InitialCameraForward);

                //float actualDistance = Mathf.Lerp(cam.nearClipPlane, actualFarClipDepth, depthR - Mathf.Pow(depthR, 10)); //AAAAA LERP IS DEFINITELY NOT RIGHT
                //Debug.Log("actualDistance " + actualDistance);
                //Debug.Log("depthR " + depthR);
                float actualDistance = depthR * actualFarClipDepth;

                //Debug.Log("adjustedDpeth " + (depthR * actualDepth));
                //Debug.DrawRay(ViewportRay.origin, wg * (depthR * actualFarClipDepth), Color.green, 10.1f);

                //depthR *= cam.farClipPlane;
                //Debug.Log("depthR " + depthR);
                //Debug.DrawRay(transform.position, wg * depthR, Color.red, 0.1f);



                //depthR = Mathf.LinearToGammaSpace(depthR);

                //depthR = Mathf.Log(depthR, 10);
                //depthR = Mathf.Pow(depthR, 10);
                //linear to gamma color
                //gamma to linear color

                //float actualDistance = Mathf.Lerp(cam.nearClipPlane, actualDepth, depthR - Mathf.Pow(depthR, 10)); //AAAAA LERP IS DEFINITELY NOT RIGHT


                //Debug.Log("actual distance " + actualDistance);

                Debug.DrawRay(InitialPosition, InitialCameraForward * 10, Color.blue, 0.1f);
                Debug.DrawRay(InitialPosition, ReProjectedWorldGazeDirection * 10, Color.white, 0.1f);


                Vector3 worldHitPoint = InitialPosition + wg * actualDistance;

                //Debug.DrawLine(ViewportRay.origin, worldHitPoint, Color.green, 10);
                //Debug.Log(Vector3.Distance(worldHitPoint, ViewportRay.origin));

                onPostRenderCommand.Invoke(new Ray(InitialPosition, wg), wg * actualDistance, worldHitPoint);
            }
#endif
        }

        //returns the depth adjusted by near/farplanes. without this, gaze distance doesn't scale correctly when looking off center
        private float GetAdjustedDistance(float farclipplane, Vector3 gazeDir, Vector3 camForward)
        {
            float fwdAmount = Vector3.Dot(gazeDir.normalized * farclipplane, camForward.normalized);
            //get angle between center and gaze direction. cos(A) = b/c
            float gazeRads = Mathf.Acos(fwdAmount / farclipplane);
            if (Mathf.Approximately(fwdAmount, farclipplane))
            {
                //when fwdAmount == far, Acos returns NaN for some reason
                gazeRads = 0;
            }
            float dist = farclipplane * Mathf.Tan(gazeRads);
            float hypotenuseDist = Mathf.Sqrt(Mathf.Pow(dist, 2) + Mathf.Pow(farclipplane, 2));
            return hypotenuseDist;
        }

#if UNITY_2018_2_OR_NEWER
        public Texture2D debugtex;
        void AsyncDone(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
        {
            var pixels = request.GetData<Color>();
            Color c;

            if (false) //TEXTURE
            {
                var x = (int)((ViewportGazePoint.x) * rect.width);
                var y = (int)((ViewportGazePoint.y) * rect.height);


                //var viewport = cam.WorldToViewportPoint(cam.transform.position + cam.transform.forward);
                //Debug.Log(viewport);

                //Debug.Log(x + " " + y);
                //depthR = pixels[y * rt.width + x].r;
                //depthR = pixels[16384];

                //Color[] colors = new Color[rt.width * rt.height];

                debugtex.SetPixels(pixels.ToArray());
                //debugtex.Apply(); //EXPENSIVE BUT OK FOR DEBBUGING.
                //don't need to call Apply() to read pixels from this texture! awesome!

                depthR = debugtex.GetPixel(x, y).r;
                //Debug.Log(depthR - depthR2 + " depth sample error");
                //depthR = depthR2;
            }
            else //PIXEL
            {
                depthR = pixels[0].r;
                c = pixels[0];
            }

            //c.a = 1;
            //c.r = depthR;
            //c.g = depthR;
            //c.b = depthR;
            //
            //Debug.DrawRay(ViewportRay.origin, Vector3.up,c,1f);            

            var wg = gaze.GetWorldGazeDirection();
            float actualDepth = GetAdjustedDistance(cam.farClipPlane, wg, cam.transform.forward);

            //depthR = Mathf.LinearToGammaSpace(depthR);

            //depthR = Mathf.Log(depthR, 10);
            //depthR = Mathf.Pow(depthR, 10);
            //linear to gamma color
            //gamma to linear color

            float actualDistance = Mathf.Lerp(cam.nearClipPlane, actualDepth, depthR - Mathf.Pow(depthR,10)); //AAAAA LERP IS DEFINITELY NOT RIGHT

            if (actualDistance > cam.farClipPlane * 2)
            {
                Debug.DrawRay(cam.transform.position, ViewportRay.direction * 100, Color.blue, 100.1f); //with adjustment
                return;
            }

            if (actualDistance > cam.farClipPlane * 0.99f)
            {
                Debug.DrawRay(cam.transform.position, ViewportRay.direction * 100, Color.cyan, 100.1f); //with adjustment
                return;
            }

            //gazeWorldPoint = hmdpos + GazeDirection * actualDistance;

            depthR *= cam.farClipPlane;

            //Debug.Log(depthR);
            RaycastHit hit = new RaycastHit();
            //Physics.Raycast(ViewportRay, out hit);
            //Debug.Log(depthR);// + " " + Vector3.Distance(cam.transform.position, hit.point));

            //depthR = hit.distance;

            Debug.DrawRay(cam.transform.position, ViewportRay.direction * actualDistance, Color.magenta, 100.1f); //with adjustment

            //SEEMS MORE CORRECT WHEN LOOKING IN CENTER
            Debug.DrawRay(cam.transform.position, ViewportRay.direction * depthR, new Color(1,1,1,0.5f), 100.1f); //depth without accounting for difference from farclip and angle


            Vector3 world = ViewportRay.origin + ViewportRay.direction * actualDistance;
            //onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * actualDistance, world);
        }
#endif
    }
}