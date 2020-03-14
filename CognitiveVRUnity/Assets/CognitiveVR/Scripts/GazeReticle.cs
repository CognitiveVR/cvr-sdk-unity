using UnityEngine;
using System.Collections;
using CognitiveVR;
#if CVR_AH
using AdhawkApi;
using AdhawkApi.Numerics.Filters;
#endif

//debug helper for gaze tracking with Fove, Pupil, Tobii, Vive Pro Eye, Adhawk, Varjo

namespace CognitiveVR
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