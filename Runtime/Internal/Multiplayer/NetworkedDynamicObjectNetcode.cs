using UnityEngine;
using Cognitive3D.Components;


#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
using Unity.Netcode;
using Unity.Netcode.Components;
#endif

namespace Cognitive3D
{
#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
    /// <summary>
    /// Unity Netcode-specific implementation of networked dynamic object tracking.
    /// Monitors Unity Netcode NetworkObject validity and notifies DynamicObject when ready.
    /// </summary>
    public class NetworkedDynamicObjectNetcode : NetworkedDynamicObjectBase
    {
        private NetworkObject networkObject;
        private NetworkTransform networkTransform;

        public NetworkVariable<bool> IsPlayerAvatarVar = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        internal override void OnEnable()
        {
            networkObject = GetComponent<NetworkObject>();
            networkTransform = GetComponent<NetworkTransform>();
            base.OnEnable();
        }

        /// <summary>
        /// Checks if the Unity Netcode NetworkObject has been spawned and has a valid ID.
        /// </summary>
        internal override bool IsNetworkObjectValid()
        {
            return (networkObject != null && networkObject.IsSpawned) || (networkTransform != null && networkTransform.IsSpawned);
        }

        internal override string GetNetworkIdString()
        {
            if (networkObject != null)
            {
                return networkObject.NetworkObjectId.ToString();
            }

            if (networkTransform != null)
            {
                return networkTransform.NetworkObjectId.ToString();
            }

            return null;
        }

        /// <summary>
        /// Gets the OwnerClientId that owns this NetworkObject.
        /// In Unity Netcode, this is the client ID that has ownership.
        /// </summary>
        internal override string GetOwnerId()
        {
            if (networkObject != null && networkObject.IsSpawned)
            {
                return networkObject.OwnerClientId.ToString();
            }

            if (networkTransform != null && networkTransform.IsSpawned)
            {
                return networkTransform.OwnerClientId.ToString();
            }
            return null;
        }

        /// <summary>
        /// Checks if the local client owns this NetworkObject.
        /// </summary>
        internal override bool IsOwnedLocally()
        {
            return networkObject != null && networkObject.IsOwner;
        }

        /// <summary>
        /// Checks if this NetworkObject is a player avatar/rig.
        /// Requests server verification and returns the cached NetworkVariable value.
        /// </summary>
        internal override bool IsPlayerAvatar()
        {
            NetworkObject no = networkObject;
            if (no == null && networkTransform != null)
            {
                no = networkTransform.NetworkObject;
            }

            if (no != null && no.IsSpawned && no.NetworkManager != null)
            {
                RequestIsPlayerAvatarServerRpc(no.NetworkObjectId);
                return IsPlayerAvatarVar.Value;
            }

            return false;
        }

        /// <summary>
        /// Server RPC that determines if the specified network object is a player avatar
        /// by checking if it matches any connected client's PlayerObject, then syncs the result
        /// to all clients via the IsPlayerAvatarVar NetworkVariable.
        /// </summary>
        /// <param name="objectId">The NetworkObjectId to check</param>
        [ServerRpc(RequireOwnership = false)]
        void RequestIsPlayerAvatarServerRpc(ulong objectId)
        {
            bool result = false;
            if (Unity.Netcode.NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(objectId, out var networkObject))
            {
                foreach (var client in Unity.Netcode.NetworkManager.Singleton.ConnectedClients)
                {
                    if (client.Value.PlayerObject == networkObject)
                    {
                        result = true;
                        break;
                    }
                }
                // Server writes to NetworkVariable, which automatically syncs to all clients
                IsPlayerAvatarVar.Value = result;
            }
        }
    }
#endif
}
