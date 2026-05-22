using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Cognitive3D.Components
{
    /// <summary>
    /// Captures room layout (anchors, planes, volumes)
    /// </summary>
    internal class RoomLayout : AnalyticsComponentBase
    {
        private static RoomLayout instance;
        public static RoomLayout Instance => instance;

        /// <summary>
        /// True if a room layout provider exists and has been initialized for this session
        /// </summary>
        public bool IsAvailable => isActiveAndEnabled && provider != null;

        IRoomLayoutProvider provider;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded += OnLevelLoaded;

            instance = this;

#if COGNITIVE3D_META_MRUK_68_OR_NEWER 
            provider = new MetaRoomLayoutProvider(); 
#endif
            provider?.Start();
        }

        void OnLevelLoaded(Scene scene, LoadSceneMode mode, bool newSceneId)
        {
            if (!newSceneId) return;
            provider?.Restart();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;

            instance = null;
        }

        void OnDestroy()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;

            instance = null;
        }

        private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;
            provider?.Stop();
        }

        /// <summary>
        /// Raycast against the active room layout provider.
        /// Returns false if no provider, no rooms, or no hit
        /// </summary>
        public bool TryGetGazedAnchor(Ray ray, float maxDistance, out string anchorId, out Vector3 worldHit, out float distance)
        {
            anchorId = null;
            worldHit = Vector3.zero;
            distance = 0f;
            if (provider == null) return false;
            return provider.TryGetGazedAnchor(ray, maxDistance, out anchorId, out worldHit, out distance);
        }
    }
}
