using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    internal interface IRoomLayoutProvider
    {
        void Start();
        void Stop();
        bool TryGetGazedAnchor(out string anchorId, out Vector3 worldHit);
    }
}
