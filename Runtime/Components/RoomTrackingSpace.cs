using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// This is meant to be an empty class so we can find it using FindObjectOfType<>
/// </summary>

namespace Cognitive3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Internal/Room Tracking Space")]
    public class RoomTrackingSpace : MonoBehaviour
    {
        public static event Action<int, Transform> TrackingSpaceChanged;
        private int trackingSpaceIndex;
        private Transform cachedTrackingSpace;

        /// <summary>
        /// Set tracking space when the RoomTrackingSpace is enabled
        /// </summary>
        private void OnEnable()
        {
            if (Cognitive3D_Manager.Instance != null)
            {
                trackingSpaceIndex = Cognitive3D_Manager.Instance.trackingSpaceIndex;
                cachedTrackingSpace = transform;

                if (!Cognitive3D_Manager.IsInitialized)
                {
                    Cognitive3D_Manager.OnSessionBegin += InvokeTrackingSpaceChanged;
                    return;
                }

                InvokeTrackingSpaceChanged();
            }
        }

        /// <summary>
        /// Reset tracking space to null
        /// </summary>
        private void OnDisable()
        {
            cachedTrackingSpace = null;
            InvokeTrackingSpaceChanged();
            Cognitive3D_Manager.OnSessionBegin -= InvokeTrackingSpaceChanged;
        }

        private void InvokeTrackingSpaceChanged()
        {
            TrackingSpaceChanged?.Invoke(trackingSpaceIndex, cachedTrackingSpace);
        }
    }
}
