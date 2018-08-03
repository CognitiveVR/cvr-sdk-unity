using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommandBufferHelper : MonoBehaviour
{
    int readpostrender = 1;
    RenderTexture temp;
    public Texture2D readTexture;
    public RenderTexture rt;
    Camera cam;

    public delegate void PostRenderCommandCallback(Ray ray, Vector3 gazepoint);
    PostRenderCommandCallback onPostRenderCommand;
    public void Initialize(RenderTexture src_rt, Camera src_cam, PostRenderCommandCallback postcallback)
    {
        cam = src_cam;
        rt = src_rt;
        onPostRenderCommand = postcallback;
        readTexture = new Texture2D(256, 256, TextureFormat.RGBAFloat, false);
        temp = new RenderTexture(rt.width, rt.height, 32, RenderTextureFormat.ARGBFloat);
        enabled = false;
    }


    Ray ray;
    public void Begin(Ray tempray)
    {
        enabled = true;
        ray = tempray;
    }
    public float red;
    //might have to do some trig to support eye trackign
    Rect rect = new Rect(0, 0, 256, 256);
    private void OnPreRender()
    {
        if (temp != null)
        {
            Graphics.Blit(rt, temp);
        }
        RenderTexture.active = temp;
        readTexture.ReadPixels(rect, 0, 0);
        //float distance;
        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            red = readTexture.GetPixel(128, 128).linear.r;
        else
        {
            //TODO gamma lighting doesn't correctly sample the greyscale depth value, but very close. not sure why
            red = readTexture.GetPixel(128, 128).r;
        }

        //distance = DepthDistance(cam.farClipPlane, cam.nearClipPlane, red);

        red *= cam.farClipPlane;

        //red = 1/(red*256);

        //red *= -10;

        //red = Mathf.Log(red, 10);
        enabled = false;
        onPostRenderCommand.Invoke(ray, ray.direction * red);
    }

    //should return the world space distance of the sampled pixel
    private float DepthDistance(float f, float n, float z)
    {
        //https://forum.unity.com/threads/_zbufferparams-values.39332/
        //http://www.humus.name/temp/Linearize%20depth.txt
        //http://web.archive.org/web/20130416194336/http://olivers.posterous.com/linear-depth-in-glsl-for-real





        // Z buffer to linear 0..1 depth
        //inline float Linear01Depth( float z )
        //{
        //    return 1.0 / (_ZBufferParams.x * z + _ZBufferParams.y);
        //}

        //float4 _ZBufferParams (0 - 1 range): 
        //x is 1.0 - (camera's far plane) / (camera's near plane)
        //y is (camera's far plane) / (camera's near plane)
        //z is x / (camera's far plane)
        //w is y / (camera's far plane)


        double zbx = (1.0 - f / n);
        double zby = (f / n);

        return (float)(1.0 / (zbx * z + zby));
    }
}
