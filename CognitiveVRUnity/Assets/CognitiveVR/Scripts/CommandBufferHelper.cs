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

        public delegate void PostRenderCommandCallback(Ray ray, Vector3 viewportVector, Vector3 worldHitPoint);
        PostRenderCommandCallback onPostRenderCommand;
        public void Initialize(RenderTexture src_rt, Camera src_cam, PostRenderCommandCallback postcallback, CommandGaze gaze)
        {
            this.gaze = gaze;
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
            if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1 && UnityEngine.VR.VRSettings.enabled) //ie singlepass
            {
                //steam renders this side by side with mask
                //oculus renders side by side full size
                rect = new Rect(0, 0, rt.width / 2, rt.height);
                //rect = new Rect(0, 0, 1134, 1200);
                //assuming 1512 x 1680
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
        Ray ViewportRay;
        public void Begin(Vector3 ViewportGazePoint, Ray viewportray) //the vector2(0-1) viewport position of gaze, and the worldspace ray from the camera in the gaze direction
        {
            enabled = true;
            this.ViewportGazePoint = ViewportGazePoint;

            //this viewport gaze point needs to be projected into openvr's camera projection matrix

#if CVR_OCULUS //the plugin, not the builtin package. ARGH UNITY WHY ARE YOU MAKING THIS SO HARD
#endif

            bool bOculus = true;
            Matrix4x4 matrix;

            //save camera projection
            var savedProjection = cam.projectionMatrix;

            if (bOculus)
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
            else //OpenVR
            {
                //var pm1 = Valve.VR.OpenVR.System.GetProjectionMatrix(Valve.VR.EVREye.Eye_Right, cam.nearClipPlane, cam.farClipPlane);
                var pm = cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);

                matrix.m00 = pm.m00;
                matrix.m01 = pm.m01;
                matrix.m02 = pm.m02;
                matrix.m03 = pm.m03;
                matrix.m10 = pm.m10;
                matrix.m11 = pm.m11;
                matrix.m12 = pm.m12;
                matrix.m13 = pm.m13;
                matrix.m20 = pm.m20;
                matrix.m21 = pm.m21;
                matrix.m22 = pm.m22 / 2f;
                matrix.m23 = pm.m23;
                matrix.m30 = pm.m30;
                matrix.m31 = pm.m31;
                matrix.m32 = pm.m32 / 2f;
                matrix.m33 = pm.m33;
            }

            if (true)
            {
                //set to openvr projection
                //cam.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);

                cam.projectionMatrix = matrix;

                //project viewport into world
                viewportray = cam.ViewportPointToRay(ViewportGazePoint);

                //reset camera projection
                cam.projectionMatrix = savedProjection;
                //Debug.DrawRay(viewportray.origin, viewportray.direction * 100, Color.magenta,100);

                //world to viewport
                //var direction = cam.WorldToViewportPoint(adjustedRay.GetPoint(100));
                //viewportray = new Ray(viewportray.origin, adjustedRay.direction);
            }

            ViewportRay = viewportray;
        }

        /*private void OnDrawGizmos()
        {
            UnityEditor.Handles.BeginGUI();
            GUI.Label(new Rect(0, 0, 128, 128), rt);
            UnityEditor.Handles.EndGUI();
        }*/
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
        public Texture2D debugtex;
        private void OnPreRender()
        {
            //need this blit to temp. setting rt as RenderTexture.active crashes Unity with access violation


#if UNITY_2018_2_OR_NEWER
            //will use async read request
            //UnityEngine.Rendering.AsyncGPUReadback.Request(rt, callback: AsyncDone);
            enabled = false;
            var x = (int)((ViewportGazePoint.x + offsetx) * rect.width);
            var y = (int)((ViewportGazePoint.y + offsety) * rect.height);

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
            if (x < 0 || y < 0 || x > rect.width || y > rect.height)
            {
                Debug.LogError(x + " " + y + " out of bounds!");
            }
            else
                UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, x, 1, y, 1, 0, 1, TextureFormat.RGBAFloat, AsyncDone); //PIXEL
#else
            Graphics.Blit(rt, temp);
            RenderTexture.active = temp;
            readTexture.ReadPixels(rect, 0, 0);
            depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
            depthR *= cam.farClipPlane;
            enabled = false;
            onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR, ViewportRay.origin + ViewportRay.direction * depthR);
#endif
        }

        //PREFER NOT TO HARD CODE MAGIC PROJECTION NUMBERS
        //oculus rift
        //public float offsetx = -0.075f;
        //public float offsety = 0.054f;

        //vive projection
        public float offsetx = 0;//-0.03f;
        public float offsety = 0;//0.0f;

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
        void AsyncDone(UnityEngine.Rendering.AsyncGPUReadbackRequest request)
        {
            var pixels = request.GetData<Color>();
            Color c;

            if (false) //TEXTURE
            {
                var x = (int)((ViewportGazePoint.x + offsetx) * rect.width);
                var y = (int)((ViewportGazePoint.y + offsety) * rect.height);


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
            Debug.DrawRay(cam.transform.position, ViewportRay.direction * depthR, new Color(1, 1, 1, 0.5f), 100.1f); //depth without accounting for difference from farclip and angle


            Vector3 world = ViewportRay.origin + ViewportRay.direction * actualDistance;
            //onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * actualDistance, world);
        }
#endif
#endif
    }
}