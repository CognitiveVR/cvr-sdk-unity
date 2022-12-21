using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//simple script to randomly move an object within a sphere

namespace Cognitive3D.Demo
{
    public class SimpleMover : MonoBehaviour
    {
		[SerializeField]
        private float MoveSpeed = 1;
		[SerializeField]
        private float RotateSpeed = 180;
		[SerializeField]
        private float Range = 3;
        private Vector3 TargetPosition;
        private Vector3 StartingPosition;

        private void Start()
        {
            StartingPosition = transform.position;
            PickNewTargetPosition();
        }

        void Update()
        {
            //at destination, choose a new position
            if (Vector3.Distance(transform.position, TargetPosition) < 0.1f)
            {
                PickNewTargetPosition();
            }

            //move forward and rotate toward target position
            transform.position = transform.position + transform.forward * Time.deltaTime * MoveSpeed;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(TargetPosition - transform.position), Time.deltaTime * RotateSpeed);
        }

        void PickNewTargetPosition()
        {
            TargetPosition = StartingPosition + Random.insideUnitSphere * Range;
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying)
            {
                Gizmos.DrawWireSphere(StartingPosition, Range);
                Gizmos.DrawLine(transform.position, TargetPosition);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, Range);
            }
        }
    }
}