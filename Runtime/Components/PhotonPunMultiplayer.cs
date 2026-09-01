using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    // Can't inherit multiple classes: https://forum.unity.com/threads/multiple-inheritance-implementation-alternative.367802/
    public class PhotonPunMultiplayer : MonoBehaviourPunCallbacks, IOnEventCallback
    {
        // Photon event codes used instead of PhotonView RPCs, so this component does not
        // require a PhotonView on Cognitive3D_Manager (a scene PhotonView on a persistent
        // manager collides with scene ViewIDs in newly loaded scenes). Codes 0-199 are
        // available for application use; 200-255 are reserved by Photon.
        private const byte EVENT_PLAYER_JOINED = 1;
        private const byte EVENT_CALCULATE_CONNECTIONS = 2;
        private const string LOBBY_ID_ROOM_PROPERTY = "c3d.lobbyId";

        private int playerPhotonActorNumber;
        private int maxPlayerPhotonActorConnected;
        private int currentPlayerPhotonActorConnected;
        private string photonRoomName;
        private string serverAddress;
        private int port;
        private const float PHOTON_SENSOR_RECORDING_INTERVAL_IN_SECONDS = 1.0f;
        private float currentTime = 0;

        private void Start ()
        {
            Cognitive3D_Manager.OnSessionBegin += OnSessionBegin;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
        }

        private void OnSessionBegin()
        {
            // PUN ID and Realtime ID is same: pun is unity specific implementation of realtime
            string photonAppID = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonAppId", photonAppID);
            PhotonNetwork.NetworkStatisticsEnabled = true;
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
                if (currentTime > PHOTON_SENSOR_RECORDING_INTERVAL_IN_SECONDS)
                {
                    currentTime = 0;
                    RecordSensorValues();
                }
            }
            else
            {
                Debug.LogWarning("Photon Multiplayer component is disabled. Please enable in inspector.");
            }
        }

        /// <summary>
        /// Records sensor values for
        /// </summary>
        private void RecordSensorValues()
        {
            // Time from my device to server and back
            // AKA latency
            int roundTripTimeInMilliseconds = PhotonNetwork.GetPing();
            SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", roundTripTimeInMilliseconds);

            // How much the RTT changes - gives an idea of consistency of connection
            int roundTripTimeVariance = PhotonNetwork.NetworkingClient.LoadBalancingPeer.RoundTripTimeVariance;
            SensorRecorder.RecordDataPoint("c3d.multiplayer.rttVariance", roundTripTimeVariance);
        }

        /// <summary>
        /// Called when this player creates a room <br/>
        /// Sends a custom event
        /// </summary>
        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            SetMultiplayerSessionProperties();
            if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(photonRoomName))
            {
                new CustomEvent("c3d.multiplayer.thisPlayerCreatedANewRoom")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .SetProperty("Number of players in room", PhotonNetwork.CurrentRoom.PlayerCount)
                    .Send();
            }
        }

        /// <summary>
        /// Called when this player joins a room <br/>
        /// Sends a custom event, RPC for other players, and calculates the max number of players
        /// </summary>
        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            SetMultiplayerSessionProperties();
            if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(photonRoomName))
            {
                new CustomEvent("c3d.multiplayer.thisPlayerJoinedARoom")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .SetProperty("Number of players in room", PhotonNetwork.CurrentRoom.PlayerCount)
                    .Send();
                // Broadcast to everyone (incl. self)
                PhotonNetwork.RaiseEvent(EVENT_PLAYER_JOINED, playerPhotonActorNumber,
                    new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);

                // Broadcast to everyone and cache for late joiners
                PhotonNetwork.RaiseEvent(EVENT_CALCULATE_CONNECTIONS, null,
                    new RaiseEventOptions { Receivers = ReceiverGroup.All, CachingOption = EventCaching.AddToRoomCache },
                    SendOptions.SendReliable);

                // The master client generates the lobby id once and stores it in room state.
                // Every client (including clients that join later) reads it below / via
                // OnRoomPropertiesUpdate, so all sessions share the same lobby id.
                if (PhotonNetwork.IsMasterClient)
                {
                    EnsureLobbyId();
                }
                ApplyLobbyIdFromRoomProperties();
            }
        }

        /// <summary>
        /// Called after this player leaves the room <br/>
        /// Sends a custom event
        /// </summary>
        public override void OnLeftRoom()
        {
            if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(photonRoomName))
            {
                base.OnLeftRoom();
                new CustomEvent("c3d.multiplayer.thisPlayerLeftTheRoom")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .Send();
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.maxNumberConnections", maxPlayerPhotonActorConnected);
            }
        }

        /// <summary>
        /// Called after a player leaves the room <br/>
        /// Sends a custom event
        /// </summary>
        /// <param name="otherPlayer"></param>
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(photonRoomName))
            {
                base.OnPlayerLeftRoom(otherPlayer);
                new CustomEvent("c3d.multiplayer.aPlayerLeftThisRoom")
                    .SetProperty("Player ID", otherPlayer.ActorNumber)
                    .SetProperty("Number of players in room", PhotonNetwork.CurrentRoom.PlayerCount)
                    .Send();
            }
        }

        /// <summary>
        /// Called after the player disconnects <br/>
        /// Sends a custom event
        /// </summary>
        /// <param name="cause">The cause behind the player disconnecting</param>
        public override void OnDisconnected(DisconnectCause cause)
        {
            if (PhotonNetwork.CurrentRoom != null && !string.IsNullOrEmpty(photonRoomName))
            {
                base.OnDisconnected(cause);
                new CustomEvent("c3d.multiplayer.thisPlayerDisconnected")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .SetProperty("Disconnect cause", cause)
                    .Send();
            }
        }

        /// <summary>
        /// Called when room custom properties change. Applies the shared lobby id as soon
        /// as the master client publishes it (covers clients that were already in the room
        /// before the id was assigned).
        /// </summary>
        /// <param name="propertiesThatChanged">The room properties that changed</param>
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            base.OnRoomPropertiesUpdate(propertiesThatChanged);
            if (propertiesThatChanged != null && propertiesThatChanged.ContainsKey(LOBBY_ID_ROOM_PROPERTY))
            {
                ApplyLobbyIdFromRoomProperties();
            }
        }

        /// <summary>
        /// Sets session properties for multiplayer related details
        /// </summary>
        private void SetMultiplayerSessionProperties()
        {
            playerPhotonActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonRoomName = PhotonNetwork.CurrentRoom.Name;
            serverAddress = PhotonNetwork.ServerAddress;
            port = PhotonNetwork.PhotonServerSettings.AppSettings.Port;
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonPlayerId", playerPhotonActorNumber);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonRoomName", photonRoomName);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonServerAddress", serverAddress);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.port", port);
        }

        /// <summary>
        /// Master-client only: generates the shared lobby id and writes it into room state once,
        /// if it hasn't been set yet. The "already set" check keeps the id stable across master
        /// migration. Helpful for identifying multiple individual sessions as part of one
        /// multiplayer session
        /// </summary>
        private void EnsureLobbyId()
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LOBBY_ID_ROOM_PROPERTY, out object existing)
                && existing is string existingId && !string.IsNullOrEmpty(existingId))
            {
                return; // already assigned (e.g. by a previous master client)
            }

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { LOBBY_ID_ROOM_PROPERTY, System.Guid.NewGuid().ToString() }
            });
        }

        /// <summary>
        /// Reads the shared lobby id from the current room's properties
        /// No-op if the id hasn't been published yet.
        /// </summary>
        private void ApplyLobbyIdFromRoomProperties()
        {
            if (PhotonNetwork.CurrentRoom != null
                && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(LOBBY_ID_ROOM_PROPERTY, out object value)
                && value is string lobbyId && !string.IsNullOrEmpty(lobbyId))
            {
                Cognitive3D_Manager.SetLobbyId(lobbyId);
            }
        }


