using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CognitiveVR
{
    public class HMDPointer : MonoBehaviour
    {
        public Transform MarkerTransform;
        public float Distance = 3;
        public float Speed = 0.3f;

        void Update()
        {
            Ray ray = CognitiveVR.GazeHelper.GetCurrentWorldGazeRay();
            UpdateDrawLine(ray);
            Debug.DrawRay(transform.position, ray.direction * 10, Color.red);

            if (MarkerTransform != null)
            {
                var newPosition = ray.GetPoint(Distance);
                MarkerTransform.position = Vector3.Lerp(MarkerTransform.position, newPosition, Speed);
                MarkerTransform.LookAt(CognitiveVR.GameplayReferences.HMD.position);
            }
        }

        IGazeFocus UpdateDrawLine(Ray ray)
        {
            IGazeFocus button = null;

            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 10)) //hit a button
            {
                button = hit.collider.GetComponent<IGazeFocus>();
                if (button != null)
                {
                    button.SetGazeFocus();
                }
            }
            return button;
        }
    }
}