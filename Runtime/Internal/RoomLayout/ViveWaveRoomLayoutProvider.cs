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
    public class ViveWaveRoomLayoutProvider : IRoomLayoutProvider
    {
        private const string SYNTHETIC_ROOM_ID = "wave-room";

        private ScenePerceptionManager _manager;
        private ScenePlane[] _cachedPlanes;
        private SceneObject[] _cachedObjects;
        private bool _perceptionStarted;            // 2D plane perception
        private bool _objectsPerceptionStarted;     // 3D object perception
        private CancellationTokenSource _cts;       // cancels the perception-completion poll on Stop()

        public void Start()
        {
            // Locate the Wave perception manager in the scene
            _manager = Object.FindFirstObjectByType<ScenePerceptionManager>();
            if (_manager == null)
            {
                Util.logWarning("ViveWaveRoomLayoutProvider: ScenePerceptionManager not found in scene. Room layout will not be captured.");
                return;
            }

            // Confirm device supports it
            if ((Interop.WVR_GetSupportedFeatures() & (ulong)WVR_SupportedFeature.WVR_SupportedFeature_ScenePerception) == 0)
            {
                Util.logWarning("ViveWaveRoomLayoutProvider: Device does not support Scene Perception.");
                return;
            }

            if (_manager.StartScene() != WVR_Result.WVR_Success) return;

            // 2D plane perception is required
            if (_manager.StartScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_2dPlane) != WVR_Result.WVR_Success)
            {
                Util.logWarning("ViveWaveRoomLayoutProvider: failed to start 2D plane perception.");
                _manager.StopScene();
                return;
            }
            _perceptionStarted = true;

            // 3D object perception is best effort
            if (_manager.StartScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_3dObject) == WVR_Result.WVR_Success)
            {
                _objectsPerceptionStarted = true;
            }
            else
            {
                Util.logWarning("ViveWaveRoomLayoutProvider: failed to start 3D object perception. Volumes will not be captured this session.");
            }

            _cts = new CancellationTokenSource();
            _ = WaitForCompletionAndSweepAsync(_cts.Token);
        }

        private async Task WaitForCompletionAndSweepAsync(CancellationToken token)
        {
            try
            {
                // Poll until perception completes for both targets, or timeout.
                // If 3D object perception wasn't started (StartScenePerception failed for that target), we treat it as already done so we don't block on it
                float deadline = Time.realtimeSinceStartup + 10f;
                bool planesDone = false;
                bool objectsDone = !_objectsPerceptionStarted;

                while (Time.realtimeSinceStartup < deadline)
                {
                    token.ThrowIfCancellationRequested();

                    if (!planesDone)
                        planesDone = IsPerceptionCompleted(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_2dPlane);
                    if (!objectsDone)
                        objectsDone = IsPerceptionCompleted(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_3dObject);

                    if (planesDone && objectsDone)
                    {
                        EmitInitialManifest();
                        return;
                    }

                    await Task.Delay(250, token);
                }

                // Timed out. Emit whatever data is available rather than silently skipping
                Util.logWarning($"ViveWaveRoomLayoutProvider: perception incomplete after 10s. planesDone={planesDone} objectsDone={objectsDone}. Emitting available data.");
                EmitInitialManifest();
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception e) { Debug.LogException(e); }
        }

        private bool IsPerceptionCompleted(WVR_ScenePerceptionTarget target)
        {
            WVR_ScenePerceptionState state = WVR_ScenePerceptionState.WVR_ScenePerceptionState_Empty;
            return _manager.GetScenePerceptionState(target, ref state) == WVR_Result.WVR_Success
                && state == WVR_ScenePerceptionState.WVR_ScenePerceptionState_Completed;
        }

        private void EmitInitialManifest()
        {
            var planesAvailable = _manager.GetScenePlanes(ScenePerceptionManager.GetTrackingOriginModeFlags(), out _cachedPlanes) == WVR_Result.WVR_Success;
            var objectsAvailable = _manager.GetSceneObjects(ScenePerceptionManager.GetTrackingOriginModeFlags(), out _cachedObjects) == WVR_Result.WVR_Success;
            if (!planesAvailable && !objectsAvailable) return;

            var manifest = new RoomManifestEntry
            {
                id = SYNTHETIC_ROOM_ID,
                label = "Wave Room",
                anchors = new List<AnchorManifestEntry>()
            };

            if (planesAvailable)
            {
                foreach (var plane in _cachedPlanes)
                {
                    manifest.anchors.Add(new AnchorManifestEntry
                    {
                        id = plane.uuid.ToString(),
                        label = plane.planeLabel.ToString(),
                        shape = "plane",
                        isPlane = true,
                    });
                }
            }

            if (objectsAvailable)
            {
                foreach (var obj in _cachedObjects)
                {
                    manifest.anchors.Add(new AnchorManifestEntry
                    {
                        id = obj.uuid.ToString(),
                        label = obj.semanticName,
                        shape = "volume",
                        isPlane = false,
                    });
                }
            }

            CoreInterface.RecordRoomManifest(manifest);
            CoreInterface.RecordRoomData(RoomLayoutUtil.BuildRoomToggle(SYNTHETIC_ROOM_ID, true));

            if (planesAvailable)
            {
                foreach (var plane in _cachedPlanes)
                {
                    CoreInterface.RecordRoomData(new RoomDataEntry
                    {
                        id = plane.uuid.ToString(),
                        time = Util.Timestamp(),
                        position = plane.pose.position,
                        rotation = plane.pose.rotation,
                        scale = new Vector3(plane.extent.x, plane.extent.y, 0f),
                        enabled = true,
                        hasTransform = true,
                        isPlane = true,
                    });
                }
            }

            if (objectsAvailable)
            {
                foreach (var obj in _cachedObjects)
                {
                    CoreInterface.RecordRoomData(new RoomDataEntry
                    {
                        id = obj.uuid.ToString(),
                        time = Util.Timestamp(),
                        position = obj.pose.position,
                        rotation = obj.pose.rotation,
                        scale = obj.extent,
                        enabled = true,
                        hasTransform = true,
                        isPlane = false,
                    });
                }
            }
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
            if (_manager != null && _perceptionStarted)
            {
                _manager.StopScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_2dPlane);
                if (_objectsPerceptionStarted)
                    _manager.StopScenePerception(WVR_ScenePerceptionTarget.WVR_ScenePerceptionTarget_3dObject);
                _manager.StopScene();
            }

            // Clear so Restart() / next Start() re-finds the manager in the new scene
            // rather than reusing a potentially destroyed reference from the old one
            _manager = null;
            _perceptionStarted = false;
            _objectsPerceptionStarted = false;
            _cachedPlanes = null;
            _cachedObjects = null;
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
            if (_manager == null) return false;
            if (_cachedPlanes == null && _cachedObjects == null) return false;

            bool found = false;

            // ---- Planes (2D) ----
            if (_cachedPlanes != null)
            {
                foreach (var plane in _cachedPlanes)
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
            if (_cachedObjects != null)
            {
                foreach (var obj in _cachedObjects)
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
