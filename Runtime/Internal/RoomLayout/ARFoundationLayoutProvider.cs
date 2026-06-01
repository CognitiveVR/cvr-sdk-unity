using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

#if COGNITIVE3D_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

namespace Cognitive3D
{
#if COGNITIVE3D_AR_FOUNDATION
    public class ARFoundationLayoutProvider : IRoomLayoutProvider
    {
        const string SYNTHETIC_ROOM_ID = "arfoundation-room";

        ARRaycastManager raycastManager;
        ARBoundingBoxManager boundingBoxManager;
        ARPlaneManager planeManager;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        public virtual void Start()
        {
            planeManager = UnityEngine.Object.FindAnyObjectByType<ARPlaneManager>();
            boundingBoxManager = UnityEngine.Object.FindAnyObjectByType<ARBoundingBoxManager>();
            raycastManager = UnityEngine.Object.FindAnyObjectByType<ARRaycastManager>();

            if (planeManager == null && boundingBoxManager == null)
            {
                Util.logWarning("ARFoundationLayoutProvider: no ARPlaneManager or " +
                                "ARBoundingBoxManager found. Room layout will not be captured.");
                return;
            }

            // Room exists for this session
            CoreInterface.RecordRoomManifest(new RoomManifestEntry {
                id = SYNTHETIC_ROOM_ID, label = "Unnamed",
                anchors = new List<AnchorManifestEntry>()
            });
            CoreInterface.RecordRoomData(RoomLayoutUtil.BuildRoomToggle(SYNTHETIC_ROOM_ID, true));

            if (boundingBoxManager != null)
            {
                foreach (var b in boundingBoxManager.trackables)
                {
                    EmitAdded(BoxManifest(b), b.transform, BoxScale(b));
                }
                boundingBoxManager.trackablesChanged.AddListener(OnBoundingBoxChanged);
            }

            if (planeManager != null)
            {
                foreach (var p in planeManager.trackables)
                {
                    EmitAdded(PlaneManifest(p), p.transform, PlaneScale(p));
                }
                planeManager.trackablesChanged.AddListener(OnPlanesChanged);
            }
        }

        public virtual void Stop()
        {
            if (boundingBoxManager != null)
            {
                boundingBoxManager.trackablesChanged.RemoveListener(OnBoundingBoxChanged);
            }

            if (planeManager != null)
            {
                planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
            }

            raycastManager = null;
            boundingBoxManager = null;
            planeManager = null;
        }

        public void Restart() 
        { 
            Stop(); 
            Start(); 
        }

        public virtual bool TryGetGazedAnchor(Ray ray, float maxDistance, out string anchorId, out Vector3 worldHit, out Vector3 localHit, out float distance)
        {            
            anchorId = null;
            worldHit = Vector3.zero;
            localHit = Vector3.zero;
            distance = 0f;

            if (raycastManager == null) return false;

            hits.Clear();
            if (!raycastManager.Raycast(ray, hits, TrackableType.PlaneWithinBounds | TrackableType.BoundingBox)) return false;

            var hit = hits[0];
            if (hit.distance > maxDistance) return false;

            anchorId = hit.trackableId.ToString();
            worldHit = hit.pose.position;
            distance = hit.distance;

            if (hit.hitType == TrackableType.PlaneWithinBounds && planeManager != null)
            {
                localHit = planeManager.GetPlane(hit.trackableId).transform.InverseTransformPoint(worldHit);
            }
            else if (hit.hitType == TrackableType.BoundingBox && boundingBoxManager != null
                && boundingBoxManager.TryGetBoundingBox(hit.trackableId, out var boundingBox))
            {
                localHit = boundingBox.transform.InverseTransformPoint(worldHit);
            }

            return true;
        }

        public void OnBoundingBoxChanged(ARTrackablesChangedEventArgs<ARBoundingBox> changes)
        {
            foreach (var b in changes.added)
            {
                EmitAdded(BoxManifest(b), b.transform, BoxScale(b));
            }

            foreach (var b in changes.updated)
            {
                CoreInterface.RecordRoomData(RoomLayoutUtil.BuildAnchorData(b.trackableId.ToString(), b.transform.position, b.transform.rotation, BoxScale(b), enabled: true, isPlane: false));
            }

            foreach (var kvp in changes.removed)
            {
                CoreInterface.RecordRoomData(RoomLayoutUtil.BuildRemoval(kvp.Key.ToString()));
            }
        }

        public void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> changes)
        {
            foreach (var p in changes.added)
            {
                EmitAdded(PlaneManifest(p), p.transform, PlaneScale(p));
            }

            foreach (var p in changes.updated)
            {
                CoreInterface.RecordRoomData(RoomLayoutUtil.BuildAnchorData(p.trackableId.ToString(), p.transform.position, p.transform.rotation, PlaneScale(p), enabled: true, isPlane: true));
            }

            foreach (var kvp in changes.removed)
            {
                CoreInterface.RecordRoomData(RoomLayoutUtil.BuildRemoval(kvp.Key.ToString()));
            }
        }

        void EmitAdded(AnchorManifestEntry m, Transform t, Vector3 scale)
        {
            // additive partial manifest. One anchor under the synthetic room
            CoreInterface.RecordRoomManifest(new RoomManifestEntry {
                id = SYNTHETIC_ROOM_ID,
                anchors = new List<AnchorManifestEntry> { m }
            });
            CoreInterface.RecordRoomData(RoomLayoutUtil.BuildAnchorData(m.id, t.position, t.rotation, scale, enabled: true, isPlane: m.isPlane));
        }

#region Manifest and Scale Helpers
        static AnchorManifestEntry PlaneManifest(ARPlane p) => new AnchorManifestEntry {
            id = p.trackableId.ToString(),
            label = p.classifications.ToString(),   // "WallFace", "Floor", "Table", ...
            shape = "plane",
            isPlane = true,
        };

        static AnchorManifestEntry BoxManifest(ARBoundingBox b) => new AnchorManifestEntry {
            id = b.trackableId.ToString(),
            label = b.classifications.ToString(),
            shape = "volume",
            isPlane = false,
        };

        static Vector3 PlaneScale(ARPlane p) => new Vector3(p.size.x, p.size.y, 0f); // z=0 convention
        static Vector3 BoxScale(ARBoundingBox b) => b.size;
#endregion
    }
#endif
}
