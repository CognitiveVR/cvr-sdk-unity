using UnityEngine;
using System.Collections;

/// <summary>
/// this is automatically added to your main camera if PlayerRecorderTracker is set to capture Gaze Points
/// </summary>

namespace CognitiveVR
{
    //depth capture generated this and calls 'do render' a couple times a second, instead of 60fps
    public class PlayerTrackerHelper : MonoBehaviour
    {
        Camera cam;
        Material _mat;
        Material material
        {
            get {
                if (_mat == null)
                    _mat = new Material(Shader.Find("Hidden/CognitiveVRSceneDepth"));
                return _mat;
            }
        }

        public RenderTexture DoRender(RenderTexture rt)
        {
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;

            return rt;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, material);
        }
    }
}