using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using CognitiveVR;

//used in cognitivevr exit poll to call actions on the main exit poll panel

namespace CognitiveVR
{
    public class GazeButton : MonoBehaviour
    {
        [Header("Gaze Settings")]
        public Image Button;
        public Image Fill;

        public float LookTime = 1.5f;

        //float _dotThreshold = 0.99f;
        float _currentLookTime;

        //this is used to increase the dot product threshold as distance increases - basically a very cheap raycast
        public float Radius = 0.25f;
        float _distanceToTarget;
        float _angle;
        float _theta;

        public UnityEngine.Events.UnityEvent OnLook;

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
            if (lastSearchFrame != Time.frameCount && pointer == null)
            {
                lastSearchFrame = Time.frameCount;
                pointer = FindObjectOfType<ExitPollPointer>();
            }
        }

        static ExitPollPointer pointer;
        static int lastSearchFrame = -1;

        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        void Update()
        {
            if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.NextResponseTimeValid == false) { return; }
            if (OnLook == null) { return; }

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
                        ActivateAction();
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
                        ActivateAction();
                    }
                }
                else if (tt < _theta * pointer.Stickiness && pointer.Target == transform) //bendy line pointing too far away from button
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
                        ActivateAction();
                    }
                }
            }
        }

        void UpdateFillAmount()
        {
            Fill.fillAmount = _currentLookTime / LookTime;
        }

        public void ActivateAction()
        {
            OnLook.Invoke();
        }

        public void ClearAction()
        {
            _currentLookTime = 0;
            UpdateFillAmount();
        }

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