#region Photon Events
        /// <summary>
        /// Receives the Photon events raised in place of RPCs. Registration is handled by
        /// MonoBehaviourPunCallbacks (AddCallbackTarget on enable, removed on disable), since
        /// this class implements IOnEventCallback.
        /// </summary>
        /// <param name="photonEvent">The received Photon event</param>
        public void OnEvent(EventData photonEvent)
        {
            switch (photonEvent.Code)
            {
                case EVENT_PLAYER_JOINED:
                    SendCustomEventOnJoin((int)photonEvent.CustomData);
                    break;
                case EVENT_CALCULATE_CONNECTIONS:
                    CalculateNumberConnections();
                    break;
            }
        }

        /// <summary>
        /// Calculates the maximum players in the room
        /// </summary>
        private void CalculateNumberConnections()
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                currentPlayerPhotonActorConnected = PhotonNetwork.CurrentRoom.PlayerCount;
                if (currentPlayerPhotonActorConnected > maxPlayerPhotonActorConnected)
                {
                    maxPlayerPhotonActorConnected = currentPlayerPhotonActorConnected;
                }
            }
        }

        /// <summary>
        /// Fired when a player joins a room <br/>
        /// For other users: Participant A sends event when participant B joins
        /// </summary>
        private void SendCustomEventOnJoin(int actorNumber)
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                // Send events only for "other" players
                if (actorNumber != playerPhotonActorNumber)
                {
                    new CustomEvent("c3d.multiplayer.aNewPlayerJoinedThisRoom")
                    .SetProperty("Player ID", actorNumber)
                    .SetProperty("Number of players in room", PhotonNetwork.CurrentRoom.PlayerCount)
                    .Send();

                }
            }
        }
        #endregion

        private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnSessionBegin -= OnSessionBegin;
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
        }
    }
}
#endif
