using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Cognitive3D.Components;


#if C3D_VIVEWAVE && C3D_VIVEWAVE_SCENEPERCEPTION
using Wave.Native;
using Wave.Essence.ScenePerception;
#endif

namespace Cognitive3D
{
#if C3D_VIVEWAVE && C3D_VIVEWAVE_SCENEPERCEPTION
    public class ViveWaveRoomCaptureProvider : IRoomCaptureProvider
    {
        private const string SYNTHETIC_ROOM_ID = "wave-room";

        // Wave has no add/update/remove events; we poll the perception state and diff the current
        // snapshot against the previous one by uuid
        private const int POLL_INTERVAL_MS = 1000;
        private const float POS_EPSILON = 0.02f;     // 2 cm
        private const float SCALE_EPSILON = 0.02f;   // 2 cm
        private const float ROT_EPSILON_DEG = 1.0f;  // degrees

        private ScenePerceptionManager manager;
        private ScenePlane[] cachedPlanes;           // latest snapshot, used by the gaze raycast
        private SceneObject[] cachedObjects;
        private bool perceptionStarted;              // 2D plane perception
        private bool objectsPerceptionStarted;       // 3D object perception
        private bool roomEmitted;                    // room manifest + toggle emitted once
        private CancellationTokenSource cts;         // cancels the poll loop on Stop()

        // Last reported anchor per uuid, so we can diff for add/update/remove and so a removal can carry the anchor's last pose
        private readonly Dictionary<string, ScenePlane> prevPlanes = new Dictionary<string, ScenePlane>();
        private readonly Dictionary<string, SceneObject> prevObjects = new Dictionary<string, SceneObject>();

        public void Start()
        {
            // Locate the Wave perception manager in the scene
            manager = Object.FindFirstObjectByType<ScenePerceptionManager>();
            if (manager == null)
            {
                Util.logWarning("ViveWaveRoomCaptureProvider: ScenePerceptionManager not found in scene. Room layout will not be captured.");
                return;
            }

            // Confirm device supports it
            if ((Interop.WVR_GetSupportedFeatures() & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_ScenePerception) == 0)
            {
                Util.logWarning("ViveWaveRoomCaptureProvider: Device does not support Scene Perception.");
                return;
            }

            if (manager.StartScene() != WVR_Result.WVR_Success) return;

            // 2D plane perception is required
            if (manager.StartScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_2dPlane) != WVR_Result.WVR_Success)
            {
                Util.logWarning("ViveWaveRoomCaptureProvider: failed to start 2D plane perception.");
                manager.StopScene();
                return;
            }
            perceptionStarted = true;

            // 3D object perception is best effort
            if (manager.StartScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_3dObject) == WVR_Result.WVR_Success)
            {
                objectsPerceptionStarted = true;
            }
            else
            {
                Util.logWarning("ViveWaveRoomCaptureProvider: failed to start 3D object perception. Volumes will not be captured this session.");
            }

