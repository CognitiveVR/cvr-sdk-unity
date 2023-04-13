using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for tasks in Ready Room
//You can directly call CompleteAssessment or create a subclass for additional logic

namespace Cognitive3D.ReadyRoom
{
    public class AssessmentBase : MonoBehaviour
    {
        // -------- meta data about how the assessment manager should display this assessment

        //indicates that this assessment is only valid if Eye Tracking SDK is present
        public bool RequiresEyeTracking;
        //indicates that this assessment is only valid if Room Scale is configured
        public bool RequiresRoomScale;
        //indicates that this assessment is only valid if controllers and an interaction system allows picking up objects
        public bool RequiresGrabbing;

        // -------- variables and events about this assessment's logic
        [Space(10)]

        // -------- internal logic and state
        protected bool hasBegun;
        protected bool hasCompleted;

        public delegate void onAssessmentStateChanged();
        public event onAssessmentStateChanged OnAssessmentBegin;
        public event onAssessmentStateChanged OnAssessmentComplete;

        public virtual void OnEnable()
        {
            
        }

        //enables child gameobjects and calls OnAssessmentBegin event
        public virtual void BeginAssessment()
        {
            if (hasBegun) { return; }
            hasBegun = true;

            if (OnAssessmentBegin != null)
                OnAssessmentBegin.Invoke();
        }

        //checks if sdk supports required features (eye tracking, room scale, controllers)
        public virtual bool IsValid()
        {
            if (AssessmentManager.Instance == null) { return false; }

            if (RequiresEyeTracking == true && !AssessmentManager.Instance.AllowEyeTrackingAssessments)
            {
                return false;
            }
            if (RequiresGrabbing == true && !AssessmentManager.Instance.AllowGrabbingAssessments)
            {
                return false;
            }
            if (RequiresRoomScale == true && !AssessmentManager.Instance.AllowRoomScaleAssessments)
            {
                return false;
            }
            return true;
        }

        public virtual string InvalidReason()
        {
            if (AssessmentManager.Instance == null) { return "Assessment Manager is missing"; }
            if (RequiresEyeTracking == true && !AssessmentManager.Instance.AllowEyeTrackingAssessments) { return "Eye Tracking is required but not enabled"; }
            if (RequiresGrabbing == true && !AssessmentManager.Instance.AllowGrabbingAssessments) { return "Grabbing Objects is required but not enabled"; }
            if (RequiresRoomScale == true && !AssessmentManager.Instance.AllowRoomScaleAssessments) { return "Room Scale is required but not enabled"; }
            return string.Empty;
        }

        //calls OnAssessmentComplete event and disables child gameobjects
        //also calls ActivateNextAssessment
        [ContextMenu("DEBUG Complete")]
        public virtual void CompleteAssessment()
        {
            if (hasCompleted) { return; }
            hasCompleted = false;

            if (OnAssessmentComplete != null)
                OnAssessmentComplete.Invoke();

            //some VR interaction systems may need to destroy objects to make sure they correctly drop from the player's hand
#if C3D_STEAMVR2
            if (Valve.VR.InteractionSystem.Player.instance != null)
            {
                foreach (var v in Valve.VR.InteractionSystem.Player.instance.leftHand.AttachedObjects)
                {
                    Object.Destroy(v.attachedObject);
                }
                foreach (var v in Valve.VR.InteractionSystem.Player.instance.rightHand.AttachedObjects)
                {
                    Object.Destroy(v.attachedObject);
                }
            }
#endif

            gameObject.SetActive(false);
            AssessmentManager.InvokeCompleteAssessmentEvent();
        }
    }
}