using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

# if FUSION2
using Fusion;
using Fusion.Sockets;
using Fusion.Photon.Realtime;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    public class PhotonFusionMultiplayer : MonoBehaviour, INetworkRunnerCallbacks
    {
        private const float PHOTON_SENSOR_RECORDING_INTERVAL_IN_SECONDS = 1.0f;
        private float currentTime = 0;
        NetworkRunner activeRunner;

        void Start()
        {
            TryGetNetworkRunner();
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
        }

        private void OnDestroy()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
        }

        private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
            {
                if (!Cognitive3D_Manager.IsInitialized) { return; }
                if (!TryGetNetworkRunner()) { return; }

                currentTime += deltaTime;
                if (currentTime > PHOTON_SENSOR_RECORDING_INTERVAL_IN_SECONDS)
                {
                    currentTime = 0;
                    RecordSensorValues();
                }
            }
            else
            {
                Debug.LogWarning("Photon Fusion Multiplayer component is disabled. Please enable in inspector.");
            }
        }

        private bool TryGetNetworkRunner()
        {
            if (activeRunner != null)
                return true;

            foreach (var instance in NetworkRunner.Instances)
            {
                if (instance.IsStarting)
                {
                    activeRunner = instance;

                    if (activeRunner.GetComponent<PhotonFusionNetworkObjectProvider>() == null)
                        activeRunner.gameObject.AddComponent<PhotonFusionNetworkObjectProvider>();

                    activeRunner.AddCallbacks(this);
                    return true;
                }
            }

            Util.LogOnce("No active NetworkRunner instance found.", UnityEngine.LogType.Warning);
            return false;
        }

        /// <summary>
        /// Records sensor values for 
        /// </summary>
        private void RecordSensorValues()
        {
            // Time from my device to server and back
            // AKA latency
            if (activeRunner.IsRunning && (activeRunner.IsConnectedToServer || activeRunner.IsServer))
            {
                double rtt = activeRunner.GetPlayerRtt(activeRunner.LocalPlayer);
                int roundTripTimeInMilliseconds = (int)(rtt * 1000f);
                SensorRecorder.RecordDataPoint("c3d.multiplayer.ping", roundTripTimeInMilliseconds);
            }
        }

        /// <summary>
        /// Sets session properties for multiplayer related details
        /// </summary>
        private void SetMultiplayerSessionProperties(NetworkRunner runner)
        {
            if (runner)
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonPlayerId", runner.LocalPlayer.PlayerId);
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonUserId", runner.UserId);
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonRoomName", runner.SessionInfo.Name);
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonGameMode", runner.GameMode);
            }

            PhotonAppSettings.TryGetGlobal(out var globalSettings);
            if (globalSettings)
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonAppId", globalSettings.AppSettings.AppIdFusion);
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.photonServerAddress", globalSettings.AppSettings.Server);
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.port", globalSettings.AppSettings.Port);
            }  
        }

        /// <summary>
        /// Waits for the shared LobbyId to be available (set by the host).
        /// If the LobbyId isn't set within the timeout window, logs an error and exits early.
        /// </summary>
        private IEnumerator WaitForLobbyIdThenContinue()
        {
            float timeout = 10f;
            while (PhotonFusionLobbySession.Instance == null || string.IsNullOrEmpty(PhotonFusionLobbySession.Instance.LobbyId.ToString()))
            {
                timeout -= Time.deltaTime;
                if (timeout <= 0f)
                {
                    Util.logError("Timeout waiting for lobbyId.");
                    yield break;
                }
                yield return null;
            }

            string lobbyId = PhotonFusionLobbySession.Instance.LobbyId.ToString();
            Cognitive3D_Manager.SetLobbyId(lobbyId);
            Util.logDebug($"Received shared lobbyId: {lobbyId}");
        }

        /// <summary>
        /// Waits for a specified delay (in seconds) before invoking the provided callback action
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        private IEnumerator DelayCoroutine(Action callback, float delay)
        {
            yield return new WaitForSeconds(delay);
            callback?.Invoke();
        }

        #region Network Callbacks
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            SetMultiplayerSessionProperties(runner);
        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason reason)
        {
            if (runner == null) return;
            
            new CustomEvent("c3d.multiplayer.sessionShutdown")
                .SetProperty("Shutdown reason", reason.ToString())
                .Send();
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (runner == null) return;

            new CustomEvent("c3d.multiplayer.disconnectedFromServer")
                .SetProperty("Disconnect reason", reason.ToString())
                .Send();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            bool isAuthoritative = runner.IsServer || runner.IsSharedModeMasterClient;

            if (isAuthoritative && PhotonFusionLobbySession.Instance == null)
            {
                NetworkObject spawned = runner.Spawn(
                    typeId: NetworkPrefabId.FromRaw(PhotonFusionNetworkObjectProvider.C3D_PREFAB_FLAG),
                    position: Vector3.zero,
                    rotation: Quaternion.identity,
                    inputAuthority: null,
                    onBeforeSpawned: null,
                    flags: NetworkSpawnFlags.SharedModeStateAuthMasterClient
                );
                PhotonFusionLobbySession lobbySession = spawned.GetComponent<PhotonFusionLobbySession>();
                lobbySession.activeRunner = runner;
                lobbySession.LobbyId = System.Guid.NewGuid().ToString();
                Util.logDebug($"Host created lobbyId: {lobbySession.LobbyId}");
            }

            runner.StartCoroutine(WaitForLobbyIdThenContinue());

            SetMultiplayerSessionProperties(runner);

            if (runner.SessionInfo != null && !string.IsNullOrEmpty(runner.SessionInfo.Name))
            {
                if (player.PlayerId == runner.LocalPlayer.PlayerId)
                {
                    new CustomEvent("c3d.multiplayer.thisPlayerJoinedARoom")
                        .SetProperty("Session name", runner.SessionInfo.Name)
                        .SetProperty("Player ID", runner.LocalPlayer.PlayerId)
                        .SetProperty("Number of players in room", runner.SessionInfo.PlayerCount)
                        .Send();
                }

                if (PhotonFusionLobbySession.Instance == null) return;
                // Only the StateAuthority is allowed to call this RPC
                if (PhotonFusionLobbySession.Instance.HasStateAuthority)
                {
                    PhotonFusionLobbySession.Instance.RPC_SendCustomEventOnJoin(player.PlayerId);
                    PhotonFusionLobbySession.Instance.RPC_CalculateNumberConnections();
                }
            }
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.SessionInfo != null && !string.IsNullOrEmpty(runner.SessionInfo.Name))
            {
                if (player.PlayerId == runner.LocalPlayer.PlayerId)
                {
                    new CustomEvent("c3d.multiplayer.thisPlayerLeftTheRoom")
                        .SetProperty("Room name", runner.SessionInfo.Name)
                        .SetProperty("Player ID", runner.LocalPlayer.PlayerId)
                        .Send();
                }

                if (PhotonFusionLobbySession.Instance == null) return;
                // Only the StateAuthority is allowed to call this RPC
                if (PhotonFusionLobbySession.Instance.HasStateAuthority)
                {
                    // Added a 1-second delay before sending the leave event 
                    // to ensure the player count is correctly updated after a player leaves the session.
                    StartCoroutine(DelayCoroutine(() => PhotonFusionLobbySession.Instance.RPC_SendCustomEventOnLeave(player.PlayerId), 1));
                }
            }
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject player, PlayerRef playerRef) { }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject player, PlayerRef playerRef)  { }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }

        public void OnSceneLoadStart(NetworkRunner runner) { }
        #endregion
    }
}
#endif
