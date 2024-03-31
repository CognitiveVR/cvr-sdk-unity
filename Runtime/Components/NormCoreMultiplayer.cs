using UnityEngine;
using Normal.Realtime;

namespace Cognitive3D
{
    public class NormCoreMultiplayer : MonoBehaviour
    {
        RealtimeAvatarManager normcoreAvatarManagerComponent;
        Realtime realtimeComponent;

        // Start is called before the first frame update
        void Start()
        {
            normcoreAvatarManagerComponent = FindObjectOfType<RealtimeAvatarManager>();
            if (normcoreAvatarManagerComponent != null)
            {
                normcoreAvatarManagerComponent.avatarCreated += OnAvatarCreated;
                normcoreAvatarManagerComponent.avatarDestroyed += OnAvatarDestroyed;
            }
            else
            {
                Util.logWarning("No Normcore RealtimeAvatarManager component found in scene.");
            }
            realtimeComponent = FindObjectOfType<Realtime>();
            if (realtimeComponent == null)
            {
                Util.logWarning("No Normcore Realtime component found in scene.");
            }
        }

        private void Update()
        {
            SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", realtimeComponent.ping);
        }

        private void OnAvatarCreated(RealtimeAvatarManager avatarManager, RealtimeAvatar avatar, bool isLocalAvatar)
        {
            int newPlayerId = -1;
            foreach (var dict in avatarManager.avatars)
            {
                if (dict.Value == avatar)
                {
                    newPlayerId = dict.Key;
                }
            }
            new CustomEvent("c3d.multiplayer.A new avatar created")
                .SetProperty("Player ID", newPlayerId)
                .SetProperty("Number of players", avatarManager.avatars.Count)
                .Send();
        }

        private void OnAvatarDestroyed(RealtimeAvatarManager avatarManager, RealtimeAvatar avatar, bool isLocalAvatar)
        {
            new CustomEvent("c3d.multiplayer.An avatar was destroyed")
                .SetProperty("Number of players", avatarManager.avatars.Count)
                .Send();
            
        }
    }
}