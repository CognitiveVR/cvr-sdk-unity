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

        YieldInstruction endOfFrame = new WaitForEndOfFrame();

        public IEnumerator OnPostRender()
        {
            if (CognitiveVR_Preferences.Instance.EvaluateGazeRealtime)
            {
                yield return endOfFrame;
                CognitiveVR_Manager.Instance.TickPostRender(Vector3.zero);
                CognitiveVR_Manager.hasHitDynamic = false;
            }
            yield return null;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            Graphics.Blit(source, destination, material);
        }
    }
}