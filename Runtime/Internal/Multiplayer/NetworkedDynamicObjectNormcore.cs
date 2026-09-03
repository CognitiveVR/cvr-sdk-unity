using UnityEngine;

#if COGNITIVE3D_INCLUDE_NORMCORE
using Normal.Realtime;
#endif

namespace Cognitive3D
{
#if COGNITIVE3D_INCLUDE_NORMCORE
    /// <summary>
    /// Normcore-specific implementation of networked dynamic object tracking.
    /// Monitors Normcore RealtimeView validity and notifies DynamicObject when ready.
    /// </summary>
    public class NetworkedDynamicObjectNormcore : NetworkedDynamicObjectBase
    {
        private RealtimeView realtimeView;

        protected override void OnEnable()
        {
            realtimeView = GetComponent<RealtimeView>();
            base.OnEnable();
        }

        /// <summary>
        /// Checks if the Normcore RealtimeView is connected and has a valid ID.
        /// </summary>
        protected override bool IsNetworkObjectValid()
        {
            // Check if RealtimeView exists, is connected to room, and has a valid view UUID
            return realtimeView != null &&
                   realtimeView.isOwnedLocally != null && // This checks if realtime is connected
                   realtimeView.viewUUID != 0; // ViewUUID is assigned when spawned
        }

        /// <summary>
        /// Logs information about the Normcore RealtimeView including ownership.
        /// </summary>
        protected override void LogNetworkObjectInfo()
        {
            if (realtimeView != null)
            {
                string ownerType = realtimeView.isOwnedLocally ? "LOCAL" : "REMOTE";
                string ownerId = GetOwnerId();
                Debug.Log($"[Normcore] [{ownerType}] RealtimeView UUID: {realtimeView.viewUUID}, Owner: {ownerId}, Name: {gameObject.name}, Total Count: {MultiplayerUtil.networkedObjCounter}");
            }
        }

        /// <summary>
        /// Gets the Client ID that owns this RealtimeView.
        /// In Normcore, ownership is determined by the ownerIDInHierarchy or ownerID.
        /// </summary>
        /// <returns>The owner's client ID as a string, or "None" if unowned</returns>
        public override string GetOwnerId()
        {
            if (realtimeView != null)
            {
                // ownerIDInHierarchy includes ownership from parent objects
                int ownerId = realtimeView.ownerIDInHierarchy;

                if (ownerId >= 0)
                {
                    return ownerId.ToString();
                }
            }
            return "None";
        }

        /// <summary>
        /// Checks if the local client owns this RealtimeView.
        /// </summary>
        internal override bool IsOwnedLocally()
        {
            return realtimeView != null && realtimeView.isOwnedLocally;
        }

        /// <summary>
        /// Checks if this RealtimeView is a player avatar/rig.
        /// For Normcore, checks:
        /// 1. If it has a RealtimeAvatar component (Normcore's built-in avatar system)
        /// 2. Component name patterns (Player, Avatar, etc.)
        /// 3. GameObject name patterns
        /// </summary>
        internal override bool IsPlayerAvatar()
        {
            if (realtimeView == null || realtimeView.viewUUID == 0) return false;

            // Method 1: Check for RealtimeAvatar component (Normcore's avatar system) in hierarchy
            var realtimeAvatar = GetComponentInParent<Normal.Realtime.RealtimeAvatar>();
            if (realtimeAvatar != null)
            {
                return true;
            }

            var realtimeAvatarInChildren = GetComponentInChildren<Normal.Realtime.RealtimeAvatar>();
            if (realtimeAvatarInChildren != null)
            {
                return true;
            }

            // Method 2: Check for common player-related components in hierarchy
            // Check all ancestors (self, parent, grandparent, etc.)
            var allAncestorComponents = GetComponentsInParent<Component>(true);
            foreach (var component in allAncestorComponents)
            {
                string typeName = component.GetType().Name;
                if (typeName.Contains("Player") || typeName.Contains("Character") ||
                    typeName.Contains("Avatar") || typeName.Contains("Rig"))
                {
                    return true;
                }
            }

            // Check all descendants (children, grandchildren, etc.)
            var childComponents = GetComponentsInChildren<Component>(true);
            foreach (var component in childComponents)
            {
                string typeName = component.GetType().Name;
                if (typeName.Contains("Player") || typeName.Contains("Character") ||
                    typeName.Contains("Avatar") || typeName.Contains("Rig"))
                {
                    return true;
                }
            }

            // Method 3: Check GameObject name patterns in hierarchy
            // Check all ancestors (self, parent, grandparent, etc.)
            Transform currentTransform = transform;
            while (currentTransform != null)
            {
                string transformName = currentTransform.name.ToLower();
                if (transformName.Contains("player") || transformName.Contains("avatar") ||
                    transformName.Contains("rig") || transformName.Contains("character"))
                {
                    return true;
                }
                currentTransform = currentTransform.parent;
            }

            return false;
        }

        /// <summary>
        /// Returns the transform holding the RealtimeView this object belongs to.
        /// </summary>
        internal override Transform GetNetworkRootTransform()
        {
            return realtimeView != null ? realtimeView.transform : null;
        }
    }
#endif
}
