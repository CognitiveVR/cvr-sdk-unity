using System.Collections.Generic;
using UnityEngine;

#if COGNITIVE3D_AR_FOUNDATION_6_0_OR_NEWER
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

namespace Cognitive3D
{
#if COGNITIVE3D_AR_FOUNDATION_6_0_OR_NEWER
    public class ARFoundationRoomCaptureProvider : IRoomCaptureProvider
    {
        const string SYNTHETIC_ROOM_ID = "arfoundation-room";

        // AR Foundation planes: surface lies in local XZ, normal = transform.up (+Y).
        // C3D expects: surface in local XY, normal = +Z. Rotate -90 about local X to convert.
        // (If normals end up facing the wrong way on the dashboard, flip to Euler(90, 0, 0).)
        static readonly Quaternion planeOffset = Quaternion.Euler(-90f, 0f, 0f);

        // trackablesChanged.updated fires ~every frame per trackable as AR refines a pose.
        // Without dedup that floods the boundary payload. Re-record only when the pose/scale 
        // moved past these thresholds AND enough time has passed.
        const float POS_EPSILON = 0.02f;          // 2 cm
        const float SCALE_EPSILON = 0.02f;        // 2 cm
        const float MIN_UPDATE_INTERVAL = 0.25f;  // seconds, per anchor

        ARRaycastManager raycastManager;
        ARBoundingBoxManager boundingBoxManager;
        ARPlaneManager planeManager;

        List<ARRaycastHit> hits = new List<ARRaycastHit>();

        // Last reported pose/scale/time per namespaced anchor id. Presence of a key also
        // means "we've already emitted the manifest add for this id".
        readonly Dictionary<string, (Vector3 pos, Vector3 scale, float time)> lastReported = new Dictionary<string, (Vector3, Vector3, float)>();
        
        const PlaneClassifications StructuralPlanes =
            PlaneClassifications.Floor
          | PlaneClassifications.Ceiling
          | PlaneClassifications.WallFace
          | PlaneClassifications.InvisibleWallFace
          | PlaneClassifications.DoorFrame
          | PlaneClassifications.WindowFrame;

        public virtual void Start()
        {
            planeManager = UnityEngine.Object.FindAnyObjectByType<ARPlaneManager>();
            boundingBoxManager = UnityEngine.Object.FindAnyObjectByType<ARBoundingBoxManager>();
            raycastManager = UnityEngine.Object.FindAnyObjectByType<ARRaycastManager>();

            if (planeManager == null && boundingBoxManager == null)
            {
                Util.logWarning("ARFoundationRoomCaptureProvider: no ARPlaneManager or ARBoundingBoxManager found. Room layout will not be captured.");
                return;
            }

            // Room exists for this session
            CoreInterface.RecordRoomManifest(new RoomManifestEntry {
                id = SYNTHETIC_ROOM_ID, label = SYNTHETIC_ROOM_ID,
                anchors = new List<AnchorManifestEntry>()
            });
            CoreInterface.RecordRoomData(RoomCaptureUtil.BuildRoomToggle(SYNTHETIC_ROOM_ID, true));

            if (boundingBoxManager != null)
            {
                foreach (var b in boundingBoxManager.trackables) HandleBox(b);
                boundingBoxManager.trackablesChanged.AddListener(OnBoundingBoxChanged);
            }

            if (planeManager != null)
            {
                foreach (var p in planeManager.trackables) HandlePlane(p);
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
            lastReported.Clear();
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

            worldHit = hit.pose.position;
            distance = hit.distance;

            if (hit.hitType == TrackableType.PlaneWithinBounds && planeManager != null)
            {
                anchorId = hit.trackableId.ToString();
                localHit = planeManager.GetPlane(hit.trackableId).transform.InverseTransformPoint(worldHit);
            }
            else if (hit.hitType == TrackableType.BoundingBox && boundingBoxManager != null
                && boundingBoxManager.TryGetBoundingBox(hit.trackableId, out var boundingBox))
            {
                anchorId = hit.trackableId.ToString();
                localHit = boundingBox.transform.InverseTransformPoint(worldHit);
            }
            else
            {
                anchorId = hit.trackableId.ToString();
            }

            return true;
        }

        public void OnBoundingBoxChanged(ARTrackablesChangedEventArgs<ARBoundingBox> changes)
        {
            foreach (var b in changes.added) HandleBox(b);
            foreach (var b in changes.updated) HandleBox(b);
            foreach (var kvp in changes.removed)
            {
                string id = kvp.Key.ToString();
                CoreInterface.RecordRoomData(RoomCaptureUtil.BuildRemoval(id));
                lastReported.Remove(id);
            }
        }

        public void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> changes)
        {
            foreach (var p in changes.added) HandlePlane(p);
            foreach (var p in changes.updated) HandlePlane(p);
            foreach (var kvp in changes.removed)
            {
                string id = kvp.Key.ToString();
                CoreInterface.RecordRoomData(RoomCaptureUtil.BuildRemoval(id));
                lastReported.Remove(id);
            }
        }

