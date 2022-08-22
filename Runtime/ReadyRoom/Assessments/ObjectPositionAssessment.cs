using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class ObjectPositionAssessment : AssessmentBase
    {
        public Transform Destination;

        [Tooltip("If null, this assessment will immediately complete")]
        public Transform Target;
        public float Distance = 0.1f;

        public UnityEngine.Events.UnityEvent OnEnter;
        bool hasVisited = false;

        //start a coroutine to check distance frequently
        public override void OnEnable()
        {
            base.OnEnable();
            if (Target == null)
            {
                CompleteAssessment();
                return;
            }
            if (hasVisited) { return; }
            StartCoroutine(CheckDistance());
        }

        //check the distance between the target and this destination
        IEnumerator CheckDistance()
        {
            var wait = new WaitForSeconds(0.2f);
            while (true)
            {
                yield return wait;
                if (hasVisited) { yield break; }
                if (Target == null) { Debug.Log("Object Position Assessment Target is null!", this); yield break; }

                if (Vector3.Distance(Destination.position, Target.position) < Distance)
                {
                    OnEnter.Invoke();
                    CompleteAssessment();
                    hasVisited = true;
                    GetComponent<MeshRenderer>().enabled = false;
                    yield break;
                }
            }
        }
    }
}