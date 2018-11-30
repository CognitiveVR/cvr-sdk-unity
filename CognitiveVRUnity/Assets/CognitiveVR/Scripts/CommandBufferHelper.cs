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

    public delegate void PostRenderCommandCallback(Ray ray, Vector3 gazepoint);
    PostRenderCommandCallback onPostRenderCommand;
    public void Initialize(RenderTexture src_rt, Camera src_cam, PostRenderCommandCallback postcallback)
    {
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
    Ray ViewportRay;
    public void Begin(Vector3 ViewportGazePoint, Ray viewportray) //the vector2(0-1) viewport position of gaze, and the worldspace ray from the camera in the gaze direction
    {
        enabled = true;
        this.ViewportGazePoint = ViewportGazePoint;
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
    private void OnPreRender()
    {
        //need this blit to temp. setting rt as RenderTexture.active crashes Unity with access violation
        Graphics.Blit(rt, temp);

        RenderTexture.active = temp;
        readTexture.ReadPixels(rect, 0, 0);
        depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;

        depthR *= cam.farClipPlane;
        enabled = false;
        onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR);
    }
#endif
    }
}