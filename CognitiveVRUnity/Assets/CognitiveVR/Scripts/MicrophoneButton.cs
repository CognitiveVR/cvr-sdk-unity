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
        [Header("Gaze Settings")]
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

        [Header("Recording")]
        public int RecordTime = 10;
        private int outputRate = 16000;
        AudioClip clip;
        bool _recording;
        bool _finishedRecording;

        [Header("Visuals")]
        public Image MicrophoneImage;
        public Text TipText;

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
            if (!Application.isPlaying) { return; }
            if (CognitiveVR_Manager.HMD == null) { return; }
            _currentLookTime = 0;
            UpdateFillAmount();
            _distanceToTarget = Vector3.Distance(CognitiveVR_Manager.HMD.position, _transform.position);
            _angle = Mathf.Atan(Radius / _distanceToTarget);
            _theta = Mathf.Cos(_angle);
            MicrophoneImage.transform.localScale = Vector3.one;
            pointer = FindObjectOfType<ExitPollPointer>();
        }

        ExitPollPointer pointer;
        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        void Update()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.NextResponseTimeValid == false) { return; }
            if (_finishedRecording) { return; }

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
                    ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.AnswerMicrophone(encodedWav);
                    _finishedRecording = true;
                }
            }
            else
            {
                if (pointer == null)
                {
                    //use hmd
                    if (CognitiveVR_Manager.HMD == null) { return; }

                    if (Vector3.Dot(GetHMDForward(), (_transform.position - CognitiveVR_Manager.HMD.position).normalized) > _theta)
                    {
                        _currentLookTime += Time.deltaTime;
                        UpdateFillAmount();

                        //maybe also scale button slightly if it has focus

                        if (_currentLookTime >= LookTime)
                        {
                            RecorderActivate();
                        }
                    }
                    else if (_currentLookTime > 0)
                    {
                        _currentLookTime = 0;
                        UpdateFillAmount();
                    }
                }
                else //use pointer
                {
                    var tt = Vector3.Dot(pointer.transform.forward, (_transform.position - pointer.transform.position).normalized);
                    if (tt > _theta) //pointing at the button
                    {
                        pointer.Target = transform;
                        _currentLookTime += Time.deltaTime;
                        UpdateFillAmount();

                        if (_currentLookTime >= LookTime)
                        {
                            RecorderActivate();
                        }
                    }
                    else if (tt < _theta * pointer.Stiffness && pointer.Target == transform) //bendy line pointing too far away from button
                    {
                        pointer.Target = null;
                    }
                    else if (pointer.Target != transform) //selection is not this
                    {
                        if (_currentLookTime > 0)
                        {
                            _currentLookTime = 0;
                            UpdateFillAmount();
                        }
                    }
                    else //pointing nearby button
                    {
                        _currentLookTime += Time.deltaTime;
                        UpdateFillAmount();

                        if (_currentLookTime >= LookTime)
                        {
                            RecorderActivate();
                        }
                    }
                }
            }
        }

        void RecorderActivate()
        {
            // Call this to start recording. 'null' in the first argument selects the default microphone. Add some mic checking later
            clip = Microphone.Start(null, false, RecordTime, outputRate);
            Fill.color = Color.red;

            GetComponentInParent<ExitPollPanel>().DisableTimeout();
            pointer.Target = null;
            _currentRecordTime = RecordTime;
            _finishedRecording = false;
            _recording = true;
            TipText.text = "Recording...";
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

        /*public void ActivateAction()
        {
            OnFinishedRecording.Invoke(null);
        }

        public void ClearAction()
        {
            //_action = null;
            _currentLookTime = 0;
            UpdateFillAmount();
        }*/

        public Vector3 GetHMDForward()
        {
#if CVR_FOVE||CVR_PUPIL

#if CVR_FOVE
            if (CognitiveVR_Manager.FoveInstance != null)
            {
                var ray = CognitiveVR_Manager.FoveInstance.GetGazeRays();
                return ray.left.direction;
            }
#endif

#if CVR_PUPIL
            //TODO return pupil labs gaze direction
#endif

            return CognitiveVR_Manager.HMD.forward;
#else
            return CognitiveVR_Manager.HMD.forward;
#endif
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}