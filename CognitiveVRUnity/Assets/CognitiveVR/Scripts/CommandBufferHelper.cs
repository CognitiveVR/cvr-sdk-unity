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
    ColorSpace colorSpace;

    public delegate void PostRenderCommandCallback(Ray ray, Vector3 gazepoint);
    PostRenderCommandCallback onPostRenderCommand;
    public void Initialize(RenderTexture src_rt, Camera src_cam, PostRenderCommandCallback postcallback)
    {
        colorSpace = QualitySettings.activeColorSpace;
        cam = src_cam;
        rt = src_rt;
        res = rt.width;
        onPostRenderCommand = postcallback;
        readTexture = new Texture2D(res, res, TextureFormat.RGBAFloat, false);
        temp = new RenderTexture(rt.width, rt.height, 0, RenderTextureFormat.ARGBFloat);
        enabled = false;

        if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1) //ie singlepass
        {
            rect = new Rect(0, 0, res / 2, res);
        }
        else
        {
            rect = new Rect(0, 0, res, res);
        }
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

    private void OnDrawGizmos()
    {
        UnityEditor.Handles.BeginGUI();
        GUI.Label(new Rect(0, 0, 128, 128), rt);
        UnityEditor.Handles.EndGUI();
    }

    private void OnPreRender()
    {
        //TODO do i need to blit rt to temp? i don't wait for frames, so no need to put into a temporary variable?
        Graphics.Blit(rt, temp);

        RenderTexture.active = temp;
        readTexture.ReadPixels(rect, 0, 0);
        if (colorSpace == ColorSpace.Linear)
        {
            depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
        }
        else
        {
            depthR = readTexture.GetPixel((int)(ViewportGazePoint.x * rect.width), (int)(ViewportGazePoint.y * rect.height)).r;
        }

        depthR *= cam.farClipPlane;
        Debug.Log(depthR);
        enabled = false;
        onPostRenderCommand.Invoke(ViewportRay, ViewportRay.direction * depthR);
    }
}
