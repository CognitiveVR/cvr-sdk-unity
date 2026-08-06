using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    internal interface IRoomCaptureProvider
    {
        void Start();
        void Stop();
        void Restart(); 
        bool TryGetGazedAnchor(Ray ray, float maxDistance, out string anchorId, out Vector3 worldHit, out Vector3 localHit, out float distance);
    }
}
