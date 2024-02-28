using UnityEngine;
using System;

/// <summary>
/// This is meant to be an empty class so we can find it using FindObjectOfType<>
/// </summary>

namespace Cognitive3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class RoomTrackingSpace : MonoBehaviour
    {
        public static event Action<Transform> TrackingSpaceChanged;
        private Transform cachedTrackingSpace;

        /// <summary>
        /// Set tracking space when the RoomTrackingSpace is enabled
        /// </summary>
        private void OnEnable()
        {
            cachedTrackingSpace = transform;

            if (!Cognitive3D_Manager.IsInitialized)
            {
                Cognitive3D_Manager.OnSessionBegin += InvokeTrackingSpaceChanged;
                return;
            }
            
            InvokeTrackingSpaceChanged();
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
            TrackingSpaceChanged?.Invoke(cachedTrackingSpace);
        }
    }
}
