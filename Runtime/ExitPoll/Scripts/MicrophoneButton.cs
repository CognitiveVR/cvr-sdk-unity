using UnityEngine;
using System.Collections;
using UnityEngine.UI;

//used in ExitPoll to record participant's voice
//on completion, will encode the audio to a wav and pass a base64 string of the data to the ExitPoll

namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Internal/Microphone Button")]
    public class MicrophoneButton : VirtualButton
    {
        [Header("Gaze Settings")]
        float _currentRecordTime;

        [Header("Recording")]
        public int RecordTime = 10;
        private int outputRate = 16000;
        AudioClip clip;
        bool _recording;
        private bool _recordingCooldown;

        [Header("Visuals")]
        public Image MicrophoneImage;
        public Text buttonPrompt;
        ExitPollSet questionSet;

        public void SetExitPollQuestionSet(ExitPollSet questionSet)
        {
            this.questionSet = questionSet;
        }

        protected virtual void OnEnable()
        {
            if (GameplayReferences.HMD == null) { return; }
            if (FindObjectOfType<ExitPollHolder>().Parameters.PointerType == ExitPollManager.PointerType.HMD)
            {
                buttonPrompt.text = "Hover To Record";
            }
            else
            {
                buttonPrompt.text = "When ready to record click the record button";
            }
            FillAmount = 0;
            UpdateFillAmount();
        }

        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        //uses QuestionSet's CurrentExitPollPanel to pass base64 string to panel using the 'AnswerMicrophone' method

        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }

#if UNITY_WEBGL
            // Microphone not supported on WebGL
#else
            if (_recording)
            {
                _currentRecordTime -= Time.deltaTime;
                UpdateFillAmount();
                float volumeLevel = MicrophoneUtility.LevelMax(clip);
                Vector3 newScale = new Vector3(0.8f, 0.1f + Mathf.Clamp(volumeLevel, 0, 0.7f), 0.8f);
                MicrophoneImage.transform.localScale = Vector3.Lerp(MicrophoneImage.transform.localScale, newScale, 0.1f);

                if (_currentRecordTime <= 0)
                {
                    StopRecording();
                }
            }

            if (focusThisFrame && !_recordingCooldown)
            {
                if (_recording)
                {
                    StopRecording();
                }
                else
                {
                    StartRecording();
                }

                StartCoroutine(ResetFocusCooldown());
            }
#endif
        }

        // Increase the fill amount if this image was focused this frame. Calls RecorderActivate if past threshold
        protected override void LateUpdate()
        {
            if (OnConfirm == null) { return; }

            if (focusThisFrame && !_recording && !_recordingCooldown)
            {
                // Gradually fill
                if (slowFill)
                {
                    FillAmount += Time.deltaTime;
                    UpdateFillAmount();
                    if (FillAmount >= FillDuration)
                    {
                        StartRecording();
                        StartCoroutine(ResetFocusCooldown());
                    }
                }
                else // Immediately activate
                {
                    StartRecording();
                    StartCoroutine(ResetFocusCooldown());
                }
            }
            else if (FillAmount > 0 && !_recording)
            {
                FillAmount = 0;
                UpdateFillAmount();
            }
            focusThisFrame = false;
        }

        void StartRecording()
        {
#if UNITY_WEBGL
            return; // Microphone not supported on WebGL
#else
            clip = Microphone.Start(null, false, RecordTime, outputRate);
#endif
            fillImage.color = Color.red;

            _currentRecordTime = RecordTime;
            _recording = true;
            buttonPrompt.text = "Recording...";
        }

        void StopRecording()
        {
            Microphone.End(null);
            byte[] bytes;
            Cognitive3D.MicrophoneUtility.Save(clip, out bytes);
            string encodedWav = MicrophoneUtility.EncodeWav(bytes);
            questionSet.CurrentExitPollPanel.AnswerMicrophone(encodedWav);
            buttonPrompt.text = "Recording saved\nPress again to re-record";

            _recording = false;
            FillAmount = 0; // Reset the fill amount
            UpdateFillAmount();
        }

        // Cooldown to prevent immediate toggling
        private IEnumerator ResetFocusCooldown()
        {
            _recordingCooldown = true; // Set cooldown
            yield return new WaitForSeconds(0.2f); // Adjust the cooldown duration as needed
            _recordingCooldown = false;
        }

        //increases the fill image if confirming selection with a pointer. lowers as recording time increases
        protected void UpdateFillAmount()
        {
            if (_recording)
            {
                fillImage.fillAmount = _currentRecordTime / RecordTime;
            }
            else
            {
                fillImage.fillAmount = FillAmount / FillDuration;
            }
        }
    }
}