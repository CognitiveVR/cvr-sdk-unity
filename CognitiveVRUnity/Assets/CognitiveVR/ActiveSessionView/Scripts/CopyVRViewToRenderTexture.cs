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

        RenderTexture rt;
        BuiltinRenderTextureType blitTo = BuiltinRenderTextureType.CurrentActive;
        CameraEvent camevent = CameraEvent.AfterEverything;

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
        }

        RenderTexture temp;

        private void OnPreRender()
        {
            Graphics.Blit(rt, temp);
        }
    }
}