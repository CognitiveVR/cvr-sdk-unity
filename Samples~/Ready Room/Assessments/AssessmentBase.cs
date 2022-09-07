using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for tasks in Ready Room
//You can directly call CompleteAssessment or create a subclass for additional logic

namespace Cognitive3D
{
    public class AssessmentBase : MonoBehaviour
    {
        // -------- meta data about how the assessment manager should display this assessment

        //used in the editor to indicate if this Assessment will be activated
        [HideInInspector]
        public bool Active = true;

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
            if (RequiresEyeTracking == true && !GameplayReferences.SDKSupportsEyeTracking)
            {
                return false;
            }
            if (RequiresGrabbing == true && !GameplayReferences.SDKSupportsControllers)
            {
                return false;
            }
            if (RequiresRoomScale == true && !GameplayReferences.SDKSupportsRoomSize)
            {
                return false;
            }
            return true;
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