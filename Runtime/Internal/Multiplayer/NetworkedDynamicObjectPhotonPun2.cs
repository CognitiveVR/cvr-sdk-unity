using UnityEngine;

#if C3D_PHOTON && PUN_2_0_OR_NEWER
using Photon.Pun;
using Photon.Realtime;
#endif

namespace Cognitive3D
{
#if C3D_PHOTON && PUN_2_0_OR_NEWER
    /// <summary>
    /// PUN2-specific implementation of networked dynamic object tracking.
    /// Monitors Photon PhotonView validity and notifies DynamicObject when ready.
    /// </summary>
    public class NetworkedDynamicObjectPhotonPun2 : NetworkedDynamicObjectBase
    {
        PhotonView photonView;
        PhotonTransformView photonTransformView;

        internal override void OnEnable()
        {
            photonView = GetComponent<PhotonView>();
            photonTransformView = GetComponent<PhotonTransformView>();
            base.OnEnable();
        }

        /// <summary>
        /// Checks if a PhotonView (directly or via PhotonTransformView) is present.
        /// </summary>
        internal override bool IsNetworkObjectValid()
        {
            return photonView != null || photonTransformView !=null;
        }

        /// <summary>
        /// Gets the PhotonView ID (ViewID) that Photon assigns to this object.
        /// </summary>
        internal override string GetNetworkIdString()
        {
            if (photonView != null)
            {
                return photonView.ViewID.ToString();
            }

            if (photonTransformView != null)
            {
                return photonTransformView.photonView.ViewID.ToString();
            }
            return null;
        }

        /// <summary>
        /// Gets the UserId of the Photon player that owns this PhotonView.
        /// Falls back to the PhotonTransformView's PhotonView if needed.
        /// </summary>
        /// <returns>The owner's UserId as a string, or null if unowned</returns>
        internal override string GetOwnerId()
        {
            if (photonView != null && photonView.Owner != null)
            {
                return photonView.Owner.ActorNumber.ToString();
            }

            if (photonTransformView != null && photonTransformView.photonView != null
                && photonTransformView.photonView.Owner != null)
            {
                return photonTransformView.photonView.Owner.ActorNumber.ToString();
            }
            return null;
        }

        /// <summary>
        /// Checks if the local player owns this PhotonView (IsMine).
        /// </summary>
        internal override bool IsOwnedLocally()
        {
            return (photonView != null && photonView.IsMine) ||
            (photonTransformView != null && photonTransformView.photonView != null && photonTransformView.photonView.IsMine);
        }

        /// <summary>
        /// Checks if this PhotonView is a player avatar/rig. PUN2 has no built-in
        /// player-object registry, so detection uses two methods:
        /// 1. Match against a player's Player.TagObject (the app's avatar reference)
        /// 2. Fall back to typical player/avatar components in the hierarchy
        /// </summary>
        /// <returns>True if this is a player avatar</returns>
        internal override bool IsPlayerAvatar()
        {
            PhotonView view = photonView;
            if (view == null && photonTransformView != null)
            {
                view = photonTransformView.photonView;
            }

            if (view == null) return false;

            // Room/scene objects are never player avatars
            if (view.IsRoomView) return false;

            // Method 1: authoritative, did the app tag a player's avatar via TagObject?
            // (equivalent to Fusion's SetPlayerObject / GetPlayerObject)
            foreach (var player in PhotonNetwork.PlayerList)
            {
                var tagged = player.TagObject as GameObject;
                if (tagged == null) continue;

                // Match this object or any ancestor (avatar root may hold the tag,
                // while the DynamicObject sits on a hand/head child)
                if (tagged == gameObject || transform.IsChildOf(tagged.transform))
                {
                    return true;
                }
            }

            // Method 2: heuristic fallback for projects that don't set TagObject
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

            return false;
        }

        /// <summary>
        /// Returns the transform holding the PhotonView this object belongs to.
        /// </summary>
        internal override Transform GetNetworkRootTransform()
        {
            if (photonView != null)
            {
                return photonView.transform;
            }

            if (photonTransformView != null && photonTransformView.photonView != null)
            {
                return photonTransformView.photonView.transform;
            }

            return null;
        }
    }
#endif
}
