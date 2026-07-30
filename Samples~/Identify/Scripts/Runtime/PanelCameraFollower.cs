using UnityEngine;

namespace Cognitive3D.Auth
{
    /// <summary>
    /// Anchors an identification panel when enabled (FollowCamera head-locks, PlayerRelative
    /// places once, WorldSpace keeps authored pose). Only FollowCamera re-parents the panel.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Auth/Panel Camera Follower")]
    public class PanelCameraFollower : MonoBehaviour
    {
        [Tooltip("How the panel is anchored when enabled. On IdentificationPanelBase panels this is " +
                 "driven by the panel's Panel Anchoring settings and overridden at runtime; set it " +
                 "here only for standalone use.\n" +
                 "Follow Camera: head-locked (parents to the camera).\n" +
                 "Player Relative: placed in front of the HMD once, then stays put.\n" +
                 "World Space: left where you placed it in the scene.")]
        [SerializeField] private PanelAnchorMode anchorMode = PanelAnchorMode.FollowCamera;

        [Tooltip("Optional camera/HMD transform override. Defaults to Camera.main.")]
        [SerializeField] private Transform cameraOverride;

        [Tooltip("Distance in meters to place the panel in front of the camera/HMD " +
                 "(used by Follow Camera and Player Relative).")]
        [SerializeField] private float distanceFromCamera = 1.5f;

        [Tooltip("Player Relative soft-follow options (mirrors ExitPoll). Driven by the panel " +
                 "when governed by an IdentificationPanelBase.")]
        [SerializeField] private PanelFollowSettings followSettings = new PanelFollowSettings();

        [Tooltip("World Space placement options (mirrors ExitPoll). Driven by the panel " +
                 "when governed by an IdentificationPanelBase.")]
        [SerializeField] private PanelWorldSpaceSettings worldSpaceSettings = new PanelWorldSpaceSettings();

        // Rig position tracked for the StickWindow (walk-along) behavior
        private Vector3 lastRigPosition;

        // Authored transform captured before any re-parenting, so PlayerRelative/WorldSpace
        // can restore the hierarchy regardless of a prior FollowCamera re-parent
        private Transform initialParent;
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private bool cachedInitial;

        private void Awake()
        {
            CacheInitialTransform();
        }

        private void CacheInitialTransform()
        {
            if (cachedInitial) return;
            initialParent = transform.parent;
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            cachedInitial = true;
        }

        private void OnEnable()
        {
            ApplyAnchor();
        }

        /// <summary>
        /// Sets the anchor settings (typically from the owning IdentificationPanelBase) and
        /// applies them immediately
        /// </summary>
        public void Configure(PanelAnchorMode mode, float distance, Transform camOverride,
            PanelFollowSettings follow = null, PanelWorldSpaceSettings worldSpace = null)
        {
            anchorMode = mode;
            distanceFromCamera = distance;
            cameraOverride = camOverride;
            if (follow != null) followSettings = follow;
            if (worldSpace != null) worldSpaceSettings = worldSpace;
            ApplyAnchor();
        }

        /// <summary>
        /// Applies the configured anchoring. Public so callers can reposition on demand
        /// </summary>
        public void ApplyAnchor()
        {
            CacheInitialTransform(); // ApplyAnchor may run before Awake (e.g. via Configure)

            switch (anchorMode)
            {
                case PanelAnchorMode.FollowCamera:
                    PositionInFrontOfCamera();
                    break;
                case PanelAnchorMode.PlayerRelative:
                    PlaceInFrontOfPlayer();
                    break;
                case PanelAnchorMode.WorldSpace:
                    ApplyWorldSpace();
                    break;
            }
        }

