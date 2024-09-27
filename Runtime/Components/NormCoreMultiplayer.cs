using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;
using System;
using System.Linq;

namespace Cognitive3D
{
    public class NormcoreMultiplayer : MonoBehaviour
    {
        public static string lobbyId;
        RealtimeAvatarManager normcoreAvatarManagerComponent;
        Realtime realtimeComponent;
        public GameObject normcoreSyncPrefab;
        NormcoreSync normcoreSync;

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
            if (realtimeComponent != null)
            {
                realtimeComponent.didConnectToRoom += OnDidConnectToRoom;
                realtimeComponent.didDisconnectFromRoom += OnDidDisconnectFromRoom;
            }
            else
            {
                Util.logWarning("No Normcore Realtime component found in scene.");
            }
        }

        private void Update()
        {
            SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", realtimeComponent.ping);

            if (realtimeComponent == null)
            {
                if (Realtime.instances.Count > 0)
                {
                    realtimeComponent = Realtime.instances.First();
                    realtimeComponent.didConnectToRoom += OnDidConnectToRoom;
                    realtimeComponent.didDisconnectFromRoom += OnDidDisconnectFromRoom;

                    normcoreAvatarManagerComponent = FindObjectOfType<RealtimeAvatarManager>();
                    if (normcoreAvatarManagerComponent != null)
                    {
                        normcoreAvatarManagerComponent.avatarCreated += OnAvatarCreated;
                        normcoreAvatarManagerComponent.avatarDestroyed += OnAvatarDestroyed;
                    }
                }
            }
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

        private void OnDidConnectToRoom(Realtime realtime)
        {
            Debug.LogError("Connected to room!");

            if (TryInstantiateNormcoreSync(out var normcoreSyncInstance))
            {
                normcoreSync = normcoreSyncInstance.GetComponent<NormcoreSync>();
                var normcoreSyncView = normcoreSyncInstance.GetComponent<RealtimeView>();
                Debug.LogError("Do we have realtime? " + normcoreSyncView.realtime);
            }
            
            if (normcoreSync == null) return;

            if (!normcoreSync.TryGetLobbyId(out var lobbyId))
            {
                normcoreSync.SetLobbyId();
            }
            
            Cognitive3D_Manager.SetLobbyId(lobbyId);

            new CustomEvent("c3d.multiplayer.connected_to_room")
                .SetProperty("Room Name", realtime.room.name)
                .SetProperty("Player ID", realtime.clientID)
                .Send();
        }

        private void OnDidDisconnectFromRoom(Realtime realtime)
        {
            Debug.LogError("Disconnected from room!");

            new CustomEvent("c3d.multiplayer.disconnected_from_room")
                .SetProperty("Room Name", realtime.room.name)
                .SetProperty("Player ID", realtime.clientID)
                .Send();
        }

        private bool TryInstantiateNormcoreSync(out GameObject normcoreSyncInstance)
        {
            var options = new Realtime.InstantiateOptions{
                ownedByClient = false,
                preventOwnershipTakeover = false,
                destroyWhenLastClientLeaves = false
            };

            if (FindObjectOfType<NormcoreSync>() == null)
            {
                normcoreSyncInstance = Realtime.Instantiate(normcoreSyncPrefab.name, 
                position: transform.position,
                rotation: transform.rotation,
                options);

                normcoreSyncInstance.name = "Cognitive3D_NormcoreSync";
            }
            else
            {
                normcoreSyncInstance = FindObjectOfType<NormcoreSync>().gameObject;
            }

            return normcoreSyncInstance != null;
        }

#region RPC
        
#endregion
    }
}
