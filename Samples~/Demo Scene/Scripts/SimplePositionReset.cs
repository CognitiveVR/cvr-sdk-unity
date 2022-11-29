using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//resets the position of an object if it falls outside of the world
//also sends a custom event when that happens

namespace Cognitive3D.Demo
{
    public class SimplePositionReset : MonoBehaviour
    {
        private Vector3 StartingPosition;
        void Start()
        {
            StartingPosition = transform.position;
        }

        void FixedUpdate()
        {
            //fell off the world
            if (transform.position.y < -10)
            {
                RecordCustomEvent();
                ResetPosition();
            }
        }

        void RecordCustomEvent()
        {
            Cognitive3D.CustomEvent customEvent = new Cognitive3D.CustomEvent("Fell off World");
            Cognitive3D.DynamicObject dynamicObject = GetComponent<Cognitive3D.DynamicObject>();
            if (dynamicObject != null)
            {
                customEvent.SetDynamicObject(dynamicObject.GetId());
            }
            customEvent.Send(transform.position);
        }

        void ResetPosition()
        {
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            transform.position = StartingPosition;
        }
    }
}