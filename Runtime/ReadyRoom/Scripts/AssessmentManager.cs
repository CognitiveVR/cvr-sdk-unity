using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Instance created from assessment base on Start
//holds ordered collection of assessments to complete in the ready room

namespace CognitiveVR
{
    public class AssessmentManager
    {
        //Index of the current Assessment in the AllAssessments list
        int CurrentAssessmentIndex = -1;

        //Ordered list of all assessments to be run in the Ready Room
        List<AssessmentBase> AllAssessments;

        public static AssessmentManager Instance;

        //create an Assessment Manager instance and find + sort all assessments in the scene
        //called from AssessmentBase on Start()
        public AssessmentManager()
        {
            Instance = this;
            AllAssessments = new List<AssessmentBase>(Object.FindObjectsOfType<AssessmentBase>());

            AllAssessments.RemoveAll(delegate (AssessmentBase obj) { return obj.Active == false; });

            AllAssessments.Sort(delegate (AssessmentBase a, AssessmentBase b)
            {
                return a.Order.CompareTo(b.Order);
            });

            CurrentAssessmentIndex = -1;
            ActivateNextAssessment();
        }

        //Iterate to the next Assessment and call BeginAssessment on it
        public AssessmentBase ActivateNextAssessment()
        {
            if (AllAssessments.Count > CurrentAssessmentIndex)
            {
                CurrentAssessmentIndex++;
                var current = AllAssessments[CurrentAssessmentIndex];
                if (current == null)
                {
                    Debug.LogError("Assessment Manager returned null assessment!");
                    return ActivateNextAssessment();
                    //return null;
                }
                else
                {
                    current.BeginAssessment();
                    return current;
                }
            }
            return null;
        }
    }
}