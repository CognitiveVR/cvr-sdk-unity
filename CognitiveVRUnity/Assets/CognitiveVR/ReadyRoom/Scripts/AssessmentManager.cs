using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Instance created from assessment base on Start
//holds ordered collection of assessments to complete in the ready room

public class AssessmentManager
{
    //Index of the current Assessment in the AllAssessments list
    int CurrentAssessmentIndex = -1;
    
    //Ordered list of all assessments to be run in the Ready Room
    List<AssessmentBase> AllAssessments;

    public static AssessmentManager Instance;

    //create an Assessment Manager instance and find + sort all assessments in the scene
    public AssessmentManager()
    {
        Instance = this;
        AllAssessments = new List<AssessmentBase>(Object.FindObjectsOfType<AssessmentBase>());
        
        AllAssessments.Sort(delegate (AssessmentBase a, AssessmentBase b)
        {
            return a.Order.CompareTo(b.Order);
        });

        CurrentAssessmentIndex = -1;

        if (Application.isPlaying)
            ActivateNextAssessment();
    }

    //Iterate to the next Assessment and call BeginAssessment on it
    public AssessmentBase ActivateNextAssessment()
    {
        if (AllAssessments.Count > CurrentAssessmentIndex)
        {
            var current = AllAssessments[CurrentAssessmentIndex];
            if (current == null)
            {
                Debug.LogError("Assessment Manager returned null assessment!");
                CurrentAssessmentIndex++;
                return ActivateNextAssessment();
            }
            else
            {
                Debug.Log(">>>   Assessment Manager begin assessment " + current);
                CurrentAssessmentIndex++;
                current.BeginAssessment();
                return current;
            }
        }
        return null;
    }
}
