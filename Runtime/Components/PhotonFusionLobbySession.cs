using UnityEngine;
#if FUSION2
using Fusion;

namespace Cognitive3D.Components
{
    public class PhotonFusionLobbySession : NetworkBehaviour
    {
        internal static PhotonFusionLobbySession Instance;

        internal NetworkRunner activeRunner { get; set; }

        [HideInInspector]
        [Networked]
        public NetworkString<_64> LobbyId { get; set; }

        public override void Spawned()
        {
            if (activeRunner == null)
            {
                activeRunner = FindAnyObjectByType<NetworkRunner>();

                if (activeRunner == null)
                {
                    Util.logError("No NetworkRunner found to register callbacks. Adding one to Cognitive3D_FusionLobbySync");
                    activeRunner = gameObject.AddComponent<NetworkRunner>();
                }
            }

            Instance = this;
        }
    }
}
#endif
