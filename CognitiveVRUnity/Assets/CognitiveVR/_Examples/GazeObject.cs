using UnityEngine;
using System.Collections;

/// =====================================================
/// add this to any object in the scene. this will send the total look time in a set interval
///
/// there are many ways to detect where the player is looking. this is just one example
/// raycast, spherecast, dot product, renderer.isvisible, monobehaviour.onbecomevisible
/// =====================================================

namespace CognitiveVR
{
    public class GazeObject : MonoBehaviour
    {
        //cached components
        Transform cameraTransform;
        Transform myTransform;
        Collider myCollider;

        //gaze interval and duration
        float GazeObjectSendInterval = 10;
        float nextIntervalTime;
        float lookDuration;

        //tracking properties
        public string GazeObjectName = "unique object name";
        public float MaxAngle = 10;
        public bool CheckLineOfSight = true;
        [Tooltip("Max Distance is ignored if value < 0")]
        public float MaxDistance = -1;

        void Start()
        {
            cameraTransform = Camera.main.transform;
            myCollider = GetComponent<Collider>();
            myTransform = GetComponent<Transform>();
            
            nextIntervalTime = Time.time + GazeObjectSendInterval;
        }

        void Update()
        {
            UpdateSendTime();

            //check distance to object
            if (MaxDistance > 0 && Mathf.Pow(MaxDistance, 2) < Vector3.SqrMagnitude(cameraTransform.position - myTransform.position))
            {
                return;
            }

            //check direction and line of sight
            if (Vector3.Angle(cameraTransform.forward, (myTransform.position - cameraTransform.position).normalized) <= MaxAngle)
            {
                if (!CheckLineOfSight)
                {
                    AddLookTime();
                    return;
                }

                RaycastHit hit = new RaycastHit();
                if (Physics.Linecast(cameraTransform.position, myTransform.position, out hit))
                {
                    //hit this gameobject's collider
                    if (hit.collider == myCollider)
                    {
                        AddLookTime();
                    }
                }
                else
                {
                    //no collider to hit. nothing between the two points
                    AddLookTime();
                }
            }
        }

        void UpdateSendTime()
        {
            //send gaze duration infrequently
            if (Time.time > nextIntervalTime)
            {
                if (lookDuration > 0)
                {
                    CognitiveVR.Instrumentation.Transaction("gazeobject.look").setProperty("name", GazeObjectName).setProperty("duration", lookDuration).beginAndEnd();
                    lookDuration = 0;
                }
                nextIntervalTime = Time.time + GazeObjectSendInterval;
            }
        }

        void AddLookTime()
        {
            lookDuration += Time.deltaTime;
        }
    }
}
