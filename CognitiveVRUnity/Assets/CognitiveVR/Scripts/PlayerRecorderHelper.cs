using UnityEngine;
using System.Collections;

/// <summary>
/// this is automatically added to your main camera if PlayerRecorderTracker is set to capture Gaze Points
/// </summary>

namespace CognitiveVR.Components
{
    //depth capture generated this and calls 'do render' a couple times a second, instead of 60fps
    public class PlayerRecorderHelper : MonoBehaviour
    {
        Camera cam;
        Material _mat;
        Material material
        {
            get
            {
                if (_mat == null)
                    _mat = new Material(Shader.Find("Hidden/CognitiveVRSceneDepth"));
                return _mat;
            }
        }

        public delegate void PostRenderCallback();
        PostRenderCallback onPostRender;
        public void Initialize(PostRenderCallback PostRenderCallback)
        {
            onPostRender = PostRenderCallback;
        }

        public RenderTexture DoRender(RenderTexture rt)
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }
            var lastTexture = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = lastTexture;

            return rt;
        }

        YieldInstruction endOfFrame = new WaitForEndOfFrame();

        int LastRenderedFrame;

        public IEnumerator OnPostRender()
        {
            if (LastRenderedFrame == Time.frameCount) { yield break; }
            LastRenderedFrame = Time.frameCount;
            yield return endOfFrame;
            onPostRender.Invoke();
        }

        //steamvr freezes unity if this is enabled in unity 5.4
        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, material);
        }
    }
}