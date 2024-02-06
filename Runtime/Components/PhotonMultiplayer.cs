using Photon.Pun;
using UnityEngine;
using Cognitive3D.Components;
using Photon.Realtime;

namespace Cognitive3D
{
    // Can't inherit multiple classes: https://forum.unity.com/threads/multiple-inheritance-implementation-alternative.367802/
    public class PhotonMultiplayer : MonoBehaviourPunCallbacks
    {
        private int playerPhotonActorNumber;
        private int maxPlayerPhotonActorConnected;
        private int currentPlayerPhotonActorConnected;
        private string photonRoomName;
        private string serverAddress;
        private int port;

        private void Start ()
        {
            // PUN ID and Realtime ID is same: pun is unity specific implementation of realtime
            string photonAppID = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.Photon App ID", photonAppID);
            PhotonNetwork.NetworkStatisticsEnabled = true;
        }

        private void Update()
        {
            // Time from my device to server and back
            // AKA latency
            int roundTripTimeInMilliseconds = PhotonNetwork.GetPing();
            SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", roundTripTimeInMilliseconds);
            
            string punStats = PhotonNetwork.NetworkStatisticsToString();
            var splitStats = punStats.Split('.');

            foreach (var split in splitStats)
            {
                Debug.Log("@@ " + punStats);
                Debug.Log("@@ Split " + split);
            }
        }

        public override void OnCreatedRoom()
        {
            base.OnCreatedRoom();
            SetMultiplayerSessionProperties();
            if (PhotonNetwork.CurrentRoom != null && photonRoomName != null)
            {
                new CustomEvent("Player created room")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .Send();
                GenerateAndSetLobbyIDForAllClients();
            }
        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();
            SetMultiplayerSessionProperties();
            if (PhotonNetwork.CurrentRoom != null && photonRoomName != null)
            {
                new CustomEvent("c3d.multiplayer.Player joined room")
                    .SetProperty("Room name", photonRoomName)
                    .SetProperty("Player ID", playerPhotonActorNumber)
                    .Send();
                this.photonView.RPC("SendCustomEventOnJoin", RpcTarget.All, playerPhotonActorNumber);
                this.photonView.RPC("CalculateNumberConnections", RpcTarget.AllBuffered);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cause"></param>
        public override void OnLeftRoom()
        {
            base.OnLeftRoom();
            new CustomEvent("c3d.multiplayer.Player left room")
                .SetProperty("Room name", photonRoomName)
                .SetProperty("Player ID", playerPhotonActorNumber)
                .Send();
            
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.maxNumberConnection", maxPlayerPhotonActorConnected);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            new CustomEvent("c3d.Other Player left room")
                .SetProperty("Player actor number", otherPlayer.ActorNumber)
                .Send();
        }

        /// <summary>
        /// Session dies before this can get called; or so it seems
        /// </summary>
        /// <param name="cause"></param>
        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            new CustomEvent("Player disconnected")
                .SetProperty("Room name", photonRoomName)
                .SetProperty("Player ID", playerPhotonActorNumber)
                .SetProperty("Disconnect cause", cause)
                .Send();
            this.photonView.RPC("SendCustomEventOnDisconnect", RpcTarget.All, playerPhotonActorNumber, cause);
        }

        private void SetMultiplayerSessionProperties()
        {
            playerPhotonActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonRoomName = PhotonNetwork.CurrentRoom.Name;
            serverAddress = PhotonNetwork.ServerAddress;
            port = PhotonNetwork.PhotonServerSettings.AppSettings.Port;
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.Player Photon Actor Number", playerPhotonActorNumber);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.Photon Room Name", photonRoomName);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.Photon Server Address", serverAddress);
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
        [PunRPC]
        private void SetLobbyAndViewID(string lobbyID)
        {
            Cognitive3D_Manager.SetLobbyId(lobbyID);
            Cognitive3D_Manager.SetSessionProperty("c3d.muliplayer.Photon View ID", this.photonView.ViewID);
        }

        [PunRPC]
        private void CalculateNumberConnections()
        {
            currentPlayerPhotonActorConnected = PhotonNetwork.CurrentRoom.PlayerCount;
            
            if (currentPlayerPhotonActorConnected > maxPlayerPhotonActorConnected)
            {
                maxPlayerPhotonActorConnected = currentPlayerPhotonActorConnected;
            }
        }

        /// <summary>
        /// For other users: Participant A sends event when participant B joins
        /// </summary>
        [PunRPC]
        private void SendCustomEventOnJoin(int actorNumber)
        {
            new CustomEvent("c3d.New player joined room")
                .SetProperty("Player actor number", actorNumber)
                .Send();
        }

        [PunRPC]
        private void SendCustomEventOnLeave(int actorNumber)
        {
            new CustomEvent("c3d.Player left room")
                .SetProperty("Player actor number", actorNumber)
                .Send();
        }

        [PunRPC]
        private void SendCustomEventOnDisconnect(int actorNumber, DisconnectCause cause)
        {
            new CustomEvent("c3d.Player disconnected")
                .SetProperty("Player actor number", actorNumber)
                .SetProperty("Disconnect cause", cause)
                .Send();
        }
#endregion
    }
}