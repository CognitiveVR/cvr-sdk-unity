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
    public class NetcodeMultiplayer : NetworkBehaviour
    {
        private UnityTransport transport;
        private ulong localClientId;
        int connectedClientsCount;

        private string serverAddress;
        private int port;

        private Lobby currentLobby;
        private static string lobbyID = string.Empty;

        protected void Awake()
        {
            Cognitive3D_Manager.OnSessionBegin += OnSessionBegin;
        }

        protected void OnSessionBegin()
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
                Cognitive3D_Manager.OnSessionBegin -= OnSessionBegin;
                Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;

                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStopped -= OnServerStoppedCallback;
            }
        }

        // Callback made on all clients
        protected void OnClientConnectedCallback(ulong clientId)
        {
            SetLobbyIDServerRpc(clientId);
            SetConnectedCountServerRPC();
            SetMultiplayerSessionProperties();
#if COGNITIVE3D_INCLUDE_UNITY_LOBBY_SERVICES
            GetLobbiesInfo();
#endif
            if (Unity.Netcode.NetworkManager.Singleton.LocalClientId == clientId)
            {
                if (IsHost)
                {
                    new CustomEvent("c3d.multiplayer.connected_as_host")
                        .SetProperty("Player ID", clientId)
                        .SetProperty("Number of connected players", connectedClientsCount)
                        .Send();
                } 
                else if (IsClient)
                {
                    new CustomEvent("c3d.multiplayer.connected_as_client")
                        .SetProperty("Player ID", clientId)
                        .SetProperty("Number of connected players", connectedClientsCount)
                        .Send();
                }
            }
            else
            {
                new CustomEvent("c3d.multiplayer.new_client_connected")
                .SetProperty("Player ID", clientId)
                .SetProperty("Number of connected players", connectedClientsCount)
                .Send();
            }
        }

        protected void OnClientDisconnectCallback(ulong clientId)
        {
            if (Unity.Netcode.NetworkManager.Singleton.IsHost)
            {
                new CustomEvent("c3d.multiplayer.host_disconnected")
                    .SetProperty("Player ID", clientId)
                    .Send();
            }
            else if (Unity.Netcode.NetworkManager.Singleton.IsClient)
            {
                new CustomEvent("c3d.multiplayer.client_disconnected")
                    .SetProperty("Player ID", clientId)
                    .Send();
            }
        }

        protected void OnServerStartedCallback()
        {
            new CustomEvent("c3d.multiplayer.server_started")
                    .Send();
        }

        protected void OnServerStoppedCallback(bool isHostShutdown)
        {
            if (isHostShutdown)
            {
                new CustomEvent("c3d.multiplayer.server_stopped")
                    .SetProperty("Reason", "Shutdown by host")
                    .Send();
            }
            else
            {
                new CustomEvent("c3d.multiplayer.server_stopped")
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
                    Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.lobby_name", currentLobby.Name);
                    Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.max_connections", currentLobby.MaxPlayers);
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
            
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.local_client_id", localClientId);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.server_address", serverAddress);
            Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.port", port);
        }

#region RPC
        /// <summary>
        /// Sets the lobby ID on the server and sends to other clients
        /// </summary>
        /// <param name="clientId"></param>
        [ServerRpc (RequireOwnership = false)]
        public void SetLobbyIDServerRpc(ulong clientId)
        {
            if (IsServer && string.IsNullOrEmpty(lobbyID))
            {
                lobbyID = System.Guid.NewGuid().ToString();
            }

            // Send the lobby ID from server to clients
            SendLobbyIDToClientRPC(clientId, lobbyID);
        }

        /// <summary>
        /// Sets the lobby ID for clients
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="lobbyID"></param>
        [ClientRpc]
        private void SendLobbyIDToClientRPC(ulong clientId, string lobbyID)
        {
            if (IsClient && Unity.Netcode.NetworkManager.Singleton.LocalClientId == clientId)
            {
                Cognitive3D_Manager.SetLobbyId(lobbyID);
            }
        }

        /// <summary>
        /// RPC when a client connects <br/>
        /// For other users: Participant A sends event when participant B connects
        /// </summary>
        [ClientRpc]
        private void SetConnectedCountClientRPC(int clientsCount)
        {
            connectedClientsCount = clientsCount;
        }

        [ServerRpc (RequireOwnership = false)]
        public void SetConnectedCountServerRPC()
        {
            if (IsServer)
            {
                connectedClientsCount = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count;
            }

            SetConnectedCountClientRPC(connectedClientsCount);
        }
#endregion
    }
}
