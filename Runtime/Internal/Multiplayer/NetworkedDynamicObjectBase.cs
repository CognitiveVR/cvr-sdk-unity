using UnityEngine;

namespace Cognitive3D
{
    /// <summary>
    /// Abstract base class for networked dynamic objects across different multiplayer frameworks.
    /// Handles registration and validation of network IDs for DynamicObject components.
    /// </summary>
    public abstract class NetworkedDynamicObjectBase : MonoBehaviour
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
    }
}