            cts = new CancellationTokenSource();
            _ = PollLoopAsync(cts.Token);
        }

        // Polls perception state and reconciles the current snapshot against the previous one.
        // Replaces the old one-shot "wait up to 10s then emit once" approach so layout changes and
        // rescans are captured continuously, the same way the AR Foundation / Meta providers do.
        private async Task PollLoopAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    if (perceptionStarted
                        && IsPerceptionCompleted(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_2dPlane)
                        && manager.GetScenePlanes(ScenePerceptionManager.GetTrackingOriginModeFlags(), out var planes) == WVR_Result.WVR_Success)
                    {
                        ReconcilePlanes(planes);
                    }

                    if (objectsPerceptionStarted
                        && IsPerceptionCompleted(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_3dObject)
                        && manager.GetSceneObjects(ScenePerceptionManager.GetTrackingOriginModeFlags(), out var objects) == WVR_Result.WVR_Success)
                    {
                        ReconcileObjects(objects);
                    }

                    await Task.Delay(POLL_INTERVAL_MS, token);
                }
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        private bool IsPerceptionCompleted(WVR_ScenePerceptionTarget target)
        {
            WVR_ScenePerceptionState state = WVR_ScenePerceptionState.WVR_ScenePerceptionState_Empty;
            return manager.GetScenePerceptionState(target, ref state) == WVR_Result.WVR_Success
                && state == WVR_ScenePerceptionState.WVR_ScenePerceptionState_Completed;
        }

        // Emits the synthetic room manifest + enabled toggle once on the first anchor data
        private void EnsureRoomEmitted()
        {
            if (roomEmitted) return;
            roomEmitted = true;
            CoreInterface.RecordRoomManifest(new RoomManifestEntry
            {
                id = SYNTHETIC_ROOM_ID,
                label = "Wave Room",
                anchors = new List<AnchorManifestEntry>()
            });
            CoreInterface.RecordRoomData(RoomCaptureUtil.BuildRoomToggle(SYNTHETIC_ROOM_ID, true));
        }

        private void ReconcilePlanes(ScenePlane[] current)
        {
            EnsureRoomEmitted();
            currentPlaneIds.Clear();
            foreach (var p in current) currentPlaneIds[p.uuid.ToString()] = p;

            // Removals: any previously-reported plane absent from the current snapshot
            staleScratch.Clear();
            foreach (var kvp in prevPlanes)
                if (!currentPlaneIds.ContainsKey(kvp.Key)) staleScratch.Add(kvp.Key);
            foreach (var id in staleScratch)
            {
                var prev = prevPlanes[id];
                CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, prev.pose.position, prev.pose.rotation,
                    new Vector3(prev.extent.x, prev.extent.y, 0f), enabled: false, isPlane: true));
                prevPlanes.Remove(id);
            }

            // Adds + updates
            foreach (var kv in currentPlaneIds)
            {
                string id = kv.Key;
                var plane = kv.Value;
                var scale = new Vector3(plane.extent.x, plane.extent.y, 0f);

                if (!prevPlanes.ContainsKey(id))
                {
                    CoreInterface.RecordRoomManifest(new RoomManifestEntry
                    {
                        id = SYNTHETIC_ROOM_ID,
                        anchors = new List<AnchorManifestEntry>
                        {
                            new AnchorManifestEntry
                            {
                                id = id,
                                label = RoomCaptureUtil.NormalizeLabel(plane.planeLabel.ToString()),
                                shape = "plane",
                                isPlane = true,
                            }
                        }
                    });
                    CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, plane.pose.position, plane.pose.rotation, scale, enabled: true, isPlane: true));
                    prevPlanes[id] = plane;
                }
                else if (PoseOrSizeChanged(prevPlanes[id].pose, new Vector3(prevPlanes[id].extent.x, prevPlanes[id].extent.y, 0f), plane.pose, scale))
                {
                    CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, plane.pose.position, plane.pose.rotation, scale, enabled: true, isPlane: true));
                    prevPlanes[id] = plane;
                }
            }

            cachedPlanes = current;
        }

        private void ReconcileObjects(SceneObject[] current)
        {
            EnsureRoomEmitted();

            currentObjectIds.Clear();
            foreach (var o in current) currentObjectIds[o.uuid.ToString()] = o;

            staleScratch.Clear();
            foreach (var kvp in prevObjects)
                if (!currentObjectIds.ContainsKey(kvp.Key)) staleScratch.Add(kvp.Key);
            foreach (var id in staleScratch)
            {
                var prev = prevObjects[id];
                CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, prev.pose.position, prev.pose.rotation,
                    prev.extent, enabled: false, isPlane: false));
                prevObjects.Remove(id);
            }

            foreach (var kv in currentObjectIds)
            {
                string id = kv.Key;
                var obj = kv.Value;

                if (!prevObjects.ContainsKey(id))
                {
                    CoreInterface.RecordRoomManifest(new RoomManifestEntry
                    {
                        id = SYNTHETIC_ROOM_ID,
                        anchors = new List<AnchorManifestEntry>
                        {
                            new AnchorManifestEntry
                            {
                                id = id,
                                label = RoomCaptureUtil.NormalizeLabel(obj.semanticName),
                                shape = "volume",
                                isPlane = false,
                            }
                        }
                    });
                    CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, obj.pose.position, obj.pose.rotation, obj.extent, enabled: true, isPlane: false));
                    prevObjects[id] = obj;
                }
                else if (PoseOrSizeChanged(prevObjects[id].pose, prevObjects[id].extent, obj.pose, obj.extent))
                {
                    CoreInterface.RecordRoomData(RoomCaptureUtil.BuildAnchorData(id, obj.pose.position, obj.pose.rotation, obj.extent, enabled: true, isPlane: false));
                    prevObjects[id] = obj;
                }
            }

            cachedObjects = current;
        }

        private readonly List<string> staleScratch = new List<string>();
        private readonly Dictionary<string, ScenePlane> currentPlaneIds = new Dictionary<string, ScenePlane>();
        private readonly Dictionary<string, SceneObject> currentObjectIds = new Dictionary<string, SceneObject>();

        private static bool PoseOrSizeChanged(Pose a, Vector3 extentA, Pose b, Vector3 extentB)
        {
            return (a.position - b.position).sqrMagnitude > POS_EPSILON * POS_EPSILON
                || (extentA - extentB).sqrMagnitude > SCALE_EPSILON * SCALE_EPSILON
                || Quaternion.Angle(a.rotation, b.rotation) > ROT_EPSILON_DEG;
        }

        public void Stop()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
            if (manager != null && perceptionStarted)
            {
                manager.StopScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_2dPlane);
                if (objectsPerceptionStarted)
                    manager.StopScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_3dObject);
                manager.StopScene();
            }

            manager = null;
            perceptionStarted = false;
            objectsPerceptionStarted = false;
            roomEmitted = false;
            cachedPlanes = null;
            cachedObjects = null;
            prevPlanes.Clear();
            prevObjects.Clear();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        public bool TryGetGazedAnchor(Ray ray, float maxDistance, out string anchorId, out Vector3 worldHit, out Vector3 localHit, out float distance)
        {
            anchorId = null;
            worldHit = Vector3.zero;
            localHit = Vector3.zero;
            distance = float.MaxValue;
            if (manager == null) return false;
            if (cachedPlanes == null && cachedObjects == null) return false;

            bool found = false;

            // ---- Planes (2D) ----
            if (cachedPlanes != null)
            {
                foreach (var plane in cachedPlanes)
                {
                    // Transform the world ray into the plane's local frame.
                    // Wave planes lie on local z=0, normal along +Z, sized by extent (X = width, Y = height)
                    Quaternion invRot = Quaternion.Inverse(plane.pose.rotation);
                    Vector3 localOrigin = invRot * (ray.origin - plane.pose.position);
                    Vector3 localDir = invRot * ray.direction;

                    if (Mathf.Abs(localDir.z) < 1e-6f) continue;    // ray parallel to plane
                    float t = -localOrigin.z / localDir.z;
                    if (t < 0 || t > maxDistance) continue; // behind us or out of range
                    if (t >= distance) continue;    // not closer than what we have

                    Vector3 localPt = localOrigin + t * localDir;
                    float halfW = plane.extent.x * 0.5f;
                    float halfH = plane.extent.y * 0.5f;
                    if (Mathf.Abs(localPt.x) > halfW) continue; // outside the plane's bounds
                    if (Mathf.Abs(localPt.y) > halfH) continue;

                    distance = t;
                    worldHit = plane.pose.position + plane.pose.rotation * localPt;
                    localHit = localPt;
                    anchorId = plane.uuid.ToString();
                    found = true;
                }
            }

            // ---- Volumes (3D) ----
            if (cachedObjects != null)
            {
                foreach (var obj in cachedObjects)
                {
                    Quaternion invRot = Quaternion.Inverse(obj.pose.rotation);
                    Vector3 localOrigin = invRot * (ray.origin - obj.pose.position);
                    Vector3 localDir = invRot * ray.direction;

                    Vector3 half = obj.extent * 0.5f;

                    float tmin = 0f;
                    float tmax = maxDistance;
                    bool hitOK = true;

                    for (int axis = 0; axis < 3; axis++)
                    {
                        float o = localOrigin[axis];
                        float d = localDir[axis];
                        float mn = -half[axis];
                        float mx = half[axis];

                        if (Mathf.Abs(d) < 1e-6f)
                        {
                            // Ray parallel to this slab, must already be inside the slab for any hit
                            if (o < mn || o > mx) { hitOK = false; break; }
                            continue;
                        }

                        float invD = 1f / d;
                        float t0 = (mn - o) * invD;
                        float t1 = (mx - o) * invD;
                        if (t0 > t1) { var tmp = t0; t0 = t1; t1 = tmp; }
                        tmin = Mathf.Max(tmin, t0);
                        tmax = Mathf.Min(tmax, t1);
                        if (tmax < tmin) { hitOK = false; break; }
                    }

                    if (!hitOK) continue;
                    if (tmin >= distance) continue; // not closer than what we have

                    Vector3 localPt = localOrigin + tmin * localDir;
                    distance = tmin;
                    worldHit = obj.pose.position + obj.pose.rotation * localPt;
                    localHit = localPt;
                    anchorId = obj.uuid.ToString();
                    found = true;
                }
            }

            if (!found) distance = 0f;
            return found;
        }
    }
#endif
}
