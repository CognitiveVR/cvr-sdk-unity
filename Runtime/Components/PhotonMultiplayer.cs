using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    // Can't inherit multiple classes: https://forum.unity.com/threads/multiple-inheritance-implementation-alternative.367802/
    public class PhotonMultiplayer : MonoBehaviourPunCallbacks
    {
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
                GenerateAndSetLobbyIDForAllClients();
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
                this.photonView.RPC("SendCustomEventOnJoin", RpcTarget.All, playerPhotonActorNumber);
                this.photonView.RPC("CalculateNumberConnections", RpcTarget.AllBuffered);
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
        /// Generate and assigns a lobby id for all participants
        /// Helpful for identifying multiple individual sessions as part of a multiplayer sessions
        /// For more info, see: https://docs.cognitive3d.com/unity/multiplayer/#lobby-id
        /// </summary>
        private void GenerateAndSetLobbyIDForAllClients()
        {
            string lobbyID = System.Guid.NewGuid().ToString();
            this.photonView.RPC("SetLobbyAndViewID", RpcTarget.AllBuffered, lobbyID);
        }


#region RPC
        /// <summary>
        /// RPC to set lobbyID for all participants
        /// </summary>
        /// <param name="lobbyID">The lobbyID as a string</param>
        [PunRPC]
        private void SetLobbyAndViewID(string lobbyID)
        {
            Cognitive3D_Manager.SetLobbyId(lobbyID);
        }

        /// <summary>
        /// Calculates the maximum players in the room
        /// </summary>
        [PunRPC]
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
        /// RPC when a player joins a room <br/>
        /// For other users: Participant A sends event when participant B joins
        /// </summary>
        [PunRPC]
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
