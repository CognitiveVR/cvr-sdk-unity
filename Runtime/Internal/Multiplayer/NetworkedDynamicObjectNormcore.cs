using UnityEngine;

#if COGNITIVE3D_INCLUDE_NORMCORE
using System.Collections.Generic;
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

        // Root prefab view -> its model in the room datastore. The model's UUID is the only
        // per-instance id that is identical on every client for prefab views
        // (RealtimeView.viewUUID is always empty for views spawned with Realtime.Instantiate)
        private static readonly Dictionary<RealtimeView, RealtimeViewModel> prefabViewModelIndex = new Dictionary<RealtimeView, RealtimeViewModel>();
        private static Datastore indexedDatastore;
        private static int indexedFrame = -1;

        internal override void OnEnable()
        {
            realtimeView = GetComponent<RealtimeView>();
            base.OnEnable();
        }

        /// <summary>
        /// Checks if the Normcore RealtimeView has a model and a valid network id.
        /// </summary>
        internal override bool IsNetworkObjectValid()
        {
            // Ownership properties throw until the view has a model, so only check
            // model presence and the id here (owned locally or remotely is still valid)
            return HasModel() && !string.IsNullOrEmpty(GetNetworkIdString());
        }

        /// <summary>
        /// True once Normcore has assigned this RealtimeView its model,
        /// after which ownership can be queried safely.
        /// Scene views get their models when the room connects; prefab views once their
        /// root view model is in the room datastore and linked to the view.
        /// </summary>
        private bool HasModel()
        {
            if (realtimeView == null || realtimeView.realtime == null || !realtimeView.realtime.connected) return false;

            RealtimeView rootView = GetRootView();
            if (rootView == null) return false;

            return rootView.isSceneView || GetRootPrefabViewModel(rootView) != null;
        }

        /// <summary>
        /// Finds the datastore model linked to a root prefab view via the shared index.
        /// </summary>
        private static RealtimeViewModel GetRootPrefabViewModel(RealtimeView rootView)
        {
            var room = rootView.realtime != null ? rootView.realtime.room : null;
            if (room == null || room.datastore == null) return null;

            Datastore datastore = room.datastore;

            // A different room invalidates the whole index
            if (datastore != indexedDatastore)
            {
                RebuildPrefabViewModelIndex(datastore);
            }

            RealtimeViewModel model;
            if (TryGetIndexedModel(rootView, out model)) return model;

            // Miss: the view may have been spawned, or linked to its model, since the last scan.
            // Rescan at most once per frame, shared across all instances waiting on a model
            if (indexedFrame != Time.frameCount)
            {
                RebuildPrefabViewModelIndex(datastore);
                if (TryGetIndexedModel(rootView, out model)) return model;
            }
            return null;
        }

        private static bool TryGetIndexedModel(RealtimeView rootView, out RealtimeViewModel model)
        {
            return prefabViewModelIndex.TryGetValue(rootView, out model) && model.realtimeView == rootView;
        }

        /// <summary>
        /// Rebuilds the shared view -> model index from the datastore's prefab view models.
        /// </summary>
        private static void RebuildPrefabViewModelIndex(Datastore datastore)
        {
            var models = datastore.prefabViewModels;
            prefabViewModelIndex.Clear();
            foreach (RealtimeViewModel model in models)
            {
                if (model != null && model.realtimeView != null)
                {
                    prefabViewModelIndex[model.realtimeView] = model;
                }
            }

            indexedDatastore = datastore;
            indexedFrame = Time.frameCount;
        }

        /// <summary>
        /// Gets the UUID of the root RealtimeView this object belongs to. Child views share the root's
        /// id and are disambiguated by GetNetworkRelativeId().
        /// Scene views use their pregenerated viewUUID; prefab views use the UUID of their view model.
        /// </summary>
        internal override string GetNetworkIdString()
        {
            RealtimeView rootView = GetRootView();
            if (rootView == null) return null;

            if (!string.IsNullOrEmpty(rootView.viewUUID)) return rootView.viewUUID;

            RealtimeViewModel model = GetRootPrefabViewModel(rootView);
            return model != null ? model.UUID : null;
        }

        private RealtimeView GetRootView()
        {
            return realtimeView != null ? realtimeView.GetRootView() : null;
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
            if (realtimeView == null || string.IsNullOrEmpty(GetNetworkIdString())) return false;

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
        /// Returns the transform holding the root RealtimeView this object belongs to,
        /// since child views share the root's network id.
        /// </summary>
        internal override Transform GetNetworkRootTransform()
        {
            RealtimeView rootView = GetRootView();
            return rootView != null ? rootView.transform : null;
        }
    }
#endif
}
