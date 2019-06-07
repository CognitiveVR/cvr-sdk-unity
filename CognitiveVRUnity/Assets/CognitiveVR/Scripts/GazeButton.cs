using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using CognitiveVR;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

//used in cognitivevr exit poll to call actions on the main exit poll panel

namespace CognitiveVR
{
    [AddComponentMenu("Cognitive3D/Internal/Gaze Button")]
    public class GazeButton : MonoBehaviour
    {
        public Image Button;
        public Image Fill;

        public float LookTime = 1.5f;

        //float _dotThreshold = 0.99f;
        protected float _currentLookTime;
        public UnityEngine.Events.UnityEvent OnLook;

        protected virtual void OnEnable()
        {
            if (GameplayReferences.HMD == null) { return; }
            _currentLookTime = 0;
            UpdateFillAmount();
        }

        protected bool lookedAtThisFrame = false;

        //if the player is looking at the button, updates the fill image and calls ActivateAction if filled
        protected virtual void LateUpdate()
        {
            //if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.NextResponseTimeValid == false) { return; }
            if (OnLook == null) { return; }
            //if (ExitPoll.CurrentExitPollSet.CurrentExitPollPanel.IsClosing) { return; }

            //set fill visual
            //check for over fill threshold to activate action

            if (lookedAtThisFrame)
            {
                _currentLookTime += Time.deltaTime;
                UpdateFillAmount();
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
            lookedAtThisFrame = false;
        }

        protected virtual void UpdateFillAmount()
        {
            Fill.fillAmount = _currentLookTime / LookTime;
        }

        /// <summary>
        /// call this from some pointer script to tell this button that it has the player's focus
        /// ie, the player is trying to press this button
        /// </summary>
        public void SetFocus()
        {
            lookedAtThisFrame = true;
        }

        protected virtual void ActivateAction()
        {
            OnLook.Invoke();
        }
    }
}