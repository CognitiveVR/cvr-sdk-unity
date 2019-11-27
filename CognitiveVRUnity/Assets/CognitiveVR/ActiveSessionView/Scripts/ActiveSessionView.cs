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
        public RenderEyetracking RenderEyetracking;
        public Text WarningText;

        IEnumerator Start()
        {
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

            RenderEyetracking.Initialize(VRSceneCamera);
            var copy = VRSceneCamera.gameObject.AddComponent<CopyVRViewToRenderTexture>();
            copy.Initialize(MainCameraRenderImage);

            WarningText.gameObject.SetActive(false);
#if CVR_AH
            if (AdhawkApi.Calibrator.Instance != null)
            {
                if (!AdhawkApi.Calibrator.Instance.Calibrated)
                {
                    WarningText.text = "Eye Tracking not Calibrated";
                }

                while (!AdhawkApi.Calibrator.Instance.Calibrated)
                {
                    yield return new WaitForSeconds(1);
                    if (AdhawkApi.Calibrator.Instance == null) { break; }
                }

                if (!AdhawkApi.Calibrator.Instance.Calibrated)
                {
                    WarningText.text = "Eye Tracking Calibrated";
                    yield return new WaitForSeconds(2);
                    WarningText.enabled = false;
                }
            }
            else
            {
                WarningText.text = "Could not find Calibrator";
            }
#elif CVR_PUPIL
            if (calibrationController == null)
                calibrationController = FindObjectOfType<PupilLabs.CalibrationController>();
            if (calibrationController != null)
            {
                calibrationController.OnCalibrationSucceeded += PupilLabs_OnCalibrationSucceeded;
                calibrationController.OnCalibrationStarted += PupilLabs_OnCalibrationStarted;
                calibrationController.OnCalibrationFailed += PupilLabs_OnCalibrationFailed;
            }
            else
            {
                WarningText.text = "Could not find Calibration Controller";
            }
#elif CVR_TOBIIVR
            WarningText.enabled = false;
#elif CVR_FOVE
            if (!Fove.Unity.FoveManager.IsEyeTrackingCalibrated())
            {
                WarningText.text = "Eye Tracking not Calibrated";
            }

            while (Fove.Unity.FoveManager.IsEyeTrackingCalibrating() || !Fove.Unity.FoveManager.IsEyeTrackingCalibrated())
            {
                if (Fove.Unity.FoveManager.IsEyeTrackingCalibrating())
                    WarningText.text = "Calibration In Progress";
                yield return new WaitForSeconds(1);
            }

            if (Fove.Unity.FoveManager.IsEyeTrackingCalibrated())
            {
                WarningText.text = "Eye Tracking Calibrated";
                yield return new WaitForSeconds(2);
                WarningText.enabled = false;
            }
#elif CVR_VIVEPROEYE
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
#elif CVR_VARJO
            if (Varjo.VarjoPlugin.GetGaze().status == Varjo.VarjoPlugin.GazeStatus.VALID)
            {
                WarningText.text = "Eye Tracking Calibrated";
                yield return new WaitForSeconds(2);
                WarningText.enabled = false;
            }
            else
            {
                WarningText.text = "Eye Tracking not Calibrated";
            }
#else
            WarningText.text = "No Eye Tracking Found!";
#endif
        }

#if CVR_PUPIL
        PupilLabs.CalibrationController calibrationController;

        private void PupilLabs_OnCalibrationSucceeded()
        {
            WarningText.text = "Calibration Successful";
            calibrationController.OnCalibrationSucceeded -= PupilLabs_OnCalibrationSucceeded;
            calibrationController.OnCalibrationStarted -= PupilLabs_OnCalibrationStarted;
            calibrationController.OnCalibrationFailed -= PupilLabs_OnCalibrationFailed;
        }

        private void PupilLabs_OnCalibrationFailed()
        {
            WarningText.text = "Calibration Failed";
        }

        private void PupilLabs_OnCalibrationStarted()
        {
            WarningText.text = "Is Calibrating";
        }
#endif
    }
}