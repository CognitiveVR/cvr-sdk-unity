using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using CognitiveVR;
using System.IO;

//used in cognitivevr exit poll to call actions on the main exit poll panel

namespace CognitiveVR
{
    public class MicrophoneButton : MonoBehaviour
    {
        public Image Button;
        public Image Fill;

        public float LookTime = 1.5f;

        float _currentLookTime;
        float _currentRecordTime;

        //this is used to increase the dot product threshold as distance increases - basically a very cheap raycast
        public float Radius = 0.25f;
        float _distanceToTarget;
        float _angle;
        float _theta;

        //call this after the recording sent response, or _maxUploadWaitTime seconds after sent request
        public UnityEngine.EventSystems.EventTrigger.TriggerEvent OnFinishedRecording;

        private int outputRate = 16000;// 44100;
        //private int headerSize = 44; //default for uncompressed wav

        AudioClip clip;

        bool _recording;
        bool _finishedRecording;
        float _maxUploadWaitTime = 2;

        public int RecordTime = 10;

        public Image MicrophoneImage;
        public Image MicrophoneBackgroundImage;
        public Color LowVolumeColor;
        public Color HighVolumeColor;

        Transform _t;
        Transform _transform
        {
            get
            {
                if (_t == null)
                {
                    _t = transform;
                }
                return _t;
            }
        }

        void OnEnable()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            _currentLookTime = 0;
            UpdateFillAmount();
            _distanceToTarget = Vector3.Distance(CognitiveVR_Manager.HMD.position, _transform.position);
            _angle = Mathf.Atan(Radius / _distanceToTarget);
            _theta = Mathf.Cos(_angle);
            MicrophoneImage.transform.localScale = Vector3.one;
            Fill.color = Color.white;
            MicrophoneBackgroundImage.color = LowVolumeColor;
        }

        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        void Update()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            if (ExitPollPanel.NextResponseTime > Time.time) { return; }
            if (_finishedRecording) { return; }

            if (_recording)
            {
                _currentRecordTime -= Time.deltaTime;
                UpdateFillAmount();
                float volumeLevel = MicrophoneUtility.LevelMax(clip);
                MicrophoneImage.transform.localScale = Vector3.Lerp(MicrophoneImage.transform.localScale, Vector3.one * 0.5f + Vector3.one * Mathf.Clamp(volumeLevel, 0, 0.5f), 0.1f);
                MicrophoneBackgroundImage.color = Color.Lerp(LowVolumeColor, Color.Lerp(MicrophoneBackgroundImage.color, HighVolumeColor, Mathf.Clamp(volumeLevel, 0, 1f)), 0.1f);

                if (_currentRecordTime <= 0)
                {
                    Microphone.End(null);
                    StartCoroutine(UploadAudio());
                    _finishedRecording = true;
                }
            }
            else
            {
                if (Vector3.Dot(CognitiveVR_Manager.HMD.forward, (_transform.position - CognitiveVR_Manager.HMD.position).normalized) > _theta)
                {
                    _currentLookTime += Time.deltaTime;
                    UpdateFillAmount();

                    //maybe also scale button slightly if it has focus

                    if (_currentLookTime >= LookTime)
                    {
                        Debug.Log("recording");
                        // Call this to start recording. 'null' in the first argument selects the default microphone. Add some mic checking later
                        clip = Microphone.Start(null, false, RecordTime, outputRate);
                        Fill.color = Color.red;

                        GetComponentInParent<ExitPollPanel>().DisableTimeout();

                        _currentRecordTime = RecordTime;
                        _finishedRecording = false;
                        _recording = true;
                    }
                }
                else if (_currentLookTime > 0)
                {
                    _currentLookTime = 0;
                    UpdateFillAmount();
                }
            }
        }

        IEnumerator UploadAudio()
        {
            //customer id or something

            string url = "https://api.cognitivevr.io/polls/"+ExitPollPanel.PollID+"/feedback";

            byte[] bytes;
            CognitiveVR.MicrophoneUtility.Save(clip, out bytes);
            
            WWWForm form = new WWWForm();
            form.headers.Add("X-HTTP-Method-Override", "POST");
            form.AddBinaryData("feedback", bytes);
            WWW www = new UnityEngine.WWW(url, form);


            float startTime = 0;
            while (startTime < _maxUploadWaitTime) //give 2 seconds to upload before closing panel. can still upload in the background
            {
                startTime += Time.deltaTime;
                if (www.isDone) { break; }
                yield return null;
            }
            
            ActivateAction();
        }

        void UpdateFillAmount()
        {
            if (_recording)
            {
                Fill.fillAmount = _currentRecordTime / RecordTime;
            }
            else
            {
                Fill.fillAmount = _currentLookTime / LookTime;
            }
        }

        public void ActivateAction()
        {
            OnFinishedRecording.Invoke(null);
        }

        public void ClearAction()
        {
            //_action = null;
            _currentLookTime = 0;
            UpdateFillAmount();
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}