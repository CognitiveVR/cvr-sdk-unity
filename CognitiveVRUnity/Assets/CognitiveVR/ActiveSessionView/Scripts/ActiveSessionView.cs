using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//dummy script to display a bunch of options on each canvas script

namespace CognitiveVR.ActiveSession
{
    public class ActiveSessionView : MonoBehaviour
    {
        //sensor materials

        public RawImage MainCameraRenderImage;

        public Camera VRSceneCamera;
        public FixationRenderCamera FixationRenderCamera;
        public Text WarningText;

        void Start()
        {
            if (FixationRenderCamera == null)
            {
                Debug.LogError("ActiveSessionView missing FixationRenderCamera");
                WarningText.text = "ActiveSessionView missing FixationRenderCamera";
                return;
            }
            if (VRSceneCamera == null)
            {
                Debug.LogError("ActiveSessionView missing VRSceneCamera");
                WarningText.text = "ActiveSessionView missing VRSceneCamera";
                return;
            }

            FixationRenderCamera.Initialize(VRSceneCamera);
            var copy = VRSceneCamera.gameObject.AddComponent<CopyVRViewToRenderTexture>();
            copy.Initialize(MainCameraRenderImage);

            WarningText.gameObject.SetActive(false);
        }
    }
}