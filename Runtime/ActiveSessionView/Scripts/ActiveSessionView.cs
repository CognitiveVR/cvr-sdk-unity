using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//dummy script to display a bunch of options on each canvas script

namespace Cognitive3D.ActiveSession
{
    [AddComponentMenu("Cognitive3D/Active Session View/Active Session View")]
    public class ActiveSessionView : MonoBehaviour
    {
        //sensor materials

        public RawImage MainCameraRenderImage;
        public FullscreenDisplay FullscreenDisplay;
        public Camera VRSceneCamera;
        public RenderEyetracking RenderEyetracking;
        public Text WarningText;

        //waits for calibration if eye tracker is not calibrated
        IEnumerator Start()
        {
            FullscreenDisplay.Initialize(this,VRSceneCamera);
            FullscreenDisplay.SetVisible(false);
            if (RenderEyetracking == null)
            {
                Debug.LogError("ActiveSessionView missing FixationRenderCamera");
                WarningText.text = "ActiveSessionView missing FixationRenderCamera";
                yield break;
            }
            if (VRSceneCamera == null)
            {
                Debug.LogError("ActiveSessionView missing VRSceneCamera");
                WarningText.text = "ActiveSessionView missing VRSceneCamera";
                yield break;
            }

            RenderEyetracking.Initialize(this,VRSceneCamera);
            var copy = VRSceneCamera.gameObject.AddComponent<CopyVRViewToRenderTexture>();
            copy.Initialize(MainCameraRenderImage);

            WarningText.gameObject.SetActive(false);

#if C3D_SRANIPAL
            bool needCalibration = false;
            int output = ViveSR.anipal.Eye.SRanipal_Eye_API.IsUserNeedCalibration(ref needCalibration);
            ViveSR.Error error = (ViveSR.Error)output;

            if (output != 0)
            {
                WarningText.text = "Eye Tracking Calibration Error " + error.ToString();
            }
            else if (needCalibration)
            {
                WarningText.text = "Eye Tracking needs Calibration";
            }
            else
            {
                WarningText.text = "Eye Tracking Calibrated";
                yield return new WaitForSeconds(2);
                WarningText.enabled = false;
            }
#elif C3D_VARJOXR
            if (Varjo.XR.VarjoEyeTracking.GetGaze().status == Varjo.XR.VarjoEyeTracking.GazeStatus.Valid)
            {
                WarningText.text = "Eye Tracking Calibrated";
                yield return new WaitForSeconds(2);
                WarningText.enabled = false;
            }
            else
            {
                WarningText.text = "Eye Tracking not Calibrated";
            }
#elif C3D_OMNICEPT
            WarningText.text = "Omnicept Found!";
#else
            //TODO add omnicept, other eye tracking SDK status visualization
            WarningText.text = "No Eye Tracking Found!";
#endif
        }

        public void Button_ToggleFullscreen(bool showFullscreen)
        {
            if (showFullscreen)
            {
                var canvases = GetComponentsInChildren<Canvas>();
                foreach (var c in canvases)
                    c.enabled = false;
                //hide all other canvases
                //display basicgui
                FullscreenDisplay.SetVisible(true);
            }
            else
            {
                //enable canvases
                var canvases = GetComponentsInChildren<Canvas>();
                foreach (var c in canvases)
                    c.enabled = true;
                FullscreenDisplay.SetVisible(false);
            }
        }
    }
}