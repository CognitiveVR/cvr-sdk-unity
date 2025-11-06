using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public static class MultiplayerUtil
    {
        public static int networkedObjCounter;

        /// <summary>
        /// Event fired when a NetworkObject becomes valid. Passes the GameObject with the valid NetworkObject.
        /// </summary>
        public static event Action<GameObject> OnNetworkObjectValid;

        internal static bool IsNetworkedObject(GameObject obj)
        {
#if FUSION2
            if (obj.GetComponent<Fusion.NetworkObject>() || obj.GetComponent<Fusion.NetworkTransform>())
            {
                return true;
            }
#elif COGNITIVE3D_INCLUDE_UNITY_NETCODE
            if (obj.GetComponent<Unity.Netcode.NetworkObject>() || obj.GetComponent<Unity.Netcode.Components.NetworkTransform>())
            {
                return true;
            }
#elif COGNITIVE3D_INCLUDE_NORMCORE
            if (obj.GetComponent<Normal.Realtime.RealtimeView>())
            {
                return true;
            }
#endif
            return false;
        }

        internal static void AddNetworkedDynamicObject(GameObject obj)
        {
            if (obj == null || obj.GetComponent<NetworkedDynamicObjectBase>()) return;
#if FUSION2
            obj.AddComponent<NetworkedDynamicObjectPhotonFusion>();
#elif COGNITIVE3D_INCLUDE_UNITY_NETCODE
            obj.AddComponent<NetworkedDynamicObjectNetcode>();
#elif COGNITIVE3D_INCLUDE_NORMCORE
            obj.AddComponent<NetworkedDynamicObjectNormcore>();
#endif
        }

        /// <summary>
        /// Checks if a NetworkObject is valid and invokes the OnNetworkObjectValid event if it is.
        /// </summary>
        internal static void NotifyNetworkObjectValid(GameObject obj)
        {
            if (IsNetworkedObject(obj) && !string.IsNullOrEmpty(GetNetworkId(obj)))
            {
                OnNetworkObjectValid?.Invoke(obj);
            }
        }

        internal static bool IsNetworkObjectValid(GameObject obj)
        {
            if (!IsNetworkedObject(obj)) return false;

            var networkedComponent = obj.GetComponent<NetworkedDynamicObjectBase>();
            if (networkedComponent != null)
            {
                return networkedComponent.IsNetworkObjectValid();
            }

            return false;
        }

        /// <summary>
        /// Gets the Network Object ID that server assigns to an object
        /// </summary>
        /// <param name="obj">The GameObject with a network component</param>
        /// <returns>The Network ID as a string, or null if not a networked object</returns>
        internal static string GetNetworkId(GameObject obj)
        {
            if (!IsNetworkedObject(obj)) return null;

            var networkedComponent = obj.GetComponent<NetworkedDynamicObjectBase>();
            if (networkedComponent != null)
            {
                return networkedComponent.GetNetworkIdString();
            }

            return null;
        }

        /// <summary>
        /// Gets the player/client ID that owns or has authority over the networked object.
        /// </summary>
        /// <param name="obj">The GameObject with a network component</param>
        /// <returns>The owner's ID as a string, or null if not a networked object or no owner</returns>
        public static string GetOwnerId(GameObject obj)
        {
            if (!IsNetworkedObject(obj)) return null;

            var networkedComponent = obj.GetComponent<NetworkedDynamicObjectBase>();
            if (networkedComponent != null)
            {
                return networkedComponent.GetOwnerId();
            }

            return null;
        }

        /// <summary>
        /// Checks if the local player owns or has authority over the networked object.
        /// </summary>
        /// <param name="obj">The GameObject with a network component</param>
        /// <returns>True if locally owned, false otherwise</returns>
        public static bool IsOwnedLocally(GameObject obj)
        {
            if (!IsNetworkedObject(obj)) return false;

            var networkedComponent = obj.GetComponent<NetworkedDynamicObjectBase>();
            if (networkedComponent != null)
            {
                return networkedComponent.IsOwnedLocally();
            }

            return false;
        }

        /// <summary>
        /// Checks if the networked object is a player avatar/rig.
        /// </summary>
        /// <param name="obj">The GameObject with a network component</param>
        /// <returns>True if this is a player avatar, false otherwise</returns>
        public static bool IsPlayerAvatar(GameObject obj)
        {
            if (!IsNetworkedObject(obj)) return false;

            var networkedComponent = obj.GetComponent<NetworkedDynamicObjectBase>();
            if (networkedComponent != null)
            {
                return networkedComponent.IsPlayerAvatar();
            }

            return false;
        }
    }
}
