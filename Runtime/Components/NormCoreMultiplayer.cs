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
        RealtimeAvatarManager normcoreAvatarManagerComponent;
        Realtime realtimeComponent;
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
            }         
            if (normcoreSync == null) return;

            if (!normcoreSync.TryGetLobbyId(out var lobbyId))
            {
                lobbyId = normcoreSync.SetLobbyId();
            }
            
            Debug.LogError("Received lobby ID is " + lobbyId);
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
                destroyWhenLastClientLeaves = true
            };
            var normcoreSync = FindObjectOfType<NormcoreSync>();

            if (normcoreSync == null)
            {
                normcoreSyncInstance = Realtime.Instantiate("Cognitive3D_NormcoreSync", 
                position: transform.position,
                rotation: transform.rotation,
                options);
            }
            else
            {
                normcoreSyncInstance = normcoreSync.gameObject;
            }
            normcoreSyncInstance.name = "Cognitive3D_NormcoreSync";

            return normcoreSyncInstance != null;
        }
    }
}
