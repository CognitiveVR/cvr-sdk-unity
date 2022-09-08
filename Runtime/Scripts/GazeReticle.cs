using UnityEngine;
using System.Collections;
using Cognitive3D;

//debug helper for gaze tracking
//does not need to have a 'Cognitive3D session' active to work

namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Testing/Gaze Reticle")]
    public class GazeReticle : MonoBehaviour
    {
        public float Speed = 0.3f;
        public float Distance = 3;

        void Update()
        {
            Ray ray = GazeHelper.GetCurrentWorldGazeRay();

            Vector3 newPosition = transform.position;
            newPosition = ray.GetPoint(Distance);

            transform.position = Vector3.Lerp(transform.position, newPosition, Speed);
            transform.LookAt(GameplayReferences.HMD.position);
        }
    }
}