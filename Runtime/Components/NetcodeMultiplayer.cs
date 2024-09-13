using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

#if COGNITIVE3D_INCLUDE_UNITY_LOBBY_SERVICES
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
#endif

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    public class NetcodeMultiplayer : AnalyticsComponentBase
    {
        private UnityTransport transport;
        private ulong localClientId;

        private string serverAddress;
        private int port;

        private Lobby currentLobby;
        private string lobbyID;

        protected override void OnSessionBegin()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;

                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStarted += OnServerStartedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStopped += OnServerStoppedCallback;
            }
        }

        private void OnPreSessionEnd()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;

                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStopped -= OnServerStoppedCallback;
            }
        }

        protected void OnClientConnectedCallback(ulong clientId)
        {
            SetMultiplayerSessionProperties();
#if COGNITIVE3D_INCLUDE_UNITY_LOBBY_SERVICES
            GetLobbiesInfo();
#endif

            if (Unity.Netcode.NetworkManager.Singleton.IsHost)
            {
                GenerateAndSetLobbyIDForAllClients();
                
                new CustomEvent("c3d.multiplayer.thisHostConnected")
                    .SetProperty("Player ID", clientId)
                    .SetProperty("Number of players in room", Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count)
                    .Send();
            } 
            else if (Unity.Netcode.NetworkManager.Singleton.IsClient)
            {
                new CustomEvent("c3d.multiplayer.thisClientConnected")
                    .SetProperty("Player ID", clientId)
                    .SetProperty("Number of players in room", Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count)
                    .Send();
            }

            SetLobbyAndViewID(lobbyID);
            SendClientConnected(clientId);
        }

        protected void OnClientDisconnectCallback(ulong clientId)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsHost)
            {
                new CustomEvent("c3d.multiplayer.thisHostDisconnected")
                    .SetProperty("Player ID", clientId)
                    .Send();
            }
            else if (Unity.Netcode.NetworkManager.Singleton.IsClient)
            {
                new CustomEvent("c3d.multiplayer.thisClientDisconnected")
                    .SetProperty("Player ID", clientId)
                    .Send();
            }
        }

        protected void OnServerStartedCallback()
        {
            new CustomEvent("c3d.multiplayer.serverStarted")
                    .Send();
        }

        protected void OnServerStoppedCallback(bool isHostShutdown)
        {
            if (isHostShutdown)
            {
                new CustomEvent("c3d.multiplayer.serverStopped")
                    .SetProperty("Reason", "Shutdown by host")
                    .Send();
            }
            else
            {
                new CustomEvent("c3d.multiplayer.serverStopped")
                    .SetProperty("Reason", "Shutdown due to external reasons")
                    .Send();
            }
            
        }

#if COGNITIVE3D_INCLUDE_UNITY_LOBBY_SERVICES
        public async void GetLobbiesInfo()
        {
            try
            {
                QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync();
                List<Lobby> lobbies = response.Results;
                
                foreach (Lobby lobby in lobbies)
                {
                    foreach (var player in lobby.Players)
                    {
                        if (player.Id == localClientId.ToString())
                        {
                            currentLobby = lobby;
                        }
                    }
                }

                if (currentLobby != null)
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonRoomName", currentLobby.Name);
                    Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.maxNumberConnections", currentLobby.MaxPlayers);
                }
            }
            catch (SystemException e)
            {
                Util.LogOnce($"Failed to retrieve list of lobbies: {e.Message}", LogType.Error);
            }
        } 
#endif

        /// <summary>
        /// Sets session properties for multiplayer related details
        /// </summary>
        private void SetMultiplayerSessionProperties()
        {
            localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            transport = Unity.Netcode.NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport)
            {
                port = transport.ConnectionData.Port;
                serverAddress = transport.ConnectionData.Address;
            }
            
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.localClientId", localClientId);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.serverAddress", serverAddress);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.port", port);
        }

        /// <summary>
        /// Generate and assigns a lobby id for all participants
        /// Helpful for identifying multiple individual sessions as part of a multiplayer sessions
        /// For more info, see: https://docs.cognitive3d.com/unity/multiplayer/#lobby-id
        /// </summary>
        private void GenerateAndSetLobbyIDForAllClients()
        {
            lobbyID = System.Guid.NewGuid().ToString();
        }

#region RPC
        /// <summary>
        /// RPC to set lobbyID for all participants
        /// </summary>
        /// <param name="lobbyID">The lobbyID as a string</param>
        [ClientRpc]
        private void SetLobbyAndViewID(string lobbyID)
        {
            Cognitive3D_Manager.SetLobbyId(lobbyID);
        }

        /// <summary>
        /// RPC when a client connects <br/>
        /// For other users: Participant A sends event when participant B connects
        /// </summary>
        [ClientRpc]
        private void SendClientConnected(ulong clientID)
        {
            // Send events only for "other" players
            if (localClientId != clientID)
            {
                new CustomEvent("c3d.multiplayer.aNewClientConnected")
                .SetProperty("Player ID", clientID)
                .SetProperty("Number of players in room", Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count)
                .Send();
            }
        }
#endregion
    }
}
