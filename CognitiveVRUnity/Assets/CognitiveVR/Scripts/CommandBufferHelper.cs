using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandBufferHelper : MonoBehaviour
{
    int res = 256;

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
        onPostRenderCommand = postcallback;
        readTexture = new Texture2D(res, res, TextureFormat.RGBAFloat, false);
        temp = new RenderTexture(rt.width, rt.height, 32, RenderTextureFormat.ARGBFloat);
        enabled = false;

        if (CognitiveVR.CognitiveVR_Preferences.Instance.RenderPassType == 1) //ie singlepass
        {
            rect = new Rect(0, 0, res/2, res);
        }
        else
        {
            rect = new Rect(0, 0, res, res);
        }
    }

    float depthR;
    Rect rect;

    Ray ray;
    Vector3 screenGazePoint;
    public void Begin(Ray tempray, Vector3 tempscreenGazePoint)
    {
        enabled = true;
        ray = tempray;
        screenGazePoint = tempscreenGazePoint;
    }

    private void OnPreRender()
    {
        if (temp != null)
        {
            Graphics.Blit(rt, temp);
        }

        Vector2 projectionadjustedscreenpos = GetCenterPoint(screenGazePoint);
        RenderTexture.active = temp;
        readTexture.ReadPixels(rect, 0, 0);
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
        {
            //depthR = readTexture.GetPixel((int)rect.width / 2, (int)rect.height / 2).linear.r;
            depthR = readTexture.GetPixel((int)(projectionadjustedscreenpos.x * rect.width), (int)(projectionadjustedscreenpos.y * rect.height)).linear.r;
        }
        else
        {
            //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
            //depthR = readTexture.GetPixel((int)rect.width / 2, (int)rect.height / 2).r;
            depthR = readTexture.GetPixel((int)(projectionadjustedscreenpos.x * rect.width), (int)(projectionadjustedscreenpos.y * rect.height)).r;
        }

        //ADJUSTED RAY
        //Vector2 projectionadjustedscreenpos = GetCenterPoint(screenGazePoint);
        var adjustedray = cam.ViewportPointToRay(new Vector3(projectionadjustedscreenpos.x, projectionadjustedscreenpos.y, 100));
        Debug.DrawRay(adjustedray.origin, adjustedray.direction * 100, Color.blue, 1);

        depthR *= cam.farClipPlane;
        enabled = false;
        onPostRenderCommand.Invoke(ray, adjustedray.direction * depthR);
    }

    float rightoffset = 0.027f;
    float topoffset = 0;

    //expect in 0-1 vector
    //returns ....something slightly offset
    Vector2 GetCenterPoint(Vector2 normalizedPointIn)
    {
        //var hmd = Valve.VR.OpenVR.System;

        //float l_left = 0.0f, l_right = 0.0f, l_top = 0.0f, l_bottom = 0.0f;
        //hmd.GetProjectionRaw(Valve.VR.EVREye.Eye_Left, ref l_left, ref l_right, ref l_top, ref l_bottom);

        //float r_left = 0.0f, r_right = 0.0f, r_top = 0.0f, r_bottom = 0.0f;
        //hmd.GetProjectionRaw(Valve.VR.EVREye.Eye_Right, ref r_left, ref r_right, ref r_top, ref r_bottom);

        //Debug.Log("LEFT   "+l_left + " " + l_right + " " + l_top + " " + l_bottom);
        //Debug.Log("RIGHT " + r_left + " " + r_right + " " + r_top + " " + r_bottom);

        return new Vector2(normalizedPointIn.x + rightoffset, normalizedPointIn.y + topoffset);
    }
}
