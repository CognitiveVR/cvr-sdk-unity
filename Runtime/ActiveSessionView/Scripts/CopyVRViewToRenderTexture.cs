using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;

//this renders the scene in VR. also adds a command buffer to blit to rendertarget and display on canvas

namespace CognitiveVR.ActiveSession
{
    public class CopyVRViewToRenderTexture : MonoBehaviour
    {
        Camera mainCamera;
        RawImage RawImage;
#pragma warning disable 0414
        Material flipMat;
#pragma warning restore 0414

        RenderTexture rt;
        BuiltinRenderTextureType blitTo = BuiltinRenderTextureType.CurrentActive;
        CameraEvent camevent = CameraEvent.AfterImageEffects; //after everything doesn't work with 2019.4? or steamvr2?

        public void Initialize(RawImage outputImage)
        {
            mainCamera = GetComponent<Camera>();
            RawImage = outputImage;

            var buf = new CommandBuffer();
            buf.name = "cameracopy";
            
            mainCamera.AddCommandBuffer(camevent, buf);

            rt = new RenderTexture(Screen.width, Screen.height, 0);
            temp = new RenderTexture(Screen.width, Screen.height, 0);

            buf.Blit(blitTo, rt, null, (int)0);

            RawImage.texture = temp;

            Shader s = Shader.Find("Hidden/C3D/BlitFlip");
            flipMat = new Material(s);
        }

        RenderTexture temp;

        private void OnPreRender()
        {
#if UNITY_2019_2_OR_NEWER
            Graphics.Blit(rt, temp, flipMat);
#else
            Graphics.Blit(rt, temp);
#endif
        }
    }
}