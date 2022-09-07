using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Instance created from assessment base on Start
//holds ordered collection of assessments to complete in the ready room

//TODO make this a monobehaviour and just iterate through a list of assessments

namespace Cognitive3D
{
    public class AssessmentManager : MonoBehaviour
    {
        public static AssessmentManager Instance { get; private set; }

        //Index of the current Assessment in the AllAssessments list
        int CurrentAssessmentIndex = -1;

        //Ordered list of all assessments to be run in the Ready Room
        public List<AssessmentBase> AllAssessments;

        private void Awake()
        {
            Instance = this;
            foreach (var assessment in AllAssessments)
            {
                assessment.gameObject.SetActive(false);
            }
        }

        private void Start()
        {
            CurrentAssessmentIndex = -1;
            ActivateNextAssessment();
        }

        //Iterate to the next Assessment and call BeginAssessment on it
        private AssessmentBase ActivateNextAssessment()
        {
            CurrentAssessmentIndex++;
            if (AllAssessments.Count <= CurrentAssessmentIndex)
            {
                return null;
            }

            var current = AllAssessments[CurrentAssessmentIndex];
            if (current == null)
            {
                Debug.LogError("Assessment Manager returned null assessment!");
                return ActivateNextAssessment();
                //return null;
            }
            if (!current.IsValid())
            {
                Debug.LogWarning("Assessment Manager has requirements not met. Skipping " + current.gameObject.name);
                return ActivateNextAssessment();
            }
            else
            {
                Debug.Log("start assessment " + current.gameObject.name);
                current.gameObject.SetActive(true);
                current.BeginAssessment();
                return current;
            }
        }



        public delegate void onAssessmentBegin();
        public delegate void onAssessmentComplete();
        /// <summary>
        /// called after the assessment has disabled itself.
        /// </summary>
        public static event onAssessmentComplete OnAssessmentComplete;
        internal static void InvokeCompleteAssessmentEvent()
        {
            Instance.ActivateNextAssessment();
        }
    }
}