using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Cognitive3D
{
    /// <summary>
    /// Abstract base class for networked dynamic objects across different multiplayer frameworks.
    /// Handles registration and validation of network IDs for DynamicObject components.
    /// </summary>
#if COGNITIVE3D_INCLUDE_UNITY_NETCODE
    public abstract class NetworkedDynamicObjectBase : Unity.Netcode.NetworkBehaviour
#else
    public abstract class NetworkedDynamicObjectBase : MonoBehaviour
#endif
    {
        protected bool hasRegistered = false;

        internal virtual void OnEnable()
        {
            RegisterNetworkId();
        }

        internal virtual void Update()
        {
            // Keep trying until we successfully register (in case NetworkObject wasn't ready in OnEnable)
            if (!hasRegistered)
            {
                RegisterNetworkId();
            }
        }

        /// <summary>
        /// Attempts to register the network ID if the network object is valid.
        /// Calls NotifyIfValid() to fire the event when successful.
        /// </summary>
        internal void RegisterNetworkId()
        {
            if (hasRegistered) return;

            if (IsNetworkObjectValid())
            {
                hasRegistered = true;
                MultiplayerUtil.networkedObjCounter++;

                // Notify listeners that this NetworkObject is now valid
                MultiplayerUtil.NotifyNetworkObjectValid(gameObject);
            }
        }

        /// <summary>
        /// Framework-specific check to determine if the network object has a valid ID.
        /// Must be implemented by each multiplayer framework.
        /// </summary>
        /// <returns>True if the network object is valid and has an ID</returns>
        internal abstract bool IsNetworkObjectValid();

        /// <summary>
        /// Optional: Get the network ID as a string (for debugging purposes)
        /// </summary>
        /// <returns>The network ID as a string</returns>
        internal virtual string GetNetworkIdString()
        {
            return null;
        }

        /// <summary>
        /// Gets the player/client ID that owns or has authority over this network object.
        /// Returns null if not owned or if ownership concept doesn't apply.
        /// </summary>
        /// <returns>The owner's player/client ID as a string, or null if no owner</returns>
        internal virtual string GetOwnerId()
        {
            return null;
        }

        /// <summary>
        /// Checks if the local player owns or has authority over this network object.
        /// </summary>
        /// <returns>True if locally owned/has authority, false otherwise</returns>
        internal virtual bool IsOwnedLocally()
        {
            return false;
        }

        /// <summary>
        /// Checks if this network object represents a player avatar/rig.
        /// Override in framework-specific implementations to provide accurate detection.
        /// </summary>
        /// <returns>True if this is a player avatar, false otherwise</returns>
        internal virtual bool IsPlayerAvatar()
        {
            return false;
        }

        /// <summary>
        /// The transform holding this object's network identity component
        /// (PhotonView / NetworkObject / RealtimeView). Objects that share a parent's
        /// network id return the parent's transform. Override per framework.
        /// </summary>
        /// <returns>The network root transform, or null if none applies</returns>
        internal virtual Transform GetNetworkRootTransform()
        {
            return null;
        }

        /// <summary>
        /// Builds a deterministic, GUID-formatted id for this object relative to its
        /// network root. Children of a networked object share the root's network id, so
        /// this value (derived from the object's fixed position in the prefab hierarchy)
        /// disambiguates them: identical on every client and the server because the prefab
        /// is the same, but unique per child. Returns an empty string when this object is
        /// the network root itself (nothing to disambiguate).
        /// </summary>
        /// <returns>A GUID-formatted id (e.g. "9c43c9a1-cf41-45e2-bdb8-2d37dea98f38"), or "" if this object holds the network identity</returns>
        internal string GetNetworkRelativeId()
        {
            Transform root = GetNetworkRootTransform();
            if (root == null || transform == root) return string.Empty;

            // Walk from this object up to the network root, recording each sibling index.
            // Sibling indices are deterministic across clients for prefab-authored children.
            var indices = new List<int>();
            Transform current = transform;
            while (current != null && current != root)
            {
                indices.Insert(0, current.GetSiblingIndex());
                current = current.parent;
            }

            // Root was not an ancestor of this object; nothing meaningful to encode.
            if (current == null) return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < indices.Count; i++)
            {
                sb.Append(indices[i]);
                sb.Append('/');
            }

            // Hash the path into a stable 32-char value, then format it like a GUID
            // to match the SDK's custom id format.
            string hash = Hash128.Compute(sb.ToString()).ToString();
            return string.Format("{0}-{1}-{2}-{3}-{4}",
                hash.Substring(0, 8),
                hash.Substring(8, 4),
                hash.Substring(12, 4),
                hash.Substring(16, 4),
                hash.Substring(20, 12));
        }
    }
}
