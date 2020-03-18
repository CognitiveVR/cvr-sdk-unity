using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class ObjectPositionAssessment : AssessmentBase
    {
        public Transform Destination;

        public enum DestinationEnterType
        {
            Distance,
            Trigger
        }

        [Tooltip("If null, this is automatically set to the HMD camera transform when enabled")]
        public Transform Target;
        public DestinationEnterType destinationEnterType;
        public float Distance = 0.1f;

        public UnityEngine.Events.UnityEvent OnEnter;
        bool hasVisited = false;

        //start a coroutine to check distance frequently
        public override void OnEnable()
        {
            base.OnEnable();
            if (Target == null)
                Target = CognitiveVR.GameplayReferences.HMD;
            if (hasVisited) { return; }
            if (destinationEnterType == DestinationEnterType.Distance)
            {
                StartCoroutine(CheckDistance());
            }
        }

        //check the distance between the target and this destination, ignoring vertical height
        IEnumerator CheckDistance()
        {
            var wait = new WaitForSeconds(0.2f);
            while (true)
            {
                yield return wait;
                if (hasVisited) { yield break; }
                if (Target == null) { Debug.Log("Room Space Destination Target is null!", this); yield break; }
                Vector3 position = transform.position;
                position.y = 0;

                Vector3 target = Target.position;
                target.y = 0;

                if (Vector3.Distance(position, target) < Distance)
                {
                    OnEnter.Invoke();
                    CompleteAssessment();
                    hasVisited = true;
                    GetComponent<MeshRenderer>().enabled = false;
                    yield break;
                }
            }
        }

        //check that the entering trigger is a child of the target
        void OnTriggerEnter(Collider other)
        {
            if (destinationEnterType != DestinationEnterType.Trigger) { return; }
            if (hasVisited) { return; }
            if (Target == null) { Debug.Log("Room Space Destination Target is null!", this); return; }
            if (Target.IsChildOf(other.transform.root))
            {
                OnEnter.Invoke();
                CompleteAssessment();
                GetComponent<MeshRenderer>().enabled = false;
                hasVisited = true;
            }
        }
    }
}