        void HandlePlane(ARPlane p)
        {
            if ((p.classifications & StructuralPlanes) == 0) return; // skip furniture / unclassified planes
            string id = p.trackableId.ToString();
            var pos = p.pose.position;
            var rot = p.pose.rotation * planeOffset;
            var scale = new Vector3(p.size.x, p.size.y, 0f);

            var planeManifest = new AnchorManifestEntry {
                id = p.trackableId.ToString(),
                label = p.classifications.ToString(),   // "WallFace", "Floor", "Table", ...
                shape = "plane",
                isPlane = true,
            };

            if (!lastReported.ContainsKey(id))
            {
                EmitAdded(id, planeManifest, pos, rot, scale, isPlane: true);
            }    
            else if (ShouldReport(id, pos, scale))
            {
               CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, pos, rot, scale, enabled: true, isPlane: true)); 
            }    
        }

        void HandleBox(ARBoundingBox b)
        {
            string id = b.trackableId.ToString();
            var pos = b.pose.position;
            var rot = b.pose.rotation;
            var scale = b.size;

            var boxManifest = new AnchorManifestEntry {
                id = b.trackableId.ToString(),
                label = b.classifications.ToString(),
                shape = "volume",
                isPlane = false,
            };

            if (!lastReported.ContainsKey(id))
            {
                EmitAdded(id, boxManifest, pos, rot, scale, isPlane: false);
            } 
            else if (ShouldReport(id, pos, scale))
            {
                CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, pos, rot, scale, enabled: true, isPlane: false));
            }   
        }

        void EmitAdded(string id, AnchorManifestEntry m, Vector3 pos, Quaternion rot, Vector3 scale, bool isPlane)
        {
            // additive partial manifest. One anchor under the synthetic room
            CoreInterface.RecordRoomManifest(new RoomManifestEntry {
                id = SYNTHETIC_ROOM_ID,
                anchors = new List<AnchorManifestEntry> { m }
            });
            CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, pos, rot, scale, enabled: true, isPlane: isPlane));
            lastReported[id] = (pos, scale, Time.unscaledTime);
        }

        // Drops redundant updates: returns false (and leaves state untouched) when the
        // anchor hasn't moved/resized past the thresholds, or was reported too recently.
        bool ShouldReport(string id, Vector3 pos, Vector3 scale)
        {
            float now = Time.unscaledTime;
            if (lastReported.TryGetValue(id, out var prev))
            {
                bool unchanged = (pos - prev.pos).sqrMagnitude < POS_EPSILON * POS_EPSILON
                              && (scale - prev.scale).sqrMagnitude < SCALE_EPSILON * SCALE_EPSILON;
                if (unchanged) return false;
                if (now - prev.time < MIN_UPDATE_INTERVAL) return false;
            }
            lastReported[id] = (pos, scale, now);
            return true;
        }
    }
#endif
}
