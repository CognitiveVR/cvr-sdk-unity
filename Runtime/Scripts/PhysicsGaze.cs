using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;

//physics raycast from camera
//adds gazepoint at hit.point

namespace Cognitive3D
{
    /// <summary>
    /// Result of raycasting to UI elements (Canvas or UI Image DynamicObjects)
    /// </summary>
    public struct UIRaycastResult
    {
        public bool didHit;
        public float distance;
        public RectTransform rectTransform;
        public DynamicObject dynamicObject; // null for regular canvases, populated for UI Image Dynamics
        public Vector3 worldPosition;
        public Vector3 localPosition;
        public bool isUIImageDynamic; // true if it's a UI Image Dynamic, false if it's a canvas
    }

    [AddComponentMenu("Cognitive3D/Internal/Physics Gaze")]
    public class PhysicsGaze : GazeBase
    {
        private static PhysicsGaze instance;
        public static PhysicsGaze Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<PhysicsGaze>();
                }
                return instance;
            }
        }

        public delegate void onGazeTick();
        /// <summary>
        /// Called on a 0.1 second interval
        /// </summary>
        public static event onGazeTick OnGazeTick;
        private static void InvokeGazeTickEvent() { if (OnGazeTick != null) { OnGazeTick(); } }

        public bool DrawDebugLines;

        /// <summary>
        /// Enables recording gaze on active canvas rects without requiring colliders
        /// </summary>
        [Tooltip("Enables recording gaze on active canvas rects without requiring colliders")]
        public bool enableCanvasGaze;

        /// <summary>
        /// Describes how canvases are cached
        /// FindObjectsAlways searches the scene every tick. Most expensive, but very flexible when spawning canvas prefabs
        /// ListOfCanvases searches through all canvases in 'targetCanvases'. Spawned canvases will have to be added manually
        /// FindEachSceneLoad finds all canvases in the scene once on each scene load, then uses the results each tick. Spawned canvases will have to be added manually
        /// </summary>
        public enum CanvasCacheBehaviour
        {
            FindEachSceneLoad,
            ListOfCanvases,
            FindObjectsAlways
        }
        [Tooltip("Describes how canvases are cached. FindObjectsAlways gets canvases in the scene each tick, ListOfCanvases only calculates hits on specific canvases, FindEachSceneLoad finds canvases in the scene once on each scene load")]
        public CanvasCacheBehaviour canvasCacheBehaviour;

        /// <summary>
        /// Used with FindObjectsAlways and ListOfCanvases canvas cache behaviours. Used when a canvas is destroyed or removed from the cache list
        /// FindObjects finds all canvases in the scene and updates the list
        /// TrimList only removes canvases from the cache list
        /// </summary>
        public enum CanvasRefreshBehaviour
        {
            FindObjects,
            TrimList,
        }
        [Tooltip("Used with FindObjectsAlways and ListOfCanvases behaviours. When a canvas is destroyed, optionally find objects in the scene, or trim the removed canvas from the cached list")]
        public CanvasRefreshBehaviour canvasRefreshBehaviour;

        public List<Canvas> targetCanvases;
        RectTransform[] cachedCanvasRectTransforms = new RectTransform[0];

        internal readonly List<DynamicObject> uiImageDynamics = new List<DynamicObject>();
        internal readonly List<RectTransform> uiImageRectTransforms = new List<RectTransform>();

        // Cache for Canvas DynamicObjects (always tracked regardless of enableCanvasGaze)
        internal readonly List<DynamicObject> canvasDynamics = new List<DynamicObject>();
        internal readonly List<RectTransform> canvasDynamicRectTransforms = new List<RectTransform>();

        public override void Initialize()
        {
            base.Initialize();
            if (instance == null) instance = this;
            if (GameplayReferences.HMD == null) { Cognitive3D.Util.logWarning("HMD is null! Physics Gaze needs a camera to function"); }
            StartCoroutine(Tick());
            Cognitive3D_Manager.OnPreSessionEnd += OnEndSessionEvent;

            if (enableCanvasGaze && canvasCacheBehaviour == CanvasCacheBehaviour.FindEachSceneLoad)
            {
                Cognitive3D_Manager.OnLevelLoaded += Cognitive3D_Manager_OnLevelLoaded;
                var canvases = FindObjectsOfType<Canvas>();
                RefreshCanvasTransforms(canvases);
            }

            RefreshUIDynamics();
        }

        private void Cognitive3D_Manager_OnLevelLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId)
        {
            var canvases = FindObjectsOfType<Canvas>();
            RefreshCanvasTransforms(canvases);
            RefreshUIDynamics();
        }

        IEnumerator Tick()
        {
            if (GameplayReferences.HMD == null) { yield return null; }

            while (Cognitive3D_Manager.IsInitialized)
            {
                yield return Cognitive3D_Manager.PlayerSnapshotInverval;
                
                try
                {
                    InvokeGazeTickEvent();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }

                Ray ray = GazeHelper.GetCurrentWorldGazeRay();

                // Unified raycast to all UI elements (Canvas and UI Image DynamicObjects)
                UIRaycastResult uiHit = RaycastToUIElements(ray.origin, ray.direction);

                if (Cognitive3D_Preferences.Instance.EnableGaze == true && GameplayReferences.HMDCameraComponent && DynamicRaycast(ray.origin, ray.direction, GameplayReferences.HMDCameraComponent.farClipPlane, 0.05f, out var hitDistance, out var hitDynamic, out var hitWorld, out var hitLocal, out var hitcoord)) //hit dynamic
                {
                    // Determine which hit is closest: UI element or regular dynamic
                    if (uiHit.didHit && uiHit.distance < hitDistance)
                    {
                        // UI element is closer than the dynamic object
                        if (uiHit.isUIImageDynamic)
                        {
                            GazeHelper.RecordUIImageGaze(uiHit.dynamicObject, uiHit.localPosition, uiHit.worldPosition, ray);
                        }
                        else
                        {
                            GazeHelper.RecordCanvasGaze(uiHit.dynamicObject, uiHit.rectTransform, uiHit.worldPosition, ray);
                        }
                    }
                    else
                    {
                        // Regular dynamic is closest
                        string ObjectId = hitDynamic.GetId();
                        var mediacomponent = hitDynamic.GetComponent<MediaComponent>();
                        if (mediacomponent != null)
                        {
                            var mediatime = mediacomponent.IsVideo ? (int)((mediacomponent.VideoPlayerFrame / mediacomponent.VideoPlayerFrameRate) * 1000) : 0;
                            var mediauvs = hitcoord;
                            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, GameplayReferences.HMD.position, GameplayReferences.HMD.rotation, mediacomponent.MediaId, mediatime, mediauvs);
                        }
                        else
                        {
                            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, hitLocal, ray.origin, GameplayReferences.HMD.rotation);
                        }

                        //debugging
                        if (DrawDebugLines)
                            DrawGazePoint(GameplayReferences.HMD.position, hitWorld, new Color(1, 0, 1, 0.5f));

                        //active session view
                        AddGazeToDisplay(hitWorld, hitLocal, hitDynamic);
                    }
                }
                else if (Cognitive3D_Preferences.Instance.EnableGaze == true && GameplayReferences.HMDCameraComponent && Physics.Raycast(ray, out var hit, GameplayReferences.HMDCameraComponent.farClipPlane, Cognitive3D_Preferences.Instance.GazeLayerMask, Cognitive3D_Preferences.Instance.TriggerInteraction))
                {
                    // Determine which hit is closest: UI element or world hit
                    if (uiHit.didHit && uiHit.distance < hit.distance)
                    {
                        // UI element is closer than the world hit
                        if (uiHit.isUIImageDynamic)
                        {
                            GazeHelper.RecordUIImageGaze(uiHit.dynamicObject, uiHit.localPosition, uiHit.worldPosition, ray);
                        }
                        else
                        {
                            GazeHelper.RecordCanvasGaze(uiHit.dynamicObject, uiHit.rectTransform, uiHit.worldPosition, ray);
                        }
                    }
                    else
                    {
                        // World hit is closest
                        Vector3 pos = GameplayReferences.HMD.position;
                        Vector3 gazepoint = hit.point;
                        Quaternion rot = GameplayReferences.HMD.rotation;

                        //hit world
                        GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), gazepoint, pos, rot);

                        //debugging
                        DrawGazePoint(pos,gazepoint,Color.red);

                        //active session view
                        AddGazeToDisplay(hit.point);
                    }
                }
                else if (GameplayReferences.HMD) //hit sky / farclip / gaze disabled. record HMD position and rotation
                {
                    // Check if we hit any UI element, otherwise record sky hit
                    if (uiHit.didHit)
                    {
                        // UI element hit (no other colliders hit)
                        if (uiHit.isUIImageDynamic)
                        {
                            GazeHelper.RecordUIImageGaze(uiHit.dynamicObject, uiHit.localPosition, uiHit.worldPosition, ray);
                        }
                        else
                        {
                            GazeHelper.RecordCanvasGaze(uiHit.dynamicObject, uiHit.rectTransform, uiHit.worldPosition, ray);
                        }
                    }
                    else
                    {
                        // Sky hit
                        Vector3 pos = GameplayReferences.HMD.position;
                        Quaternion rot = GameplayReferences.HMD.rotation;
                        Vector3 displayPosition = GameplayReferences.HMD.forward * GameplayReferences.HMDCameraComponent.farClipPlane;
                        GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot);

                        //debugging
                        if (DrawDebugLines)
                            Debug.DrawRay(pos, displayPosition, Color.cyan, 0.1f);

                        //active session view
                        AddGazeToDisplay(displayPosition);
                    }
                }
            }
        }

        void AddGazeToDisplay(Vector3 worldPoint)
        {
            if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();
            DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = worldPoint;
            DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = Vector3.zero;
            DisplayGazePoints[DisplayGazePoints.Count].Transform = null;
            DisplayGazePoints[DisplayGazePoints.Count].IsLocal = false;
            DisplayGazePoints.Update();
        }

        void AddGazeToDisplay(Vector3 worldPoint, Vector3 localPoint, DynamicObject hitDynamic)
        {
            if (DisplayGazePoints[DisplayGazePoints.Count] == null)
                DisplayGazePoints[DisplayGazePoints.Count] = new ThreadGazePoint();
            DisplayGazePoints[DisplayGazePoints.Count].WorldPoint = worldPoint;
            DisplayGazePoints[DisplayGazePoints.Count].LocalPoint = localPoint;
            DisplayGazePoints[DisplayGazePoints.Count].Transform = hitDynamic.transform;
            DisplayGazePoints[DisplayGazePoints.Count].IsLocal = true;
            DisplayGazePoints.Update();
        }

        void DrawGazePoint(Vector3 start, Vector3 worldPoint,Color color)
        {
            if (!DrawDebugLines) { return; }
            Debug.DrawLine(start, worldPoint, color, 0.1f);
            Debug.DrawRay(worldPoint, Vector3.right, Color.red, 10);
            Debug.DrawRay(worldPoint, Vector3.forward, Color.blue, 10);
            Debug.DrawRay(worldPoint, Vector3.up, Color.green, 10);
        }

        private void OnEndSessionEvent()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= OnEndSessionEvent;
            Cognitive3D_Manager.OnLevelLoaded -= Cognitive3D_Manager_OnLevelLoaded;
            Destroy(this);
        }

        /// <summary>
        /// canvases to iterate through. This must not include null objects
        /// </summary>
        /// <param name="canvases"></param>
        void RefreshCanvasTransforms(Canvas[] canvases)
        {
            //remove empty canvases
            List<RectTransform> tempRectTransforms = new List<RectTransform>(canvases.Length);
            for (int i = 0; i < canvases.Length; i++)
            {
                tempRectTransforms.Add(canvases[i].GetComponent<RectTransform>());
            }

            cachedCanvasRectTransforms = tempRectTransforms.ToArray();
        }

        void RefreshCanvasTransforms(List<Canvas> canvases)
        {
            //remove empty canvases
            List<RectTransform> tempRectTransforms = new List<RectTransform>(canvases.Count);
            for (int i = 0; i < canvases.Count; i++)
            {
                tempRectTransforms.Add(canvases[i].GetComponent<RectTransform>());
            }

            cachedCanvasRectTransforms = tempRectTransforms.ToArray();
        }

        /// <summary>
        /// Unified method to raycast to all UI elements (both Canvas and UI Image DynamicObjects)
        /// Returns the closest hit of any type
        /// </summary>
        UIRaycastResult RaycastToUIElements(Vector3 position, Vector3 forward)
        {
            UIRaycastResult result = new UIRaycastResult();
            result.didHit = false;
            result.distance = 99999;

            // Check Canvas hits (always check if enableCanvasGaze is true, or if there are canvases with DynamicObject components)
            if (enableCanvasGaze)
            {
                // Add canvases to the cache list from different behaviours
                if (canvasCacheBehaviour == CanvasCacheBehaviour.ListOfCanvases || canvasCacheBehaviour == CanvasCacheBehaviour.FindEachSceneLoad)
                {
                    // Check for null transforms in the list, indicating a change
                    if (targetCanvases.Count != cachedCanvasRectTransforms.Length)
                    {
                        // Remove null canvas from list then update cache
                        if (canvasRefreshBehaviour == CanvasRefreshBehaviour.TrimList)
                        {
                            // Remove null canvases from overrideTargetCanvases list
                            for (int i = targetCanvases.Count - 1; i >= 0; i--)
                            {
                                if (targetCanvases[i] == null)
                                {
                                    targetCanvases.RemoveAt(i);
                                }
                            }
                            RefreshCanvasTransforms(targetCanvases);
                        }
                        else if (canvasRefreshBehaviour == CanvasRefreshBehaviour.FindObjects)
                        {
                            // Find objects in the scene
                            var canvases = FindObjectsOfType<Canvas>();
                            targetCanvases.Clear();
                            targetCanvases.AddRange(canvases);
                            RefreshCanvasTransforms(canvases);
                        }
                    }
                }
                else if (canvasCacheBehaviour == CanvasCacheBehaviour.FindObjectsAlways)
                {
                    // Find all canvases and update cache of rect transforms if different
                    var canvases = FindObjectsOfType<Canvas>();
                    if (canvases.Length != cachedCanvasRectTransforms.Length)
                    {
                        targetCanvases.Clear();
                        targetCanvases.AddRange(canvases);
                        RefreshCanvasTransforms(canvases);
                    }
                }

                // Check raycast hits on each canvas
                for (int i = 0; i < cachedCanvasRectTransforms.Length; i++)
                {
                    if (targetCanvases[i].enabled == false || targetCanvases[i].gameObject.activeInHierarchy == false) { continue; }

                    float tempDistance;
                    bool didHitCanvas = CheckCanvasHit(position, forward, cachedCanvasRectTransforms[i], out tempDistance);
                    if (didHitCanvas && tempDistance < result.distance)
                    {
                        result.distance = tempDistance;
                        result.rectTransform = cachedCanvasRectTransforms[i];
                        result.worldPosition = position + forward * tempDistance;
                        result.localPosition = result.rectTransform.InverseTransformPoint(result.worldPosition);
                        result.dynamicObject = result.rectTransform.GetComponent<DynamicObject>();
                        result.isUIImageDynamic = false;
                        result.didHit = true;
                    }
                }
            }
            else
            {
                // Even if enableCanvasGaze is false, we still need to check canvases with DynamicObject components
                // This ensures that Canvas DynamicObjects are always tracked for gaze
                for (int i = 0; i < canvasDynamics.Count; i++)
                {
                    if (i >= canvasDynamicRectTransforms.Count) { break; }
                    if (canvasDynamics[i] == null || canvasDynamicRectTransforms[i] == null) { continue; }
                    if (!canvasDynamics[i].gameObject.activeInHierarchy) { continue; }

                    float tempDistance;
                    bool didHitCanvas = CheckCanvasHit(position, forward, canvasDynamicRectTransforms[i], out tempDistance);
                    if (didHitCanvas && tempDistance < result.distance)
                    {
                        result.distance = tempDistance;
                        result.rectTransform = canvasDynamicRectTransforms[i];
                        result.worldPosition = position + forward * tempDistance;
                        result.localPosition = result.rectTransform.InverseTransformPoint(result.worldPosition);
                        result.dynamicObject = canvasDynamics[i];
                        result.isUIImageDynamic = false;
                        result.didHit = true;
                    }
                }
            }

            // Check UI Image DynamicObject hits if enabled
            if (uiImageDynamics.Count > 0)
            {
                for (int i = 0; i < uiImageDynamics.Count; i++)
                {
                    if (i >= uiImageRectTransforms.Count) { break; }
                    if (uiImageDynamics[i] == null || uiImageRectTransforms[i] == null) { continue; }
                    if (!uiImageDynamics[i].gameObject.activeInHierarchy) { continue; }

                    float tempDistance;
                    bool didHit = CheckCanvasHit(position, forward, uiImageRectTransforms[i], out tempDistance);
                    if (didHit && tempDistance < result.distance)
                    {
                        result.distance = tempDistance;
                        result.rectTransform = uiImageRectTransforms[i];
                        result.dynamicObject = uiImageDynamics[i];
                        result.worldPosition = position + forward * tempDistance;
                        result.localPosition = result.rectTransform.InverseTransformPoint(result.worldPosition);
                        result.isUIImageDynamic = true;
                        result.didHit = true;
                    }
                }
            }

            // Draw debug line if we hit something
            if (result.didHit && DrawDebugLines)
            {
                Debug.DrawLine(position, result.worldPosition, Color.green);
            }

            return result;
        }


        /// <summary>
        /// Register a UI Image DynamicObject for gaze tracking (called from DynamicObject.OnEnable)
        /// </summary>
        public static void RegisterUIDynamic(DynamicObject dynamicObject, RectTransform rectTransform, List<DynamicObject> dynamics, List<RectTransform> rectTransforms)
        {
            if (rectTransform == null || dynamicObject == null) { return; }

            if (!dynamics.Contains(dynamicObject))
            {
                dynamics.Add(dynamicObject);
                rectTransforms.Add(rectTransform);
            }
        }
        
        /// <summary>
        /// Unregister a UI Image DynamicObject from gaze tracking (called from DynamicObject.OnDestroy)
        /// </summary>
        public static void UnregisterUIDynamic(DynamicObject dynamicObject, List<DynamicObject> dynamics, List<RectTransform> rectTransforms)
        {
            if (dynamics.Count <= 0) { return; }

            int index = dynamics.IndexOf(dynamicObject);
            if (index >= 0)
            {
                dynamics.RemoveAt(index);
                rectTransforms.RemoveAt(index);
            }
        }

        /// <summary>
        /// Refresh the list of UI DynamicObjects (UI Images and Canvases) in the scene
        /// </summary>
        void RefreshUIDynamics()
        {
            uiImageDynamics.Clear();
            uiImageRectTransforms.Clear();
            canvasDynamics.Clear();
            canvasDynamicRectTransforms.Clear();

            var allDynamics = FindObjectsOfType<DynamicObject>();
            foreach (var dynamic in allDynamics)
            {
                var rectTransform = dynamic.GetComponent<RectTransform>();
                if (rectTransform == null) { continue; }

                // Check if it's a UI Image DynamicObject
                var uiImage = dynamic.GetComponent<UnityEngine.UI.Image>();
                if (uiImage != null)
                {
                    uiImageDynamics.Add(dynamic);
                    uiImageRectTransforms.Add(rectTransform);
                }

                // Check if it's a Canvas DynamicObject (always tracked regardless of enableCanvasGaze)
                var canvas = dynamic.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvasDynamics.Add(dynamic);
                    canvasDynamicRectTransforms.Add(rectTransform);
                }
            }
        }

        bool CheckCanvasHit(Vector3 pos, Vector3 forward, RectTransform rt, out float hitDistance)
        {
            // Use rect.width and rect.height instead of sizeDelta to handle UI elements with stretched anchors
            // sizeDelta returns (0,0) for fully stretched UI Images/Canvases, but rect gives the actual calculated size
            var halfsize0 = rt.rect.width / 2;
            var halfsize1 = rt.rect.height / 2;

            Vector3 bottomLeft = new Vector3(-halfsize0, -halfsize1, 0);
            Vector3 bottomRight = new Vector3(halfsize0, -halfsize1, 0);
            Vector3 topLeft = new Vector3(-halfsize0, halfsize1, 0);
            Vector3 topRight = new Vector3(halfsize0, halfsize1, 0);

            //transform matrix for getting the normal
            Matrix4x4 m4 = rt.localToWorldMatrix;
            var wbottomLeft = m4.MultiplyPoint(bottomLeft);
            var wbottomRight = m4.MultiplyPoint(bottomRight);
            var wtopLeft = m4.MultiplyPoint(topLeft);

            //raycast to the surface of the canvas. need to get the distance
            float distance;
            var m_Normal = Vector3.Normalize(Vector3.Cross(wbottomRight - wbottomLeft, wtopLeft - wbottomLeft));
            var m_Distance = 0f - Vector3.Dot(m_Normal, wbottomLeft);
            bool hit = FastRaycast(pos, forward, m_Normal, m_Distance, out distance);

            Vector3 worldHitPosition;
            //hitting the plane in world space, need to convert the local hit position
            if (hit)
            {
                //world hit point to local hit point
                worldHitPosition = pos + forward * distance;
                Vector3 twoDPoint = rt.InverseTransformPoint(worldHitPosition);
                bool inPolygon = IsPointInPolygon4(bottomLeft, bottomRight, topRight, topLeft, twoDPoint);

                if (DrawDebugLines)
                {
                    Debug.DrawRay(twoDPoint, Vector3.forward, Color.blue);
                    Debug.DrawLine(bottomLeft, bottomRight, inPolygon ? Color.green : Color.red);
                    Debug.DrawLine(bottomLeft, topLeft, inPolygon ? Color.green : Color.red);
                    Debug.DrawLine(bottomRight, topRight, inPolygon ? Color.green : Color.red);
                    Debug.DrawLine(topLeft, topRight, inPolygon ? Color.green : Color.red);
                }

                if (inPolygon)
                {
                    hitDistance = distance;
                    return true;
                }
            }

            hitDistance = 0;
            return false;
        }

        //marginally faster than constructing a plane and using that
        public bool FastRaycast(Vector3 pos, Vector3 forward, Vector3 normal, float distance, out float enter)
        {
            float num = Vector3.Dot(forward, normal);
            float num2 = 0f - Vector3.Dot(pos, normal) - distance;
            // Prevent division by zero when the ray is parallel to the plane
            if (Mathf.Abs(num) < 1e-6f)
            {
                enter = 0f;
                return false;
            }
            enter = num2 / num;
            return enter > 0f;
        }

        //could be a loop, but unwrapped to avoid vector3[] garbage
        //known to always be 4 points
        private static bool IsPointInPolygon4(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 testPoint)
        {
            bool result = false;

            // Only using x and z coordinates because Unity is "y-up" and boundary is infinitely high
            if (a.y < testPoint.y && d.y >= testPoint.y || d.y < testPoint.y && a.y >= testPoint.y)
            {
                if (a.x + (testPoint.y - a.y) / (d.y - a.y) * (d.x - a.x) < testPoint.x)
                {
                    result = !result;
                }
            }

            // Only using x and z coordinates because Unity is "y-up" and boundary is infinitely high
            if (b.y < testPoint.y && a.y >= testPoint.y || a.y < testPoint.y && b.y >= testPoint.y)
            {
                if (b.x + (testPoint.y - b.y) / (a.y - b.y) * (a.x - b.x) < testPoint.x)
                {
                    result = !result;
                }
            }

            // Only using x and z coordinates because Unity is "y-up" and boundary is infinitely high
            if (c.y < testPoint.y && b.y >= testPoint.y || b.y < testPoint.y && c.y >= testPoint.y)
            {
                if (c.x + (testPoint.y - c.y) / (b.y - c.y) * (b.x - c.x) < testPoint.x)
                {
                    result = !result;
                }
            }

            // Only using x and z coordinates because Unity is "y-up" and boundary is infinitely high
            if (d.y < testPoint.y && c.y >= testPoint.y || c.y < testPoint.y && d.y >= testPoint.y)
            {
                if (d.x + (testPoint.y - d.y) / (c.y - d.y) * (c.x - d.x) < testPoint.x)
                {
                    result = !result;
                }
            }
            return result;
        }
    }
}