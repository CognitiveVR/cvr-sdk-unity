using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using CognitiveVR;
using System.IO;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

//used in cognitivevr exit poll to call actions on the main exit poll panel

namespace CognitiveVR
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

        ExitPollSet questionSet;
        public void SetExitPollQuestionSet(ExitPollSet questionSet)
        {
            this.questionSet = questionSet;
        }

        protected virtual void OnEnable()
        {
            if (GameplayReferences.HMD == null) { return; }
            FillAmount = 0;
            UpdateFillAmount();
        }


        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        void Update()
        {
            if (GameplayReferences.HMD == null) { return; }
            //if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.NextResponseTimeValid == false) { return; }
            if (_finishedRecording) { return; }
            //if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.IsClosing) { return; }

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
                    CognitiveVR.MicrophoneUtility.Save(clip, out bytes);
                    string encodedWav = MicrophoneUtility.EncodeWav(bytes);
                    questionSet.CurrentExitPollPanel.AnswerMicrophone(encodedWav); //TODO need some method of accessing question set on this component - probably pass in at start
                    _finishedRecording = true;
                }
            }
        }

        protected override void LateUpdate()
        {
            //if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.NextResponseTimeValid == false) { return; }
            if (OnFill == null) { return; }
            //if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.IsClosing) { return; }

            //set fill visual
            //check for over fill threshold to activate action

            if (focusThisFrame)
            {
                FillAmount += Time.deltaTime;
                UpdateFillAmount();
                if (FillAmount >= FillDuration)
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
            // Call this to start recording. 'null' in the first argument selects the default microphone. Add some mic checking later
            clip = Microphone.Start(null, false, RecordTime, outputRate);
            fillImage.color = Color.red;

            GetComponentInParent<ExitPollPanel>().DisableTimeout();
            _currentRecordTime = RecordTime;
            _finishedRecording = false;
            _recording = true;
            TipText.text = "Recording...";
        }

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