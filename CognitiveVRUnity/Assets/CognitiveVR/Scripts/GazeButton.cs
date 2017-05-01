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
        }

        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        void Update()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.NextResponseTimeValid == false) { return; }
            if (OnLook == null) { return; }

            if (Vector3.Dot(CognitiveVR_Manager.HMD.forward, (_transform.position - CognitiveVR_Manager.HMD.position).normalized) > _theta)
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

        void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}