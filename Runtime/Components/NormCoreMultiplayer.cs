using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Cognitive3D.Components;

#if COGNITIVE3D_INCLUDE_NORMCORE
using Normal.Realtime;

namespace Cognitive3D
{
    public class NormcoreMultiplayer : AnalyticsComponentBase
    {
        RealtimeAvatarManager normcoreAvatarManagerComponent;
        Realtime realtimeComponent;
        NormcoreSync normcoreSync;

        private int clientID;

        private const float NORMCORE_SENSOR_RECORDING_INTERVAL_IN_SECONDS = 1.0f;
        private float currentTime = 0;

        protected override void OnSessionBegin()
        {
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
            
            if (Realtime.instances.Count > 0)
            {
                realtimeComponent = Realtime.instances.First();
                realtimeComponent.didConnectToRoom += OnDidConnectToRoom;
                realtimeComponent.didDisconnectFromRoom += OnDidDisconnectFromRoom;
            }
            else
            {
                Util.logWarning("No Normcore Realtime component found in scene.");
            }

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
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                if (!Cognitive3D_Manager.IsInitialized) { return; }
                currentTime += deltaTime;
                if (currentTime > NORMCORE_SENSOR_RECORDING_INTERVAL_IN_SECONDS)
                {
                    currentTime = 0;
                    SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", realtimeComponent.ping);
                }

                // Check if the realtime component is null, and if so, attempt to retrieve the first active Realtime instance.
                // If a Realtime instance exists, set up event listeners for room connection and disconnection.
                // Also, find the RealtimeAvatarManager and attach event listeners for avatar creation and destruction.
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
            else
            {
                Util.LogOnce("Normcore Multiplayer component is disabled. Please enable in inspector.", LogType.Warning);
            }
        }

        private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;

            if (normcoreAvatarManagerComponent != null)
            {
                normcoreAvatarManagerComponent.avatarCreated -= OnAvatarCreated;
                normcoreAvatarManagerComponent.avatarDestroyed -= OnAvatarDestroyed;
            }

            if (realtimeComponent != null)
            {
                realtimeComponent.didConnectToRoom -= OnDidConnectToRoom;
                realtimeComponent.didDisconnectFromRoom -= OnDidDisconnectFromRoom;
            }
        }

        /// <summary>
        /// Handles actions when a new avatar is created.
        /// </summary>
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
            new CustomEvent("c3d.multiplayer.new_avatar_created")
                .SetProperty("Player ID", newPlayerId)
                .SetProperty("Number of players", avatarManager.avatars.Count)
                .Send();
        }

        /// <summary>
        /// Handles actions when an avatar destroyed.
        /// </summary>
        private void OnAvatarDestroyed(RealtimeAvatarManager avatarManager, RealtimeAvatar avatar, bool isLocalAvatar)
        {
            new CustomEvent("c3d.multiplayer.avatar_destroyed")
                .SetProperty("Number of players", avatarManager.avatars.Count)
                .Send();
        }

        /// <summary>
        /// Handles actions when a client connects to a room: 
        /// sets session properties, sets lobby ID, and sends a custom event with room name and player ID.
        /// </summary>
        private void OnDidConnectToRoom(Realtime realtime)
        {
            SetMultiplayerSessionProperties(realtime);
            clientID = realtime.clientID;

            if (TryInstantiateNormcoreSync(out var normcoreSyncInstance))
            {
                normcoreSync = normcoreSyncInstance.GetComponent<NormcoreSync>();
            }         
            if (normcoreSync == null) return;

            if (!normcoreSync.TryGetLobbyId(out var lobbyId))
            {
                lobbyId = normcoreSync.SetLobbyId();
            }
            
            Cognitive3D_Manager.SetLobbyId(lobbyId);

            new CustomEvent("c3d.multiplayer.connected_to_room")
                .SetProperty("Room Name", realtime.room.name)
                .SetProperty("Player ID", clientID)
                .Send();
        }

        /// <summary>
        /// Handles actions when a client disconnects from a room: 
        /// sends a custom event with room name and player ID.
        /// </summary>
        private void OnDidDisconnectFromRoom(Realtime realtime)
        {
            new CustomEvent("c3d.multiplayer.disconnected_from_room")
                .SetProperty("Room Name", realtime.room.name)
                .SetProperty("Player ID", clientID)
                .Send();
        }

        /// <summary>
        /// Sets session properties for multiplayer related details
        /// </summary>
        private void SetMultiplayerSessionProperties(Realtime realtime)
        {
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.normcoreAppKey", realtime.normcoreAppSettings.normcoreAppKey);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.clientId", realtime.clientID);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.normocoreRoomName", realtime.room.name);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.normcoreMatcherURL", realtime.normcoreAppSettings.matcherURL);
        }

        /// <summary>
        /// Attempts to find or instantiate the NormcoreSync GameObject (NormcoreSync model). 
        /// Returns true if the instance is found or created, otherwise false.
        /// </summary>
        /// <param name="normcoreSyncInstance">Output GameObject for the NormcoreSync instance.</param>
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

#region Inspector Utils
        public override string GetDescription()
        {
#if C3D_NORMCORE
            return "Records Normcore-related data and events, such as room connections, disconnections, and RTT/Ping.";
#else
            return "This component is disabled. Enable the Normcore support from project window.";
#endif
        }

        public override bool GetWarning()
        {
#if C3D_NORMCORE
            return false;
#else
            return true;
#endif
        }
#endregion
    }
}
#endif
