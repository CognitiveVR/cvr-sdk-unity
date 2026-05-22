using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


#if COGNITIVE3D_META_MRUK_68_OR_NEWER
using Meta.XR.MRUtilityKit;
#endif

namespace Cognitive3D
{
#if COGNITIVE3D_META_MRUK_68_OR_NEWER
    internal class MetaRoomLayoutProvider : IRoomLayoutProvider
    {
        public virtual void Start()
        {
            StartMrukListeners();
        }

        public virtual void Stop()
        {
            StopMrukListeners();
        }

        public virtual void Restart()
        {
            StopMrukListeners();
            StartMrukListeners();
        }

        public virtual bool TryGetGazedAnchor(Ray ray, float maxDistance, out string anchorId, out Vector3 worldHit, out float distance)
        {
            anchorId = null;
            worldHit = Vector3.zero;
            distance = 0f;

            if (MRUK.Instance == null) return false;
            if (GameplayReferences.HMDCameraComponent == null) return false;

            foreach (var room in MRUK.Instance.Rooms)
            {
                if (room == null) continue;
                if (!room.Raycast(ray, maxDistance, out RaycastHit hit, out MRUKAnchor anchor)) continue;
                if (anchor == null) continue;

                anchorId = anchor.Anchor.Uuid.ToString();
                worldHit = hit.point;
                distance = hit.distance;
                return true;
            }
            return false;
        }

        private struct RoomListeners
        {
            public MRUKRoom room;
            public UnityAction<MRUKAnchor> onCreated;
            public UnityAction<MRUKAnchor> onUpdated;
            public UnityAction<MRUKAnchor> onRemoved;
        }

        private readonly Dictionary<string, RoomListeners> _attached = new Dictionary<string, RoomListeners>();

        // Used on RoomRemoved to cascade enabled:false for every child anchor
        private readonly Dictionary<string, HashSet<string>> _roomAnchors = new Dictionary<string, HashSet<string>>();

        // Held listener references so RemoveListener gets the same delegate instance
        private UnityAction<MRUKRoom> _onRoomCreated;
        private UnityAction<MRUKRoom> _onRoomRemoved;
        private UnityAction<MRUKRoom> _onRoomUpdated;

        private void StartMrukListeners()
        {
            if (MRUK.Instance == null)
            {
                Util.logWarning("MRUK instance not found at session begin. Room layout will not be captured.");
                return;
            }

            // Subscribe to room level events. Hold delegate refs for clean teardown.
            _onRoomCreated = OnRoomCreated;
            _onRoomRemoved = OnRoomRemoved;
            _onRoomUpdated = OnRoomUpdated;
            MRUK.Instance.RoomCreatedEvent.AddListener(_onRoomCreated);
            MRUK.Instance.RoomRemovedEvent.AddListener(_onRoomRemoved);
            MRUK.Instance.RoomUpdatedEvent.AddListener(_onRoomUpdated);

            // Catches the case where MRUK loaded before the session began
            foreach (var room in MRUK.Instance.Rooms)
            {
                EmitRoomManifestAndAttach(room);
            }
        }

        private void StopMrukListeners()
        {
            if (MRUK.Instance != null)
            {
                if (_onRoomCreated != null) MRUK.Instance.RoomCreatedEvent.RemoveListener(_onRoomCreated);
                if (_onRoomRemoved != null) MRUK.Instance.RoomRemovedEvent.RemoveListener(_onRoomRemoved);
                if (_onRoomUpdated != null) MRUK.Instance.RoomUpdatedEvent.RemoveListener(_onRoomUpdated);
            }
            _onRoomCreated = _onRoomRemoved = _onRoomUpdated = null;

            // Detach all per room anchor listeners
            foreach (var kvp in _attached)
            {
                DetachRoomListenersInternal(kvp.Value);
            }
            _attached.Clear();
            _roomAnchors.Clear();
        }

        // -------- Room event handlers --------

        private void OnRoomCreated(MRUKRoom room)
        {
            EmitRoomManifestAndAttach(room);
        }

