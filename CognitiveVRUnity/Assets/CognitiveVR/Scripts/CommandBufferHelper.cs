using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Internal/Command Buffer Helper")]
    public class CommandBufferHelper : MonoBehaviour
    {
        RenderTexture temp;
        Texture2D readTexture;
        RenderTexture rt;
        Camera cam;

#if UNITY_2018_2_OR_NEWER
        CommandGaze gaze;
        bool supportsAsyncGPUReadback = false; //used in 2018
#endif

        public delegate void PostRenderCommandCallback(Ray ray, Vector3 viewportVector, Vector3 worldHitPoint);
        PostRenderCommandCallback onPostRenderCommand;
        public void Initialize(RenderTexture src_rt, Camera src_cam, PostRenderCommandCallback postcallback, CommandGaze gaze)
        {
#if UNITY_2018_2_OR_NEWER
            this.gaze = gaze;
#endif
            cam = src_cam;
            rt = src_rt;
            debugtex = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
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
#if UNITY_2017_2_OR_NEWER
            if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1 && UnityEngine.XR.XRSettings.enabled) //ie singlepass
            {
#else
            if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1 && UnityEngine.VR.VRSettings.enabled) //ie singlepass
            {
#endif
#if CVR_OCULUS
                //oculus renders side by side without a mask
                rect = new Rect(0, 0, rt.width / 2, rt.height);
#elif CVR_DEFAULT || CVR_STEAMVR || CVR_STEAMVR2 //eye tracking should use physics gaze!
                //steam renders this side by side with mask
                rect = new Rect(0, 0, rt.width / 2, rt.height);
#else //adhawk, tobii, fove, pupil, neurable, vive pro eye
                rect = new Rect(0, 0, rt.width / 2, rt.height);
#endif
            }
            else
            {
                rect = new Rect(0, 0, rt.width, rt.height);
            }
#endif

#if UNITY_2018_2_OR_NEWER
            if (SystemInfo.supportsAsyncGPUReadback)
                supportsAsyncGPUReadback = true;
#endif


        }

        float depthR;
        Rect rect;

        Vector3 ViewportGazePoint;
        Ray ViewportRay;
        public void Begin(Vector3 ViewportGazePoint, Ray viewportray) //the vector2(0-1) viewport position of gaze, and the worldspace ray from the camera in the gaze direction
        {
            enabled = true;
            this.ViewportGazePoint = ViewportGazePoint;

            //this viewport gaze point needs to be projected into openvr's camera projection matrix

            Matrix4x4 matrix = Matrix4x4.identity;
#if UNITY_2017_2_OR_NEWER
            if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1 && UnityEngine.XR.XRSettings.enabled) //ie singlepass
#else
            if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1 && UnityEngine.VR.VRSettings.enabled) //ie singlepass
#endif
                matrix = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            else //multipass
                matrix = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

            //save camera projection
            var savedProjection = cam.projectionMatrix;
            //set to eye projection

            cam.projectionMatrix = matrix;

            //project from eye viewport into world
            viewportray = cam.ViewportPointToRay(ViewportGazePoint);

            //reset camera projection
            cam.projectionMatrix = savedProjection;

            ViewportRay = viewportray;
        }

#if SRP_LW3_0_0
        private void RenderPipeline_beginCameraRendering(Camera obj)
        {
            if (obj.name != "Main Camera") { return; }

            //basic implementation without using async read requests
            RenderTexture.active = rt;
            readTexture.ReadPixels(rect, 0, 0);
            depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
            depthR *= cam.farClipPlane;
            enabled = false;

            onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR, ViewportRay.origin + ViewportRay.direction * depthR);
        }

        private void OnDestroy()
        {
            UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering -= RenderPipeline_beginCameraRendering;
        }
#else
        public Texture2D debugtex;
        private void OnPreRender()
        {
#if UNITY_2018_2_OR_NEWER
            if (supportsAsyncGPUReadback)
            {
                //will use async read request
                //UnityEngine.Rendering.AsyncGPUReadback.Request(rt, callback: AsyncDone);
                enabled = false;
                var x = (int)((ViewportGazePoint.x) * rect.width);
                var y = (int)((ViewportGazePoint.y) * rect.height);

                //UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, TextureFormat.RGBAFloat, AsyncDone); //TEXTURE
                if (x < 0 || y < 0 || x > rect.width || y > rect.height)
                {
                    Debug.LogError(x + " " + y + " out of bounds!");
                }
                else
                    UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, x, 1, y, 1, 0, 1, TextureFormat.RGBAFloat, AsyncDone); //PIXEL
            }
            else
#endif
            {
                //need this blit to temp. setting rt as RenderTexture.active crashes Unity with access violation
                Graphics.Blit(rt, temp);
                RenderTexture.active = temp;
                readTexture.ReadPixels(rect, 0, 0);
                depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
                depthR *= cam.farClipPlane;
                enabled = false;
                onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR, ViewportRay.origin + ViewportRay.direction * depthR);
            }
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
            //float hypotenuseDist = Mathf.Sqrt(Mathf.Pow(dist, 2) + Mathf.Pow(farclipplane, 2));
            float hypotenuseDist = Mathf.Sqrt(dist* dist + farclipplane * farclipplane);
            return hypotenuseDist;
        }

#if UNITY_2018_2_OR_NEWER
        void AsyncDone(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
        {
            var pixels = request.GetData<Color>();
            Color c;

            depthR = pixels[0].r;
            c = pixels[0];

            var wg = gaze.GetWorldGazeDirection();
            float actualDepth = GetAdjustedDistance(GameplayReferences.HMDCameraComponent.farClipPlane, wg, GameplayReferences.HMD.forward);

            float actualDistance = Mathf.Lerp(GameplayReferences.HMDCameraComponent.nearClipPlane, actualDepth, depthR);

            if (actualDistance > GameplayReferences.HMDCameraComponent.farClipPlane * 2)
            {
                Debug.DrawRay(GameplayReferences.HMD.position, ViewportRay.direction * 100, Color.blue, 100.1f); //with adjustment
                return;
            }

            if (actualDistance > GameplayReferences.HMDCameraComponent.farClipPlane * 0.99f)
            {
                Debug.DrawRay(GameplayReferences.HMD.position, ViewportRay.direction * 100, Color.cyan, 100.1f); //with adjustment
                return;
            }

            depthR *= GameplayReferences.HMDCameraComponent.farClipPlane;

            Debug.DrawRay(GameplayReferences.HMD.position, ViewportRay.direction * actualDistance, Color.magenta, 100.1f); //with adjustment

            //SEEMS MORE CORRECT WHEN LOOKING IN CENTER
            Debug.DrawRay(GameplayReferences.HMD.position, ViewportRay.direction * depthR, new Color(1, 1, 1, 0.5f), 100.1f); //depth without accounting for difference from farclip and angle

            //TODO when using eye tracking, use 'actualDistance'

            Vector3 world = ViewportRay.origin + ViewportRay.direction * depthR;
            onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR, world);
        }
#endif
#endif
    }
}