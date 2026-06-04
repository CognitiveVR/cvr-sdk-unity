using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Cognitive3D.Components
{
    /// <summary>
    /// Captures room geometry (anchors, planes, volumes)
    /// </summary>
    internal class RoomCapture : AnalyticsComponentBase
    {
        private static RoomCapture instance;
        public static RoomCapture Instance => instance;

        /// <summary>
        /// True if a room capture provider exists and has been initialized for this session
        /// </summary>
        public bool IsAvailable => isActiveAndEnabled && provider != null;

        IRoomCaptureProvider provider;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded += OnLevelLoaded;

            if (instance == null) instance = this;

#if C3D_OCULUS && COGNITIVE3D_META_MRUK_68_OR_NEWER
            provider = new MetaRoomCaptureProvider();
#elif C3D_VIVEWAVE && C3D_VIVEWAVE_SCENEPERCEPTION
            provider = new ViveWaveRoomCaptureProvider();
#elif COGNITIVE3D_AR_FOUNDATION
            provider = new ARFoundationRoomCaptureProvider();
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
            CleanupSubscriptions();
        }

        void OnDestroy()
        {
            CleanupSubscriptions();
        }

        private void OnPreSessionEnd()
        {
            CleanupSubscriptions();
        }

        private void CleanupSubscriptions()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;
            provider?.Stop();
            provider = null;
            if (instance == this) instance = null;
        }

        /// <summary>
        /// Raycast against the active room capture provider.
        /// Returns false if no provider, no rooms, or no hit
        /// </summary>
        public bool TryGetGazedAnchor(Ray ray, float maxDistance, out string anchorId, out Vector3 worldHit, out Vector3 localHit, out float distance)
        {
            anchorId = null;
            worldHit = Vector3.zero;
            localHit = Vector3.zero;
            distance = 0f;
            if (provider == null) return false;
            return provider.TryGetGazedAnchor(ray, maxDistance, out anchorId, out worldHit, out localHit, out distance);
        }

                public override string GetDescription()
        {
#if (C3D_OCULUS && COGNITIVE3D_META_MRUK_68_OR_NEWER) || (C3D_VIVEWAVE && C3D_VIVEWAVE_SCENEPERCEPTION) || COGNITIVE3D_AR_FOUNDATION
            return "Captures room layout (walls, floors, furniture) and gaze on surfaces via Meta MRUK, Vive Wave, or AR Foundation.";
#else
            return "Requires Meta MRUK, Vive Wave, or AR Foundation. No supported provider detected.";
#endif
        }

        public override bool GetWarning()
        {
#if (C3D_OCULUS && COGNITIVE3D_META_MRUK_68_OR_NEWER) || (C3D_VIVEWAVE && C3D_VIVEWAVE_SCENEPERCEPTION) || COGNITIVE3D_AR_FOUNDATION
            return false;
#else
            return true;
#endif
        }
    }
}