        private void OnRoomRemoved(MRUKRoom room)
        {
            string roomId = room.Anchor.Uuid.ToString();

            if (_roomAnchors.TryGetValue(roomId, out var anchorIds))
            {
                foreach (var aid in anchorIds)
                {
                    CoreInterface.RecordRoomData(BuildRemoval(aid));
                }
            }
            CoreInterface.RecordRoomData(BuildRoomToggle(roomId, false));

            // Detach listeners and forget membership
            if (_attached.TryGetValue(roomId, out var listeners))
            {
                DetachRoomListenersInternal(listeners);
                _attached.Remove(roomId);
            }
            _roomAnchors.Remove(roomId);
        }

        private void OnRoomUpdated(MRUKRoom room)
        {
            string roomId = room.Anchor.Uuid.ToString();
            var entry = new RoomManifestEntry
            {
                id = roomId,
                anchors = new List<AnchorManifestEntry>()
            };
            CoreInterface.RecordRoomManifest(entry);
            CoreInterface.RecordRoomData(BuildRoomToggle(roomId, true));
        }

        // -------- Anchor event handlers --------

        private void OnAnchorCreated(string roomId, MRUKAnchor a)
        {
            if (a == null) return;

            // Manifest for this single anchor. Additive merge on the consumer side
            if (TryBuildAnchorManifest(a, out var amEntry))
            {
                var partial = new RoomManifestEntry
                {
                    id = roomId,
                    anchors = new List<AnchorManifestEntry> { amEntry }
                };
                CoreInterface.RecordRoomManifest(partial);
                TrackAnchorId(roomId, amEntry.id);

                Vector3 scale = Vector3.zero;
                if (a.PlaneRect.HasValue)
                {
                    var r = a.PlaneRect.Value;
                    scale = new Vector3(r.width, r.height, 0f);
                }
                else if (a.VolumeBounds.HasValue)
                {
                    scale = a.VolumeBounds.Value.size;
                }

                // Initial transform + enabled:true
                CoreInterface.RecordRoomData(BuildAnchorData(a, scale, enabled: true, isPlane: amEntry.isPlane));
            }
        }

        private void OnAnchorUpdated(string roomId, MRUKAnchor a)
        {
            if (a == null) return;
            bool isPlane = a.PlaneRect.HasValue;
            Vector3 scale = Vector3.one;
            if (a.PlaneRect.HasValue)
            {
                var r = a.PlaneRect.Value;
                scale = new Vector3(r.width, r.height, 0f);
            }
            else if (a.VolumeBounds.HasValue)
            {
                scale = a.VolumeBounds.Value.size;
            }
            CoreInterface.RecordRoomData(BuildAnchorData(a, scale, enabled: true, isPlane: isPlane));
        }

        private void OnAnchorRemoved(string roomId, MRUKAnchor a)
        {
            if (a == null) return;
            string aid = a.Anchor.Uuid.ToString();
            CoreInterface.RecordRoomData(BuildRemoval(aid));
            UntrackAnchorId(roomId, aid);
        }

        // -------- Helpers --------

        private void EmitRoomState(MRUKRoom room)
        {
            if (room == null) return;
            string roomId = room.Anchor.Uuid.ToString();

            var manifest = BuildRoomManifest(room);
            CoreInterface.RecordRoomManifest(manifest);

            // Room is enabled
            CoreInterface.RecordRoomData(BuildRoomToggle(roomId, true));

            // Seed anchor membership + initial data
            if (!_roomAnchors.TryGetValue(roomId, out var idSet))
            {
                idSet = new HashSet<string>();
                _roomAnchors[roomId] = idSet;
            }
            foreach (var am in manifest.anchors)
            {
                idSet.Add(am.id);
            }
            foreach (var a in room.Anchors)
            {
                Vector3 scale = Vector3.zero;
                if (a.PlaneRect.HasValue)
                {
                    var r = a.PlaneRect.Value;
                    scale = new Vector3(r.width, r.height, 0f);
                }
                else if (a.VolumeBounds.HasValue)
                {
                    scale = a.VolumeBounds.Value.size;
                }
                if (!TryBuildAnchorManifest(a, out var amEntry)) continue;
                CoreInterface.RecordRoomData(BuildAnchorData(a, scale, enabled: true, isPlane: amEntry.isPlane));
            }
        }

        /// <summary>
        /// Emits the full manifest for a room (anchors nested), records initial data
        /// entries for the room and each anchor, and attaches per-room anchor listeners
        /// </summary>
        private void EmitRoomManifestAndAttach(MRUKRoom room)
        {
            if (room == null) return;
            EmitRoomState(room);
            AttachRoomListeners(room, room.Anchor.Uuid.ToString());
        }

