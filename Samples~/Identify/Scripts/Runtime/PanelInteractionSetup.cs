using UnityEngine;
using UnityEngine.UI;

#if COGNITIVE3D_XRI
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit;
#endif

namespace Cognitive3D.Auth
{
    /// <summary>
    /// Configures a panel's Canvas and button for VR interaction
    /// based on the selected PanelInteractionMode
    /// </summary>
    public static class PanelInteractionSetup
    {
        /// <summary>
        /// Swaps the canvas raycaster to match the interaction mode
        /// </summary>
        public static void ConfigureRaycaster(Canvas canvas, PanelInteractionMode mode)
        {
            if (canvas == null) return;
            GameObject go = canvas.gameObject;

            switch (mode)
            {
                case PanelInteractionMode.Cognitive3DPointer:
                    RemoveComponent<GraphicRaycaster>(go);
#if COGNITIVE3D_XRI
                    RemoveComponent<TrackedDeviceGraphicRaycaster>(go);
#endif
                    break;

                case PanelInteractionMode.XRIRayInteractor:
#if COGNITIVE3D_XRI
                    RemoveComponent<GraphicRaycaster>(go);
                    if (go.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                        go.AddComponent<TrackedDeviceGraphicRaycaster>();
#else
                    if (go.GetComponent<GraphicRaycaster>() == null)
                        go.AddComponent<GraphicRaycaster>();
#endif
                    break;
            }
        }

        /// <summary>
        /// Configures the raycaster on every Canvas in the panel hierarchy (including inactive
        /// ones), for multi-canvas prefabs rather than just the nearest canvas
        /// </summary>
        public static void ConfigureAllRaycasters(GameObject root, PanelInteractionMode mode)
        {
            if (root == null) return;

            var owner = root.GetComponent<IdentificationPanelBase>();

            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (owner != null)
                {
                    var nearestPanel = canvas.GetComponentInParent<IdentificationPanelBase>(true);
                    if (nearestPanel != null && nearestPanel != owner)
                        continue; // governed by a nested panel
                }
                ConfigureRaycaster(canvas, mode);
            }
        }

        /// <summary>
        /// Sets up the canvas raycaster and button interaction components
        /// for the chosen VR interaction mode
        /// </summary>
        public static void Configure(Canvas canvas, GameObject buttonGO, PanelInteractionMode mode, System.Action onConfirm, C3DPointerSettings pointerSettings = null, Transform panelRoot = null)
        {
            ConfigureRaycaster(canvas, mode);

            switch (mode)
            {
                case PanelInteractionMode.Cognitive3DPointer:
                    SetupC3DPointer(buttonGO, onConfirm, pointerSettings, panelRoot);
                    break;
                case PanelInteractionMode.XRIRayInteractor:
                    // Button.onClick already works via TrackedDeviceGraphicRaycaster
                    break;
            }
        }

        /// <summary>
        /// Ensures a Cognitive3D pointer is present and initialized for the panel
        /// </summary>
        public static void EnsurePointerForPanel(PanelInteractionMode mode, C3DPointerSettings pointerSettings, Transform panelRoot = null)
        {
            if (mode != PanelInteractionMode.Cognitive3DPointer) return;
            if (pointerSettings == null) return;

            GameObject pointerInstance = null;

            if (pointerSettings.PointerPrefab != null && pointerSettings.PointerPrefab.scene.IsValid())
                pointerInstance = pointerSettings.PointerPrefab;

            // Reuse a pre-placed pointer under this panel
            if (pointerInstance == null)
                pointerInstance = FindExistingPointer(panelRoot);

            // If PointerPrefab is a project asset, instantiate it
            if (pointerInstance == null)
                pointerInstance = SpawnPointerFromSettings(pointerSettings);

            if (pointerInstance == null) return;

            ApplyPointerSettings(pointerInstance, pointerSettings);
        }

        /// <summary>
        /// Configures a single button for the given interaction mode, WITHOUT touching the canvas raycaster or spawning the pointer
        /// </summary>
        public static void ConfigureButton(GameObject buttonGO, PanelInteractionMode mode, System.Action onConfirm, bool repeatable = false)
        {
            if (buttonGO == null) return;

            switch (mode)
            {
                case PanelInteractionMode.Cognitive3DPointer:
                    buttonGO.layer = LayerMask.NameToLayer("UI");

                    var vb = buttonGO.GetComponent<VirtualButton>();
                    if (vb == null)
                        vb = buttonGO.AddComponent<VirtualButton>();
                    vb.repeatable = repeatable;
                    vb.OnConfirm.RemoveAllListeners();
                    vb.OnConfirm.AddListener(() => onConfirm?.Invoke());
                    break;

                case PanelInteractionMode.XRIRayInteractor:
                    // Button.onClick already works via TrackedDeviceGraphicRaycaster.
                    break;
            }
        }

        // =============================================
        // Cognitive3D Custom Pointer
        // =============================================

