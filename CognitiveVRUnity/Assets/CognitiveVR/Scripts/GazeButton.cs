using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using CognitiveVR;

//used in cognitivevr exit poll to call actions on the main exit poll panel

namespace CognitiveVR
{
    public class GazeButton : MonoBehaviour
    {
        public Image Button;
        public Image Fill;

        static float LookTime = 1f;

        float _dotThreshold = 0.95f;
        float _currentLookTime;

        System.Action _action;

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
            UpdateFillAmount();
        }

        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        void Update()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            if (Vector3.Dot(CognitiveVR_Manager.HMD.forward, CognitiveVR_Manager.HMD.position - _transform.position) > _dotThreshold)
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

        public void SetAction(System.Action newAction)
        {
            _action = newAction;
        }

        public void ActivateAction()
        {
            _action.Invoke();
        }

        //used to stop Update from changing the fill amount on the buttons
        public void SetEnabled(bool enabled)
        {
            this.enabled = enabled;
        }
    }
}