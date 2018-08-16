using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandBufferHelper : MonoBehaviour
{
    int res;// set in Initialize

    //int readpostrender = 3;
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
        res = rt.width;
        onPostRenderCommand = postcallback;
        readTexture = new Texture2D(res, res, TextureFormat.RGBAFloat, false);
        temp = new RenderTexture(rt.width, rt.height, 0, RenderTextureFormat.ARGBFloat);
        enabled = false;

#if CVR_FOVE
        //fove does it's own rendering stuff and doesn't render singlepass side by side to a texture
        rect = new Rect(0, 0, res, res);
#else
        if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1) //ie singlepass
        {
            //steam renders this side by side with mask
            //oculus renders side by side full size
            rect = new Rect(0, 0, res / 2, res);
        }
        else
        {
            rect = new Rect(0, 0, res, res);
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

    private void OnPreRender()
    {
        //TODO do i need to blit rt to temp? i don't wait for frames, so no need to put into a temporary variable?
        Graphics.Blit(rt, temp);

        RenderTexture.active = temp;
        readTexture.ReadPixels(rect, 0, 0);
        depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;

        depthR *= cam.farClipPlane;
        enabled = false;
        onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR);
    }
}
