using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Base class for tasks in Ready Room
//You can directly call CompleteAssessment or create a subclass for additional logic

public class AssessmentBase : MonoBehaviour
{
    //used in the editor to indicate if this Assessment should be 
    [HideInInspector]
    public bool Active = true;

    //used by the AssessmentManager to sort assessments
    [HideInInspector]
    public int Order = 0;

    public delegate void onAssessmentStateChanged();
    public event onAssessmentStateChanged OnAssessmentBegin;
    public event onAssessmentStateChanged OnAssessmentComplete;

    //a list of objects to disable when assessment is completed
    //can be used to disable objects the player is grabbing, if these are normally parented to player hands
    public List<GameObject> ControlledByAssessmentState;

    //indicates that this assessment is only valid if Eye Tracking SDK is present
    public bool RequiresEyeTracking;
    //indicates that this assessment is only valid if Room Scale is configured
    public bool RequiresRoomScale;
    //indicates that this assessment is only valid if interaction system allows picking up objects
    public bool RequiresGrabbing;

    protected bool hasBegun;
    protected bool hasCompleted;

    //disable all child gameobjects
    public virtual void OnEnable()
    {
        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < ControlledByAssessmentState.Count; i++)
        {
            if (ControlledByAssessmentState[i] == null) { continue; }
            ControlledByAssessmentState[i].SetActive(false);
        }
    }

    //delay startup - needs to wait until all assessment steps have disabled their child gameobjects in OnEnable
    protected virtual void Start()
    {
        if (AssessmentManager.Instance == null)
        {
            new AssessmentManager();
        }
    }

    //enables child gameobjects and calls OnAssessmentBegin event
    public virtual void BeginAssessment()
    {
        if (hasBegun) { return; }
        hasBegun = true;

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }

        for (int i = 0; i < ControlledByAssessmentState.Count; i++)
        {
            if (ControlledByAssessmentState[i] == null) { continue; }
            ControlledByAssessmentState[i].SetActive(true);
        }

        if (OnAssessmentBegin != null)
            OnAssessmentBegin.Invoke();
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

        int childCount = transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }

        for (int i = 0; i < ControlledByAssessmentState.Count; i++)
        {
            if (ControlledByAssessmentState[i] == null) { continue; }
            ControlledByAssessmentState[i].SetActive(false);
        }
        AssessmentManager.Instance.ActivateNextAssessment();
    }
}