        private static void SetupC3DPointer(GameObject buttonGO, System.Action onConfirm,
            C3DPointerSettings pointerSettings, Transform panelRoot)
        {
            // Set layer to UI so C3D pointer ray hits it
            buttonGO.layer = LayerMask.NameToLayer("UI");

            var vb = buttonGO.GetComponent<VirtualButton>();
            if (vb == null)
                vb = buttonGO.AddComponent<VirtualButton>();

            vb.OnConfirm.RemoveAllListeners();
            vb.OnConfirm.AddListener(() => onConfirm?.Invoke());

            // Leave the UGUI Button interactable so it keeps its color scheme; the
            // GraphicRaycaster is gone, so VirtualButton is the only interaction path
            EnsurePointerForPanel(PanelInteractionMode.Cognitive3DPointer, pointerSettings, panelRoot);
        }

        /// <summary>
        /// Looks for an already-placed pointer (e.g. a PointerController child of the panel prefab)
        /// so we can initialize it instead of spawning a duplicate
        /// </summary>
        private static GameObject FindExistingPointer(Transform panelRoot)
        {
            if (panelRoot != null)
            {
                var childVisualizer = panelRoot.GetComponentInChildren<PointerVisualizer>(true);
                if (childVisualizer != null) return childVisualizer.gameObject;

                var childHandler = panelRoot.GetComponentInChildren<PointerInputHandler>(true);
                if (childHandler != null) return childHandler.gameObject;
            }

#if UNITY_2023_1_OR_NEWER
            var sceneVisualizer = Object.FindFirstObjectByType<PointerVisualizer>(FindObjectsInactive.Include);
            if (sceneVisualizer != null) return sceneVisualizer.gameObject;
            var sceneHandler = Object.FindFirstObjectByType<PointerInputHandler>(FindObjectsInactive.Include);
            return sceneHandler != null ? sceneHandler.gameObject : null;
#else
            var sceneVisualizer = Object.FindObjectOfType<PointerVisualizer>(true);
            if (sceneVisualizer != null) return sceneVisualizer.gameObject;
            var sceneHandler = Object.FindObjectOfType<PointerInputHandler>(true);
            return sceneHandler != null ? sceneHandler.gameObject : null;
#endif
        }

        /// <summary>
        /// Instantiates the configured pointer prefab. HMD vs. controller is inferred from
        /// whether the prefab has a <see cref="PointerInputHandler"/> component
        /// </summary>
        private static GameObject SpawnPointerFromSettings(C3DPointerSettings settings)
        {
            if (settings.PointerPrefab == null)
            {
                Debug.LogWarning("[Cognitive3D Auth] Pointer Prefab is not assigned — cannot spawn fallback pointer.");
                return null;
            }

            GameObject instance = Object.Instantiate(settings.PointerPrefab);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;

            // HMD pointer — parent to HMD so it follows gaze.
            if (instance.GetComponent<PointerInputHandler>() == null)
            {
                Transform hmd = GameplayReferences.HMD;
                if (hmd != null)
                {
                    instance.transform.SetParent(hmd);
                    instance.transform.localPosition = Vector3.zero;
                    instance.transform.localRotation = Quaternion.identity;
                }
            }

            return instance;
        }

        /// <summary>
        /// Applies pointer settings to an existing pointer: builds the LineRenderer (absent at
        /// edit time), wires controller offsets, and forwards the activation button
        /// </summary>
        private static void ApplyPointerSettings(GameObject pointer, C3DPointerSettings settings)
        {
            if (pointer == null) return;

            var handler = pointer.GetComponent<PointerInputHandler>();
            if (handler != null)
            {
                GameplayReferences.PointerController = pointer;

                if (settings.PointerPositionOffset != Vector3.zero)
                    PointerInputHandler.PointerPosOffset = settings.PointerPositionOffset;
                if (settings.PointerRotationOffset != Vector3.zero)
                    PointerInputHandler.PointerRotOffset = settings.PointerRotationOffset;

                // SetPointerType is internal on PointerInputHandler — reflection forwards
                // the configured activation button from this assembly.
                var method = typeof(PointerInputHandler).GetMethod(
                    "SetPointerType",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(handler, new object[] { settings.PointerActivationButton });
            }
            else
            {
                GameplayReferences.HMDPointer = pointer;
            }

            var visualizer = pointer.GetComponent<PointerVisualizer>();
            if (visualizer != null && pointer.GetComponent<LineRenderer>() == null)
            {
                visualizer.ConstructDefaultLineRenderer(settings.PointerLineWidth, settings.PointerGradient);
            }

            // World-space UI orders by Canvas sorting order, not render queue/ZTest; panels sit
            // at ~40, so push the line above them or the panel paints over it. Pair with ZTest-Always
            var lineRenderer = pointer.GetComponent<LineRenderer>();
            if (lineRenderer != null)
                lineRenderer.sortingOrder = PointerSortingOrder;
        }

        /// <summary>
        /// Sorting order applied to the pointer line so it renders above panel canvases
        /// </summary>
        private const int PointerSortingOrder = 1000;

        // =============================================
        // Helpers
        // =============================================

        private static void RemoveComponent<T>(GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(comp);
                else
                    Object.DestroyImmediate(comp);
            }
        }
    }
}
