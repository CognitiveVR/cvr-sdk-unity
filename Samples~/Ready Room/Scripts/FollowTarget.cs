using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D.ReadyRoom
{
    public class FollowTarget : MonoBehaviour
    {
        public bool FollowMainCamera;
        public Transform Target;

        public bool Position = true;
        public float PositionLerpSpeed = 0.5f;
        public bool Rotation = true;
        public float RotationLerpSpeed = 0.5f;

        void Start()
        {
            if (FollowMainCamera)
            {
                Target = Camera.main.transform;
            }
        }

        void LateUpdate()
        {
            if (Target == null) { return; }
            if (Position == false && Rotation == false) { return; }

            if (Position == true) { transform.position = Vector3.Lerp(transform.position, Target.position, PositionLerpSpeed); }
            if (Rotation == true) { transform.rotation = Quaternion.Lerp(transform.rotation, Target.rotation, RotationLerpSpeed); }
        }
    }
}