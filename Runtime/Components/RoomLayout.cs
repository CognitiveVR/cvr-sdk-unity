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
        IRoomLayoutProvider provider;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded += OnLevelLoaded;

#if C3D_OCULUS 
            provider = new MetaRoomLayoutProvider(); 
#endif
            provider?.Start();
        }

        void OnLevelLoaded(Scene scene, LoadSceneMode mode, bool newSceneId)
        {
            if (!newSceneId) return;
            provider?.Restart();
        }

        void OnDestroy()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;
        }

        private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            Cognitive3D_Manager.OnLevelLoaded -= OnLevelLoaded;
            provider?.Stop();
        }
    }
}
