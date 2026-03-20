using UnityEngine;
# if FUSION2
using Fusion;

namespace Cognitive3D.Components
{
    public class PhotonFusionLobbySession : NetworkBehaviour
    {
        internal static PhotonFusionLobbySession Instance;

        internal NetworkRunner activeRunner { get; set; }

        private int currentPlayerPhotonActorConnected;
        internal int maxPlayerPhotonActorConnected;

        [HideInInspector]
        [Networked]
        public NetworkString<_64> LobbyId { get; set; }

        public override void Spawned()
        {
            if (activeRunner == null)
            {
                activeRunner = FindFirstObjectByType<NetworkRunner>();

                if (activeRunner == null)
                {
                    Util.logError("No NetworkRunner found to register callbacks. Adding one to Cognitive3D_FusionLobbySync");
                    activeRunner = gameObject.AddComponent<NetworkRunner>();
                }
            }

            Instance = this;
        }

        #region RPC
        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_SendCustomEventOnJoin(int actorNumber)
        {
            if (activeRunner == null) return;
            if (activeRunner.SessionInfo == null) return;

            // Send events only for "other" players
            if (actorNumber != activeRunner.LocalPlayer.PlayerId)
            {
                new CustomEvent("c3d.multiplayer.aNewPlayerJoinedThisRoom")
                .SetProperty("Player ID", actorNumber)
                .SetProperty("Number of players in room", activeRunner.SessionInfo.PlayerCount)
                .Send();
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_SendCustomEventOnLeave(int actorNumber)
        {
            if (activeRunner == null) return;
            if (activeRunner.SessionInfo == null) return;

            // Send events only for "other" players
            if (actorNumber != activeRunner.LocalPlayer.PlayerId)
            {
                new CustomEvent("c3d.multiplayer.aPlayerLeftThisRoom")
                .SetProperty("Player ID", actorNumber)
                .SetProperty("Number of players in room", activeRunner.SessionInfo.PlayerCount)
                .Send();
            }
        }

        [Rpc(sources: RpcSources.StateAuthority, targets: RpcTargets.All)]
        public void RPC_CalculateNumberConnections()
        {
            if (activeRunner == null) return;
            if (activeRunner.SessionInfo == null) return;

            currentPlayerPhotonActorConnected = activeRunner.SessionInfo.PlayerCount;
            if (currentPlayerPhotonActorConnected > maxPlayerPhotonActorConnected)
            {
                maxPlayerPhotonActorConnected = currentPlayerPhotonActorConnected;
                Cognitive3D_Manager.SetSessionProperty("c3d.multiplayer.maxNumberConnections", maxPlayerPhotonActorConnected);
            }
        }
        #endregion
    }
}
#endif
