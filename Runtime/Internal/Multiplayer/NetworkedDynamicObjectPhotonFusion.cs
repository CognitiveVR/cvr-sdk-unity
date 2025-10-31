using UnityEngine;

#if FUSION2
using Fusion;
#endif

namespace Cognitive3D
{
#if FUSION2
    /// <summary>
    /// Fusion2-specific implementation of networked dynamic object tracking.
    /// Monitors Fusion NetworkObject validity and notifies DynamicObject when ready.
    /// </summary>
    public class NetworkedDynamicObjectPhotonFusion : NetworkedDynamicObjectBase
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
        /// Checks if the Fusion NetworkObject has been spawned and has a valid ID.
        /// </summary>
        internal override bool IsNetworkObjectValid()
        {
            return (networkTransform.Object != null && networkTransform.Object.IsValid) ||
            (networkTransform.Data.Parent != null && networkTransform.Data.Parent.IsValid) ||
            (networkObject != null && networkObject.IsValid);
        }

        internal override string GetNetworkIdString()
        {
            if (networkObject != null && networkObject.IsValid)
            {
                return networkObject.Id.Raw.ToString();
            }

            if (networkTransform != null)
            {
                if (networkTransform.Object != null && networkTransform.Object.IsValid)
                {
                    return networkTransform.Object.Id.Raw.ToString();
                }
                
                if (networkTransform.Data.Parent != null &&  networkTransform.Data.Parent.IsValid)
                {
                    return networkTransform.Data.Parent.Object.Raw.ToString();
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the PlayerRef (Player ID) that has authority over this NetworkObject.
        /// Checks InputAuthority first (client control), then StateAuthority (shared topology).
        /// Falls back to NetworkTransform if NetworkObject is unavailable.
        /// </summary>
        /// <returns>The PlayerRef as a string, or null if no valid authority</returns>
        internal override string GetOwnerId()
        {
            // Try NetworkObject first
            if (networkObject != null && networkObject.IsValid)
            {
                string ownerId = GetOwnerIdFromPlayerRefs(networkObject.InputAuthority, networkObject.StateAuthority);
                if (ownerId != null)
                {
                    return ownerId;
                }
            }

            // Fall back to NetworkTransform
            if (networkTransform != null)
            {
                // Check networkTransform.Object
                if (networkTransform.Object != null && networkTransform.Object.IsValid)
                {
                    string ownerId = GetOwnerIdFromPlayerRefs(networkTransform.Object.InputAuthority, networkTransform.Object.StateAuthority);
                    if (ownerId != null)
                    {
                        return ownerId;
                    }
                }

                // Check parent object
                if (networkTransform.Data.Parent != null && networkTransform.Data.Parent.IsValid && networkTransform.Runner != null)
                {
                    NetworkObject parentNetworkObject = networkTransform.Runner.FindObject(networkTransform.Data.Parent.Object);
                    if (parentNetworkObject != null && parentNetworkObject.IsValid)
                    {
                        string ownerId = GetOwnerIdFromPlayerRefs(parentNetworkObject.InputAuthority, parentNetworkObject.StateAuthority);
                        if (ownerId != null)
                        {
                            return ownerId;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Helper method to extract owner ID from PlayerRef authorities.
        /// Prioritizes InputAuthority, falls back to StateAuthority.
        /// </summary>
        /// <returns>PlayerId as string, or null if no valid authority</returns>
        private string GetOwnerIdFromPlayerRefs(PlayerRef inputAuthority, PlayerRef stateAuthority)
        {
            if (inputAuthority.IsRealPlayer)
            {
                return inputAuthority.PlayerId.ToString();
            }

            if (stateAuthority.IsRealPlayer)
            {
                return stateAuthority.PlayerId.ToString();
            }

            return null;
        }

        /// <summary>
        /// Checks if the local player has input authority over this NetworkObject.
        /// </summary>
        internal override bool IsOwnedLocally()
        {
            return networkObject != null && networkObject.HasInputAuthority;
        }

        /// <summary>
        /// Checks if this NetworkObject is a player avatar/rig.
        /// Multiple methods are used to detect player objects:
        /// 1. Check if it's registered as a PlayerObject in the NetworkRunner
        /// 2. Check for NetworkRig component (Fusion XR Shared addon) in hierarchy
        /// 3. Check if the GameObject or parent/children has typical player/avatar components
        /// </summary>
        /// <returns>True if this is a player avatar</returns>
        internal override bool IsPlayerAvatar()
        {
            // Check if this NetworkObject (or parent/child) is registered as a PlayerObject
            // This works if Runner.SetPlayerObject() was called (like in NetworkRig with useNetworkRigAsPlayerObject = true)
            if (networkObject != null && networkObject.IsValid && networkObject.Runner != null)
            {
                var obj = networkObject.Runner.GetPlayerObject(networkObject.Runner.LocalPlayer);
                if (obj != null && networkObject.Id.Raw == obj.Id.Raw)
                {
                    return true;
                }

                var parentObj = GetHighestParent(gameObject);
                if (obj != null && parentObj != null)
                {
                    if (parentObj.GetComponent<NetworkObject>() && parentObj.GetComponent<NetworkObject>().Id.Raw == obj.Id.Raw)
                    {
                        return true;
                    }
                }              
                
                // Check this NetworkObject
                var inputAuthority = networkObject.InputAuthority;
                if (inputAuthority != PlayerRef.None)
                {
                    var playerObject = networkObject.Runner.GetPlayerObject(inputAuthority);
                    if (playerObject == networkObject)
                    {
                        return true;
                    }
                }

                // Also check StateAuthority (for shared topology)
                var stateAuthority = networkObject.StateAuthority;
                if (stateAuthority != PlayerRef.None)
                {
                    var playerObject = networkObject.Runner.GetPlayerObject(stateAuthority);
                    if (playerObject == networkObject)
                    {
                        return true;
                    }
                }

                // Check if any parent NetworkObject is registered as a PlayerObject
                var parentNetworkObject = GetComponentInParent<NetworkObject>();
                if (parentNetworkObject != null && parentNetworkObject != networkObject)
                {
                    var parentInputAuthority = parentNetworkObject.InputAuthority;
                    if (parentInputAuthority != PlayerRef.None)
                    {
                        var parentPlayerObject = networkObject.Runner.GetPlayerObject(parentInputAuthority);
                        if (parentPlayerObject == parentNetworkObject)
                        {
                            return true;
                        }
                    }

                    var parentStateAuthority = parentNetworkObject.StateAuthority;
                    if (parentStateAuthority != PlayerRef.None)
                    {
                        var parentPlayerObject = networkObject.Runner.GetPlayerObject(parentStateAuthority);
                        if (parentPlayerObject == parentNetworkObject)
                        {
                            return true;
                        }
                    }
                }

                // Check if any child NetworkObject is registered as a PlayerObject
                var childNetworkObjects = GetComponentsInChildren<NetworkObject>(true);
                foreach (var childNetworkObject in childNetworkObjects)
                {
                    if (childNetworkObject == networkObject) continue; // Skip self

                    var childInputAuthority = childNetworkObject.InputAuthority;
                    if (childInputAuthority != PlayerRef.None)
                    {
                        var childPlayerObject = networkObject.Runner.GetPlayerObject(childInputAuthority);
                        if (childPlayerObject == childNetworkObject)
                        {
                            return true;
                        }
                    }

                    var childStateAuthority = childNetworkObject.StateAuthority;
                    if (childStateAuthority != PlayerRef.None)
                    {
                        var childPlayerObject = networkObject.Runner.GetPlayerObject(childStateAuthority);
                        if (childPlayerObject == childNetworkObject)
                        {
                            return true;
                        }
                    }
                }
            }

            // No networkObject on the game object
            if (networkTransform != null && networkTransform.Runner != null)
            {
                var obj = networkTransform.Runner.GetPlayerObject(networkTransform.Runner.LocalPlayer);
                if (obj != null)
                {
                    // Iterate through all active players
                    foreach (PlayerRef playerRef in networkTransform.Runner.ActivePlayers)
                    {
                        obj = networkTransform.Runner.GetPlayerObject(playerRef);

                        // Check if this object belongs to this player
                        if (networkTransform.Object != null && networkTransform.Object.StateAuthority == playerRef && networkTransform.Object.Id.Raw == obj.Id.Raw)
                        {
                            return true;
                        }
                        
                        // Check if the parent belongs to this player
                        if (networkTransform.Data.Parent != null && networkTransform.Data.Parent.IsValid)
                        {
                            // Access the parent's NetworkObject to check StateAuthority
                            NetworkObject parentNetworkObject = networkTransform.Runner.FindObject(networkTransform.Data.Parent.Object);
                            
                            if (parentNetworkObject != null && parentNetworkObject.StateAuthority == playerRef && parentNetworkObject.Id.Raw == obj.Id.Raw)
                            {
                                return true;
                            }
                        }
}
                }
            }

            return false;
        }

        public static GameObject GetHighestParent(GameObject obj)
        {
            if (obj == null)
                return null;

            Transform current = obj.transform;
            while (current.parent != null)
            {
                current = current.parent;
            }
            return current.gameObject;
        }
    }
#endif
}