        /// <summary>
        /// FollowCamera: parents the panel to the camera so it stays head-locked
        /// </summary>
        public void PositionInFrontOfCamera()
        {
            Transform cam = ResolveCamera();
            if (cam == null)
            {
                Debug.LogWarning("[Cognitive3D Auth] PanelCameraFollower: No camera found to follow.");
                return;
            }

            transform.SetParent(cam, false);
            transform.localPosition = new Vector3(0f, 0f, distanceFromCamera);
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// PlayerRelative: restores the authored parent, then places the panel once in front
        /// of the HMD (level, facing the user) so it stays world-fixed after it appears
        /// </summary>
        public void PlaceInFrontOfPlayer()
        {
            transform.SetParent(initialParent, false);

            Transform cam = ResolveCamera();
            if (cam == null)
            {
                Debug.LogWarning("[Cognitive3D Auth] PanelCameraFollower: No camera found to place relative to.");
                return;
            }

            // Horizontal forward so the panel sits level with the head, not tilted up/down
            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward; // looking straight up/down
            forward.Normalize();

            // Placement distance for PlayerRelative comes from the follow settings' displayDistance
            // (which is also the clamp upper bound), so there's no separate anchor distance to drift
            float placeDistance = followSettings != null ? followSettings.displayDistance : distanceFromCamera;
            transform.position = cam.position + forward * placeDistance;
            // Face the user (panel forward points away from the camera, matching FollowCamera)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position);

            lastRigPosition = cam.root != null ? cam.root.position : cam.position;
        }

        // ExitPoll-style soft-follow for PlayerRelative: keeps the panel comfortable as the user
        // moves and turns
        private void Update()
        {
            if (anchorMode != PanelAnchorMode.PlayerRelative) return;
            if (followSettings == null) return;

            Transform cam = ResolveCamera();
            if (cam == null) return;

            // StickWindow: move the panel by the rig's positional delta (walk-along), ignoring
            // head rotation
            if (followSettings.stickWindow)
            {
                Transform rig = cam.root != null ? cam.root : cam;
                if (Vector3.SqrMagnitude(lastRigPosition - rig.position) > 0.1f)
                {
                    transform.position -= lastRigPosition - rig.position;
                    lastRigPosition = rig.position;
                }
            }

            // LockYPosition: keep the panel level with the head.
            if (followSettings.lockYPosition)
            {
                Vector3 pos = transform.position;
                pos.y = cam.position.y;
                transform.position = pos;
            }

            // RotateToStayOnScreen: rotate the panel around the head to re-center it when the
            // user looks away, then clamp its distance and face it at the user
            if (followSettings.rotateToStayOnScreen)
            {
                const float maxDot = 0.9f;
                const float maxRotSpeed = 360f;

                if (followSettings.lockYPosition)
                {
                    Vector3 camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
                    Vector3 toPanel = transform.position - cam.position; toPanel.y = 0f; toPanel.Normalize();
                    float dot = Vector3.Dot(camForward, toPanel);
                    if (dot < maxDot)
                    {
                        Vector3 camRight = cam.right; camRight.y = 0f; camRight.Normalize();
                        float rotateSpeed = Mathf.Lerp(maxRotSpeed, 0f, dot);
                        if (Vector3.Dot(camRight, toPanel) < 0f) rotateSpeed *= -1f;
                        transform.RotateAround(cam.position, Vector3.down, rotateSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    Vector3 toPanel = (transform.position - cam.position).normalized;
                    float dot = Vector3.Dot(cam.forward, toPanel);
                    if (dot < maxDot)
                    {
                        Vector3 rotateAxis = Vector3.Cross(toPanel, cam.forward);
                        float rotateSpeed = Mathf.Lerp(maxRotSpeed, 0f, dot);
                        transform.RotateAround(cam.position, rotateAxis, rotateSpeed * Time.deltaTime);
                    }
                }

                // Clamp distance to the player
                float dist = Vector3.Distance(transform.position, cam.position);
                if (dist > followSettings.displayDistance)
                    transform.position = (transform.position - cam.position).normalized * followSettings.displayDistance + cam.position;
                else if (dist < followSettings.minimumDisplayDistance)
                    transform.position = (transform.position - cam.position).normalized * followSettings.minimumDisplayDistance + cam.position;

                // Face the user (inverse of looking at the HMD)
                transform.LookAt(transform.position * 2f - cam.position);
            }
        }

        /// <summary>
        /// WorldSpace: places the panel at explicit world coordinates (if overridden) or its
        /// authored transform, optionally parenting it to an attach transform (mirrors ExitPoll)
        /// </summary>
        public void ApplyWorldSpace()
        {
            var ws = worldSpaceSettings;

            // Start from the authored parent so overrides/attach apply predictably
            transform.SetParent(initialParent, false);

            // Position: explicit world override, else the authored local position
            if (ws != null && ws.useOverridePosition)
                transform.position = ws.overridePosition;
            else
                transform.localPosition = initialLocalPosition;

            // Rotation: explicit world override, else the authored local rotation
            if (ws != null && ws.useOverrideRotation)
                transform.rotation = Quaternion.Euler(ws.overrideRotationEuler);
            else
                transform.localRotation = initialLocalRotation;

            // Optionally attach to a transform, keeping the world pose we just set
            if (ws != null && ws.useAttachTransform && ws.attachTransform != null)
                transform.SetParent(ws.attachTransform, true);
        }

        private Transform ResolveCamera()
        {
            if (cameraOverride != null) return cameraOverride;
            if (GameplayReferences.HMD != null) return GameplayReferences.HMD;
            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
