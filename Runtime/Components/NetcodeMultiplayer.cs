using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

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
        private const float NETCODE_SENSOR_RECORDING_INTERVAL_IN_SECONDS = 1.0f;
        private float currentTime = 0;

        private static string lobbyID = string.Empty;

        protected void Awake()
        {
            Cognitive3D_Manager.OnSessionBegin += OnSessionBegin;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
        }

        protected void OnSessionBegin()
        {
            WaitForNetworkManager();
        }

        private async void WaitForNetworkManager()
        {
            // Wait until the NetworkManager singleton is available
            while (Unity.Netcode.NetworkManager.Singleton == null)
            {
                await Task.Yield(); // Wait for the next frame
            }

            OnNetworkManagerReady();
        }

        private void OnNetworkManagerReady()
        {
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStarted += OnServerStartedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStopped += OnServerStoppedCallback;
            }
        }

        private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnSessionBegin -= OnSessionBegin;
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;

            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStarted -= OnServerStartedCallback;
                Unity.Netcode.NetworkManager.Singleton.OnServerStopped -= OnServerStoppedCallback;
            }
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (transport && Unity.Netcode.NetworkManager.Singleton.IsListening)
            {
                currentTime += deltaTime;
                
                if (currentTime > NETCODE_SENSOR_RECORDING_INTERVAL_IN_SECONDS)
                {
                    currentTime = 0;
                    // Records Round-trip time (RTT) in milliseconds from client to server and receive a response packet back
                    // Returns 0 When the client is also the host because there's no real network communication. 
                    //      All requests are handled internally, so there's no delay to measure.
                    SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", transport.GetCurrentRtt(Unity.Netcode.NetworkManager.ServerClientId));
                }
            }
        }

        /// <summary>
        /// Handles the callback when a client connects the server.
        /// </summary>
        /// <param name="clientId"></param>
        protected void OnClientConnectedCallback(ulong clientId)
        {
            SetLobbyIDServerRpc(clientId);
            SetConnectedCountServerRPC();
            SetMultiplayerSessionProperties();

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
                SendClientConnectedServerRPC(clientId, connectedClientsCount);
            }
        }

        /// <summary>
        /// Handles the callback when a client disconnects from the server.
        /// </summary>
        /// <param name="clientId">The ID of the disconnected client.</param>
        protected void OnClientDisconnectCallback(ulong clientId)
        {
            SetConnectedCountServerRPC();

            if (Unity.Netcode.NetworkManager.ServerClientId == clientId)
            {
                new CustomEvent("c3d.multiplayer.host_disconnected")
                    .SetProperty("Player ID", clientId)
                    .SetProperty("Number of connected players", connectedClientsCount - 1)
                    .Send();
            }
            else
            {
                SendClientDisconnectedServerRPC(clientId, connectedClientsCount - 1);
            }
        }

        /// <summary>
        /// Handles the callback when the server starts, sending a custom event to notify 
        /// that the server has successfully started.
        /// </summary>
        protected void OnServerStartedCallback()
        {
            new CustomEvent("c3d.multiplayer.server_started")
                    .Send();
        }

        /// <summary>
        /// Handles the callback when the server stops, sending a custom event to notify 
        /// about the reason for the shutdown.
        /// </summary>
        /// <param name="isHostShutdown">indicating whether the shutdown was initiated by the host.</param>
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
        /// A client RPC that updates the local count of connected clients.
        /// </summary>
        [ClientRpc]
        private void SetConnectedCountClientRPC(int clientsCount)
        {
            connectedClientsCount = clientsCount;
        }

        /// <summary>
        /// A server RPC that updates the total count of connected clients and broadcasts it to all clients.
        /// After updating the count, it sends this value to all clients via the `SetConnectedCountClientRPC` method.
        /// </summary>
        [ServerRpc (RequireOwnership = false)]
        public void SetConnectedCountServerRPC()
        {
            if (IsServer)
            {
                connectedClientsCount = Unity.Netcode.NetworkManager.Singleton.ConnectedClientsList.Count;
            }

            SetConnectedCountClientRPC(connectedClientsCount);
        }

        /// <summary>
        /// Sends a custom event indicating a new client has connected to the server
        /// This is triggered for all clients except the local client
        /// </summary>
        /// <param name="clientId">The ID of the newly connected client</param>
        /// <param name="clientsCount">The total number of clients connected to the server</param>
        [ClientRpc]
        private void SendClientConnectedClientRPC(ulong clientId, int clientsCount)
        {
            if (Unity.Netcode.NetworkManager.Singleton.LocalClientId != clientId)
            {
                new CustomEvent("c3d.multiplayer.new_client_connected")
                .SetProperty("Player ID", clientId)
                .SetProperty("Number of connected players", clientsCount)
                .Send();
            }
        }

        /// <summary>
        /// A server RPC that triggers a client-side event when a new client connects.
        /// This method is called on the server and executes the `SendClientConnectedClientRPC` 
        /// method to send an event containing the connected client's ID and the total number of 
        /// connected clients.
        /// </summary>
        /// <param name="clientId">The ID of the newly connected client.</param>
        /// <param name="clientsCount">The total number of clients connected to the server.</param>
        [ServerRpc (RequireOwnership = false)]
        public void SendClientConnectedServerRPC(ulong clientId, int clientsCount)
        {
            if (IsServer)
            {
                SendClientConnectedClientRPC(clientId, clientsCount);
            }
        }

        /// <summary>
        /// Sends a custom event when a client disconnects from the server.
        /// This method is called on the clients to notify about a disconnected client, 
        /// except for the local client itself.
        /// </summary>
        /// <param name="clientId">The ID of the disconnected client.</param>
        /// <param name="clientsCount">The current total number of connected clients after the disconnection.</param>
        [ClientRpc]
        private void SendClientDisconnectedClientRPC(ulong clientId, int clientsCount)
        {
            if (Unity.Netcode.NetworkManager.Singleton.LocalClientId != clientId)
            {
                new CustomEvent("c3d.multiplayer.client_disconnected")
                .SetProperty("Player ID", clientId)
                .SetProperty("Number of connected players", clientsCount)
                .Send();
            }
        }

        /// <summary>
        /// A server RPC that triggers a client-side event when a client disconnects from the server.
        /// This method is called on the server and executes the `SendClientDisconnectedClientRPC` method 
        /// to notify clients about the disconnection.
        /// </summary>
        /// <param name="clientId">The ID of the disconnected client.</param>
        /// <param name="clientsCount">The current total number of connected clients after the disconnection.</param>
        [ServerRpc (RequireOwnership = false)]
        public void SendClientDisconnectedServerRPC(ulong clientId, int clientsCount)
        {
            if (IsServer)
            {
                SendClientDisconnectedClientRPC(clientId, clientsCount);
            }
        }
#endregion
    }
}
#endif