        private void AttachRoomListeners(MRUKRoom room, string roomId)
        {
            if (_attached.ContainsKey(roomId)) return;

            UnityAction<MRUKAnchor> onCreated = a => OnAnchorCreated(roomId, a);
            UnityAction<MRUKAnchor> onUpdated = a => OnAnchorUpdated(roomId, a);
            UnityAction<MRUKAnchor> onRemoved = a => OnAnchorRemoved(roomId, a);

            room.AnchorCreatedEvent.AddListener(onCreated);
            room.AnchorUpdatedEvent.AddListener(onUpdated);
            room.AnchorRemovedEvent.AddListener(onRemoved);

            _attached[roomId] = new RoomListeners
            {
                room = room,
                onCreated = onCreated,
                onUpdated = onUpdated,
                onRemoved = onRemoved
            };
        }

        private static void DetachRoomListenersInternal(RoomListeners l)
        {
            if (l.room == null) return;
            if (l.onCreated != null) l.room.AnchorCreatedEvent.RemoveListener(l.onCreated);
            if (l.onUpdated != null) l.room.AnchorUpdatedEvent.RemoveListener(l.onUpdated);
            if (l.onRemoved != null) l.room.AnchorRemovedEvent.RemoveListener(l.onRemoved);
        }

        private void TrackAnchorId(string roomId, string anchorId)
        {
            if (!_roomAnchors.TryGetValue(roomId, out var set))
            {
                set = new HashSet<string>();
                _roomAnchors[roomId] = set;
            }
            set.Add(anchorId);
        }

        private void UntrackAnchorId(string roomId, string anchorId)
        {
            if (_roomAnchors.TryGetValue(roomId, out var set))
            {
                set.Remove(anchorId);
            }
        }

        /// <summary>
        /// Builds a full manifest entry for one MRUK room (nested anchors inside)
        /// </summary>
        internal static RoomManifestEntry BuildRoomManifest(MRUKRoom room)
        {
            var entry = new RoomManifestEntry
            {
                id = room.Anchor.Uuid.ToString(),
                label = "Unnamed",
                anchors = new List<AnchorManifestEntry>()
            };
            foreach (var a in room.Anchors)
            {
                if (TryBuildAnchorManifest(a, out var ae))
                    entry.anchors.Add(ae);
            }
            return entry;
        }

        internal static bool TryBuildAnchorManifest(MRUKAnchor obj, out AnchorManifestEntry entry)
        {
            entry = default;
            if (obj == null) return false;
            if (obj.PlaneRect.HasValue)
            {
                entry = new AnchorManifestEntry
                {
                    id = obj.Anchor.Uuid.ToString(),
                    label = obj.Label.ToString(),
                    shape = "plane",
                    isPlane = true
                };
                return true;
            }
            if (obj.VolumeBounds.HasValue)
            {
                entry = new AnchorManifestEntry
                {
                    id = obj.Anchor.Uuid.ToString(),
                    label = obj.Label.ToString(),
                    shape = "volume",
                    isPlane = false
                };
                return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a data entry for an anchor: pos/rot/scale + enabled
        /// </summary>
        internal static RoomDataEntry BuildAnchorData(MRUKAnchor anchor, Vector3 scale, bool enabled, bool isPlane)
        {
            return new RoomDataEntry
            {
                id = anchor.Anchor.Uuid.ToString(),
                time = Util.Timestamp(),
                position = anchor.transform.position,
                rotation = anchor.transform.rotation,
                scale = scale,
                enabled = enabled,
                hasTransform = true,
                isPlane = isPlane
            };
        }

        /// <summary>
        /// Data entry for a removed anchor, only id/time/enabled meaningful
        /// </summary>
        internal static RoomDataEntry BuildRemoval(string id)
        {
            return new RoomDataEntry
            {
                id = id,
                time = Util.Timestamp(),
                enabled = false,
                hasTransform = false
            };
        }

        /// <summary>
        /// Data entry for a room toggle, only id/time/enabled meaningful
        /// </summary>
        internal static RoomDataEntry BuildRoomToggle(string roomId, bool enabled)
        {
            return new RoomDataEntry
            {
                id = roomId,
                time = Util.Timestamp(),
                enabled = enabled,
                hasTransform = false
            };
        }
    }
#endif
}
