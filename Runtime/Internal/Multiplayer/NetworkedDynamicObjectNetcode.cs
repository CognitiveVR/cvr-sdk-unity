using UnityEngine;
using Cognitive3D.Components;


#if C3D_UNITY_NETCODE && COGNITIVE3D_INCLUDE_UNITY_NETCODE
using Unity.Netcode;
using Unity.Netcode.Components;
#endif

namespace Cognitive3D
{
#if C3D_UNITY_NETCODE && COGNITIVE3D_INCLUDE_UNITY_NETCODE
    /// <summary>
    /// Unity Netcode-specific implementation of networked dynamic object tracking.
    /// Monitors Unity Netcode NetworkObject validity and notifies DynamicObject when ready.
    /// </summary>
    public class NetworkedDynamicObjectNetcode : NetworkedDynamicObjectBase
    {
        private NetworkObject networkObject;
        private NetworkTransform networkTransform;

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
        /// Returns the transform holding the NetworkObject this object belongs to.
        /// </summary>
        internal override Transform GetNetworkRootTransform()
        {
            if (networkObject != null)
            {
                return networkObject.transform;
            }

            if (networkTransform != null)
            {
                return networkTransform.NetworkObject != null
                    ? networkTransform.NetworkObject.transform
                    : networkTransform.transform;
            }

            return null;
        }

        /// <summary>
        /// Checks if this NetworkObject is a client's player object (the NetworkManager's
        /// Player Prefab, or an object spawned with SpawnAsPlayerObject).
        /// IsPlayerObject is synced to every client on spawn, so no server round trip is needed.
        /// Objects a player only owns (e.g. a held item) are not player objects.
        /// </summary>
        internal override bool IsPlayerAvatar()
        {
            NetworkObject no = networkObject;
            if (no == null && networkTransform != null)
            {
                no = networkTransform.NetworkObject;
            }

            return no != null && no.IsSpawned && no.IsPlayerObject;
        }
    }
#endif
}
