using UnityEngine;
using UnityEngine.UI;

namespace Cognitive3D.Auth
{
    /// <summary>
    /// How the user interacts with the panel button in VR.
    /// </summary>
    public enum PanelInteractionMode
    {
        [Tooltip("Cognitive3D custom pointer (gaze fill or controller ray click).")]
        Cognitive3DPointer,

        [Tooltip("XR Interaction Toolkit ray interactor (requires XRI package).")]
        XRIRayInteractor
    }

    /// <summary>
    /// How an identification panel is anchored in space when shown.
    /// </summary>
    public enum PanelAnchorMode
    {
        [Tooltip("Parent to the camera; head-locked (moves with the head).")]
        FollowCamera,

        [Tooltip("Placed in front of the HMD once when shown, then left world-fixed.")]
        PlayerRelative,

        [Tooltip("Left at its authored scene/prefab position; not repositioned.")]
        WorldSpace
    }

    /// <summary>
    /// Soft-follow options for PlayerRelative anchor mode, mirroring ExitPoll. All off = placed once and static
    /// </summary>
    [System.Serializable]
    public class PanelFollowSettings
    {
        [Tooltip("Panel follows the player's positional movement (walking), not head rotation.")]
        public bool stickWindow = false;

        [Tooltip("Keep the panel level with the head (locks its Y to the HMD's Y).")]
        public bool lockYPosition = true;

        [Tooltip("Re-center the panel in front of the user when they turn away, and clamp its distance.")]
        public bool rotateToStayOnScreen = true;

        [Tooltip("Preferred distance from the player (upper bound of the distance clamp).")]
        public float displayDistance = 3f;

        [Tooltip("Minimum distance from the player (lower bound of the distance clamp).")]
        public float minimumDisplayDistance = 0.2f;
    }

    /// <summary>
    /// Placement options for WorldSpace anchor mode, mirroring ExitPoll. All off = keeps the
    /// authored transform; enable overrides to set an explicit world pose and/or attach to a transform
    /// </summary>
    [System.Serializable]
    public class PanelWorldSpaceSettings
    {
        [Tooltip("Place the panel at Override Position (world) instead of its authored position.")]
        public bool useOverridePosition = false;
        public Vector3 overridePosition;

        [Tooltip("Rotate the panel to Override Rotation (world euler degrees) instead of its authored rotation.")]
        public bool useOverrideRotation = false;
        public Vector3 overrideRotationEuler;

        [Tooltip("Parent the panel to this transform after placing it (keeps its world pose).")]
        public bool useAttachTransform = false;
        public Transform attachTransform;
    }

