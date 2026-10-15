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

        internal override void OnEnable()
        {
            realtimeView = GetComponent<RealtimeView>();
            base.OnEnable();
        }

        /// <summary>
        /// Checks if the Normcore RealtimeView is connected and has a valid ID.
        /// </summary>
        internal override bool IsNetworkObjectValid()
        {
            // Ownership properties throw until the view has a model, so only check
            // connection state and the view UUID here (owned locally or remotely is still valid)
            return HasModel() &&
                   string.IsNullOrEmpty(realtimeView.viewUUID) == false; // ViewUUID is assigned when spawned
        }

        /// <summary>
        /// True once the RealtimeView is registered with a connected Realtime instance,
        /// which is when Normcore assigns its model and ownership can be queried safely.
        /// </summary>
        private bool HasModel()
        {
            return realtimeView != null &&
                   realtimeView.realtime != null &&
                   realtimeView.realtime.connected;
        }

        /// <summary>
        /// Gets the RealtimeView UUID that Normcore assigns to this object.
        /// </summary>
        internal override string GetNetworkIdString()
        {
            return realtimeView != null ? realtimeView.viewUUID : null;
        }

        /// <summary>
        /// Gets the Client ID that owns this RealtimeView.
        /// In Normcore, ownership is determined by the ownerIDInHierarchy or ownerID.
        /// </summary>
        /// <returns>The owner's client ID as a string, or "None" if unowned</returns>
        internal override string GetOwnerId()
        {
            if (HasModel())
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
            return HasModel() && realtimeView.isOwnedLocallySelf;
        }

        /// <summary>
        /// Checks if this RealtimeView is a player avatar/rig.
        /// For Normcore, checks:
        /// 1. If it is, or sits under, a RealtimeAvatar (Normcore's built-in avatar system)
        /// 2. GameObject name patterns in the hierarchy
        /// Component type names are not checked because they misclassify props (e.g. "Rig" matches Rigidbody).
        /// </summary>
        internal override bool IsPlayerAvatar()
        {
            if (realtimeView == null || string.IsNullOrEmpty(realtimeView.viewUUID)) return false;

            // Method 1: Check for RealtimeAvatar component (Normcore's avatar system) in hierarchy
            if (GetComponentInParent<RealtimeAvatar>(true) != null)
            {
                return true;
            }

            // Method 2: Check GameObject name patterns in hierarchy
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
