using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//marks an area that should be 'visited' by the player within the room boundaries

namespace CognitiveVR
{
    public class RoomSpaceDestination : MonoBehaviour
    {
        [Tooltip("If null, this is automatically set to the HMD camera transform when enabled")]
        public Transform Target;
        public float Distance = 1;

        public UnityEngine.Events.UnityEvent OnEnter;
        bool hasVisited = false;

        //start a coroutine to check distance frequently
        void OnEnable()
        {
            if (Target == null)
                Target = CognitiveVR.GameplayReferences.HMD;
            if (hasVisited) { return; }
            StartCoroutine(CheckDistance());
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
                    hasVisited = true;
                    GetComponent<MeshRenderer>().enabled = false;
                    yield break;
                }
            }
        }
    }
}