using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Cognitive3D;
using System.IO;
using UnityEngine.XR;

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
        bool _finishedRecording;

        [Header("Visuals")]
        public Image MicrophoneImage;
        public Text TipText;
        public Text buttonPrompt;
        ExitPollSet questionSet;

        public void SetExitPollQuestionSet(ExitPollSet questionSet)
        {
            this.questionSet = questionSet;
        }

        protected virtual void OnEnable()
        {
            if (GameplayReferences.HMD == null) { return; }
            if (FindObjectOfType<ExitPollHolder>().Parameters.PointerType == ExitPoll.PointerType.HMDPointer)
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
            if (_finishedRecording) { return; }

#if UNITY_WEBGL
            //microphone not support on webgl
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
                    Microphone.End(null);
                    byte[] bytes;
                    Cognitive3D.MicrophoneUtility.Save(clip, out bytes);
                    string encodedWav = MicrophoneUtility.EncodeWav(bytes);
                    questionSet.CurrentExitPollPanel.AnswerMicrophone(encodedWav);
                    _finishedRecording = true;
                }
            }
#endif
        }

        //increase the fill amount if this image was focused this frame. calls RecorderActivate if past threshold
        protected override void LateUpdate()
        {
            if (OnConfirm == null) { return; }
            if (_recording) { return; }
            if (_finishedRecording) { return; }

            if (focusThisFrame)
            {
                // Gradually fill
                if (slowFill)
                {
                    FillAmount += Time.deltaTime;
                    UpdateFillAmount();
                    if (FillAmount >= FillDuration)
                    {
                        RecorderActivate();
                    }
                }
                else // Immediately activate
                {
                    RecorderActivate();
                }
            }
            else if (FillAmount > 0)
            {
                FillAmount = 0;
                UpdateFillAmount();
            }
            focusThisFrame = false;
        }

        void RecorderActivate()
        {
#if UNITY_WEBGL
            //microphone not supported on webgl
            return;
#else
            clip = Microphone.Start(null, false, RecordTime, outputRate);
#endif
            fillImage.color = Color.red;

            _currentRecordTime = RecordTime;
            _finishedRecording = false;
            _recording = true;
            TipText.text = "Recording...";
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