    /// <summary>
    /// Pointer configuration for the Cognitive3D custom pointer mode. HMD vs. controller is
    /// inferred from the components on the assigned prefab.
    /// </summary>
    [System.Serializable]
    public class C3DPointerSettings
    {
        public GameObject PointerPrefab;
        public ExitPollManager.PointerInputButton PointerActivationButton = ExitPollManager.PointerInputButton.Trigger;
        public float PointerLineWidth = 0.01f;
        public Gradient PointerGradient = new Gradient
        {
            colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.286f, 0.106f, 0.631f, 1f), 0f),
                new GradientColorKey(new Color(0.055f, 0.416f, 0.624f, 1f), 0.5f),
                new GradientColorKey(new Color(0.039f, 0.557f, 0.259f, 1f), 1f)
            }
        };
        public Vector3 PointerPositionOffset;
        public Vector3 PointerRotationOffset;
    }

    /// <summary>
    /// Abstract base for all identification panel prefabs. Handles shared logic: setting the
    /// participant ID via the SDK, firing confirmation events, and managing panel visibility.
    /// </summary>
    public abstract class IdentificationPanelBase : MonoBehaviour
    {
        [Header("Base Settings")]
        [Tooltip("Automatically call Activate() when the panel is enabled.")]
        [SerializeField] protected bool activateOnEnable = true;

        [Tooltip("Set the Cognitive3D participant ID when the user confirms.")]
        [SerializeField] protected bool setParticipantIdOnConfirm = true;

        [Tooltip("Deactivate the panel GameObject after confirmation.")]
        [SerializeField] protected bool deactivateOnConfirm = true;

        [Header("VR Interaction")]
        [Tooltip("How the user interacts with the panel button in VR.")]
        [SerializeField] protected PanelInteractionMode interactionMode = PanelInteractionMode.Cognitive3DPointer;

        [Tooltip("Cognitive3D pointer configuration. Used when Interaction Mode is Cognitive3DPointer.")]
        [SerializeField] protected C3DPointerSettings pointerSettings = new C3DPointerSettings();

        [Header("Panel Anchoring")]
        [Tooltip("How the panel is positioned when shown (drives the PanelCameraFollower on this panel):\n" +
                 "Follow Camera: head-locked (parents to the camera).\n" +
                 "Player Relative: placed in front of the HMD once, then stays put.\n" +
                 "World Space: left where you placed it in the scene.")]
        [SerializeField] protected PanelAnchorMode anchorMode = PanelAnchorMode.FollowCamera;

        [Tooltip("Distance in front of the camera/HMD for Follow Camera and Player Relative modes.")]
        [SerializeField] protected float anchorDistance = 1f;

        [Tooltip("Optional camera/HMD transform override. Defaults to Camera.main.")]
        [SerializeField] protected Transform anchorCameraOverride;

        [Tooltip("Soft-follow options used when Anchor Mode is Player Relative (mirrors ExitPoll). " +
                 "Ignored for Follow Camera and World Space.")]
        [SerializeField] protected PanelFollowSettings playerRelativeFollow = new PanelFollowSettings();

        [Tooltip("Placement options used when Anchor Mode is World Space (mirrors ExitPoll). " +
                 "Ignored for Follow Camera and Player Relative.")]
        [SerializeField] protected PanelWorldSpaceSettings worldSpaceSettings = new PanelWorldSpaceSettings();

        /// <summary>
        /// Pointer configuration used when the Cognitive3D custom pointer mode is selected
        /// </summary>
        public C3DPointerSettings PointerSettings => pointerSettings;

        /// <summary>
        /// The identifier string that will be passed to SetParticipantId
        /// </summary>
        public abstract string ParticipantIdValue { get; }

        /// <summary>
        /// Fired when the user confirms their identity. The string parameter is the participant ID value
        /// </summary>
        public event System.Action<string> OnIdentificationConfirmed;

        /// <summary>
        /// Begin the identification flow (generate code, show UI, etc.)
        /// </summary>
        public virtual void Activate()
        {
            gameObject.SetActive(true);
            ApplyAnchoring();
        }

        /// <summary>
        /// Pushes this panel's anchor settings into the PanelCameraFollower(s) it owns
        /// </summary>
        protected void ApplyAnchoring()
        {
            var followers = GetComponentsInChildren<PanelCameraFollower>(true);
            foreach (var follower in followers)
            {
                if (follower.GetComponentInParent<IdentificationPanelBase>(true) != this)
                    continue; // governed by a nested panel
                follower.Configure(anchorMode, anchorDistance, anchorCameraOverride, playerRelativeFollow, worldSpaceSettings);
            }
        }

        /// <summary>
        /// Copies shared config, interaction mode, pointer style, anchoring, from another panel
        /// (e.g. QR spawning its PIN fallback)
        /// </summary>
        public void CopySharedSettingsFrom(IdentificationPanelBase source)
        {
            if (source == null) return;

            interactionMode = source.interactionMode;

            // Pointer style only, keep this panel's own PointerPrefab/instance.
            if (pointerSettings != null && source.pointerSettings != null)
            {
                pointerSettings.PointerActivationButton = source.pointerSettings.PointerActivationButton;
                pointerSettings.PointerLineWidth = source.pointerSettings.PointerLineWidth;
                pointerSettings.PointerGradient = source.pointerSettings.PointerGradient;
                pointerSettings.PointerPositionOffset = source.pointerSettings.PointerPositionOffset;
                pointerSettings.PointerRotationOffset = source.pointerSettings.PointerRotationOffset;
            }

            anchorMode = source.anchorMode;
            anchorDistance = source.anchorDistance;
            anchorCameraOverride = source.anchorCameraOverride;
            playerRelativeFollow = source.playerRelativeFollow;
            worldSpaceSettings = source.worldSpaceSettings;
        }

        /// <summary>
        /// Cancel or hide the identification UI
        /// </summary>
        public virtual void Deactivate()
        {
            gameObject.SetActive(false);
        }

        protected virtual void OnEnable()
        {
            if (activateOnEnable)
                Activate();
            else
                Deactivate();
        }

        /// <summary>
        /// Call this from subclasses when the user has confirmed their identity.
        /// Sets the participant ID on the SDK and fires the confirmation event.
        /// </summary>
        protected void ConfirmIdentification()
        {
            string id = ParticipantIdValue;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("[Cognitive3D Auth] Cannot confirm identification: ParticipantIdValue is empty.");
                return;
            }

            if (setParticipantIdOnConfirm)
            {
                ApplyParticipantId(id);
            }

            OnIdentificationConfirmed?.Invoke(id);

            if (deactivateOnConfirm)
                Deactivate();
        }

        private void ApplyParticipantId(string id)
        {
            if (Cognitive3D_Manager.IsInitialized)
            {
                SetIdAndProperties(id);
            }
            else
            {
                Cognitive3D_Manager.OnSessionBegin += OnDeferredSessionBegin;
                _deferredId = id;
            }
        }

        private string _deferredId;

        private void OnDeferredSessionBegin()
        {
            Cognitive3D_Manager.OnSessionBegin -= OnDeferredSessionBegin;
            if (!string.IsNullOrEmpty(_deferredId))
            {
                SetIdAndProperties(_deferredId);
                _deferredId = null;
            }
        }

        private void SetIdAndProperties(string id)
        {
            Cognitive3D_Manager.SetParticipantId(id);
            Cognitive3D_Manager.SetSessionProperty("c3d.auth.method", GetMethodName());
        }

        /// <summary>
        /// Returns a short identifier for the authentication method (e.g., "verbal_code", "pin_code")
        /// </summary>
        protected abstract string GetMethodName();

        private void OnDestroy()
        {
            Cognitive3D_Manager.OnSessionBegin -= OnDeferredSessionBegin;
        }
    }
}
