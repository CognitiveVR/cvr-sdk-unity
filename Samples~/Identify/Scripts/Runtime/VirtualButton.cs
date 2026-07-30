using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Cognitive3D.Auth
{
    /// <summary>
    /// Virtual button for the Cognitive3D custom pointer, driven by PointerInputHandler
    /// (controller ray and gaze/hand pinch). Tints a target Graphic on focus state.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Auth/Virtual Button")]
    public class VirtualButton : MonoBehaviour, IPointerFocus
    {
        [Tooltip("Graphic tinted by pointer focus state. If unassigned, the sibling Button's target graphic (or any sibling Graphic) is used.")]
        [SerializeField] private Graphic targetGraphic;

        [Tooltip("Color applied when the pointer is not focused on this button.")]
        [SerializeField] public Color normalColor = Color.white;

        [Tooltip("Color applied while the pointer is focused on this button.")]
        [SerializeField] public Color hoverColor = new Color(0.78f, 0.88f, 1f, 1f);

        [Tooltip("Color applied briefly while the pointer activation button is pressed on this button.")]
        [SerializeField] public Color pressedColor = new Color(0.55f, 0.75f, 1f, 1f);

        [Tooltip("Color applied when the button is not interactable (see Interactable property).")]
        [SerializeField] public Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Tooltip("If false, the button ignores pointer focus and displays Disabled Color.")]
        [SerializeField] private bool interactable = true;

        [Tooltip("If true, the button re-arms after each confirm (once the trigger is released, or " +
                 "the gaze leaves) so it can be pressed repeatedly — e.g. a keypad digit. If false, " +
                 "it confirms once and latches (one-shot), e.g. a Continue button.")]
        [SerializeField] public bool repeatable = false;

        [Tooltip("Seconds of gaze / hand pinch dwell required before the button confirms.")]
        [SerializeField] private float fillDuration = 1f;

        [SerializeField]
        private UnityEvent m_OnConfirm = new UnityEvent();

        public UnityEvent OnConfirm
        {
            get { return m_OnConfirm; }
            set { m_OnConfirm = value; }
        }

        /// <summary>Required by IPointerFocus.</summary>
        public MonoBehaviour MonoBehaviour { get { return this; } }

        /// <summary>
        /// When false the button ignores pointer focus and shows the disabled color.
        /// Setting this at runtime immediately updates the target graphic's color.
        /// </summary>
        public bool Interactable
        {
            get { return interactable; }
            set
            {
                if (interactable == value) return;
                interactable = value;
                if (!interactable)
                {
                    focusThisFrame = false;
                    activateThisFrame = false;
                    fillAmount = 0f;
                    ApplyColor(disabledColor);
                }
                else if (!alreadyConfirmed)
                {
                    ApplyColor(normalColor);
                }
            }
        }

        private float fillAmount;
        private bool focusThisFrame;
        private bool activateThisFrame;
        private bool useSlowFill;
        private bool alreadyConfirmed;

        private void Awake()
        {
            if (m_OnConfirm == null) m_OnConfirm = new UnityEvent();
            ResolveTargetGraphic();
            ApplyColor(interactable ? normalColor : disabledColor);
        }

        private void OnEnable()
        {
            // Reset transient state so the button can be reused if re-enabled after confirmation.
            alreadyConfirmed = false;
            fillAmount = 0f;
            ApplyColor(interactable ? normalColor : disabledColor);
        }

        /// <summary>
        /// Called from PointerInputHandler. hover=true means slow fill (hand pinch);
        /// hover=false means click immediately on activation (controller trigger).
        /// </summary>
        public void SetPointerFocus(bool activation, bool hover)
        {
            // Note: we still record focus/activation while alreadyConfirmed so a repeatable
            // button can detect trigger-release and re-arm (see LateUpdate).
            if (!interactable) return;
            focusThisFrame = true;
            activateThisFrame = activation;
            useSlowFill = hover;
        }

        /// <summary>
        /// Called from HMD gaze pointers. Equivalent to a slow-fill focus.
        /// </summary>
        public void SetGazeFocus()
        {
            if (!interactable) return;
            focusThisFrame = true;
            useSlowFill = true;
        }

        /// <summary>
        /// Explicitly invoke the confirm callback. Safe to call from external code
        /// (e.g. UGUI Button onClick for the XRI path).
        /// </summary>
        public void Confirm()
        {
            if (alreadyConfirmed) return;
            alreadyConfirmed = true;
            ApplyColor(pressedColor);
            m_OnConfirm?.Invoke();
        }

        private void LateUpdate()
        {
            if (alreadyConfirmed)
            {
                // Repeatable buttons re-arm: on controller trigger release, or (for gaze/hand
                // slow-fill) once the pointer leaves so it doesn't auto-repeat while still focused.
                if (repeatable && !activateThisFrame && !(useSlowFill && focusThisFrame))
                {
                    alreadyConfirmed = false;
                    fillAmount = 0f;
                    ApplyColor(focusThisFrame && interactable ? hoverColor
                        : (interactable ? normalColor : disabledColor));
                }
                focusThisFrame = false;
                activateThisFrame = false;
                return;
            }

            if (!interactable)
            {
                ApplyColor(disabledColor);
                focusThisFrame = false;
                activateThisFrame = false;
                return;
            }

            if (focusThisFrame)
            {
                if (useSlowFill)
                {
                    fillAmount += Time.deltaTime;
                    if (fillAmount >= fillDuration)
                    {
                        Confirm();
                    }
                    else
                    {
                        ApplyColor(hoverColor);
                    }
                }
                else if (activateThisFrame)
                {
                    Confirm();
                }
                else
                {
                    ApplyColor(hoverColor);
                }
            }
            else
            {
                if (fillAmount > 0f)
                {
                    fillAmount = Mathf.Max(0f, fillAmount - Time.deltaTime);
                }
                ApplyColor(normalColor);
            }

            focusThisFrame = false;
            activateThisFrame = false;
        }

        private void ResolveTargetGraphic()
        {
            if (targetGraphic != null) return;

            var button = GetComponent<Button>();
            if (button != null && button.targetGraphic != null)
            {
                targetGraphic = button.targetGraphic;
                return;
            }

            targetGraphic = GetComponent<Graphic>();
        }

        private void ApplyColor(Color color)
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = color;
            }
        }
    }
}
