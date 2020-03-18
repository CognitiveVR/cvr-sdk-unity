using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//complete when the user looks at each target

namespace CognitiveVR
{
    public class EyeTrackingAssessment : AssessmentBase
    {
        public List<HMDFocusButton> Targets = new List<HMDFocusButton>();
        int targetsRemaining;

        public override void BeginAssessment()
        {
            targetsRemaining = 0;
            for (int i = 0; i < Targets.Count; i++)
            {
                if (Targets[i] != null)
                    targetsRemaining++;
            }
            base.BeginAssessment();
        }

        public void ActivateTarget()
        {
            targetsRemaining--;
            if (targetsRemaining <= 0)
            {
                Invoke("Delay", 1);
            }
        }

        void Delay()
        {
            CompleteAssessment();
        }
    }
}