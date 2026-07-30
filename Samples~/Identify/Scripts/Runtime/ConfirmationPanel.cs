using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Cognitive3D.Auth
{
    /// <summary>
    /// Confirmation screen shown after a token resolve; fires OnContinueSuccess/OnContinueFailure
    /// when the user taps Continue.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Auth/Confirmation Panel")]
    public class ConfirmationPanel : MonoBehaviour, IPanelColorScheme
    {
        [Header("UI References")]
        [SerializeField] private Image panelBackground;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image infoBoxBackground;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text emailText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Image continueButtonImage;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_Text continueButtonText;

        [Header("Theming")]
        [SerializeField] private PanelColorScheme colorScheme;

        [Header("VR Interaction")]
        [Tooltip("How the user taps Continue in VR. Should match the parent panel's mode. " +
                 "Cognitive3DPointer ensures a collider + VirtualButton on the Continue button.")]
        [SerializeField] private PanelInteractionMode interactionMode = PanelInteractionMode.Cognitive3DPointer;

        [Header("Messages")]
        [SerializeField] private string successHeader = "Identified!";
        [SerializeField] private string successSubtitle = "Welcome back";
        [SerializeField] private string failureHeader = "Scan Failed";
        [SerializeField] private string failureSubtitle = "Please try again";
        [SerializeField] private string continueButtonLabel = "Continue";

        [Header("Events")]
        [Tooltip("Fired when the user taps Continue on a successful identification.")]
        public UnityEvent OnContinueSuccess;

        [Tooltip("Fired when the user taps Continue on a failed scan. Wire this to reset the scanner.")]
        public UnityEvent OnContinueFailure;

        private bool wasSuccess;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked); // UGUI path (XRI / touch)

                // C3D-pointer path: adds a VirtualButton on Continue if missing
                PanelInteractionSetup.ConfigureButton(
                    continueButton.gameObject, interactionMode, OnContinueClicked, repeatable: false);
            }
        }

        private void OnEnable()
        {
            Debug.LogError("[Cognitive3D Auth] QRConfirmationPanel: Showing confirmation panel.");
        }

        /// <summary>
        /// Populates the success content. Does NOT activate the GameObject, enable it after
        /// </summary>
        public void PopulateSuccess(TokenResult result)
        {
            wasSuccess = true;
            if (headerText != null) headerText.text = successHeader;
            if (subtitleText != null) subtitleText.text = successSubtitle;
            if (nameText != null)
            {
                nameText.gameObject.SetActive(true);
                nameText.text = !string.IsNullOrEmpty(result?.ParticipantName) ? result.ParticipantName : "(no name)";
            }
            if (emailText != null)
            {
                emailText.gameObject.SetActive(true);
                emailText.text = !string.IsNullOrEmpty(result?.ParticipantEmail) ? result.ParticipantEmail : (result?.ParticipantId ?? "");
            }
            if (errorText != null) errorText.gameObject.SetActive(false);
            if (continueButtonText != null) continueButtonText.text = continueButtonLabel;
            ApplyColorScheme();
        }

        /// <summary>
        /// Populates the failure content. Does NOT activate the GameObject, enable it after
        /// </summary>
        public void PopulateFailure(string error)
        {
            wasSuccess = false;
            if (headerText != null) headerText.text = failureHeader;
            if (subtitleText != null) subtitleText.text = failureSubtitle;
            if (nameText != null) nameText.gameObject.SetActive(false);
            if (emailText != null) emailText.gameObject.SetActive(false);
            if (errorText != null)
            {
                errorText.gameObject.SetActive(true);
                errorText.text = string.IsNullOrEmpty(error) ? "Unknown error" : error;
            }
            if (continueButtonText != null) continueButtonText.text = continueButtonLabel;
            ApplyColorScheme();
        }

        /// <summary>
        /// Activates the panel for a success result (populate first)
        /// </summary>
        public void ShowSuccess() => gameObject.SetActive(true);

        /// <summary>
        /// Activates the panel for a failure result (populate first)
        /// </summary>
        public void ShowFailure() => gameObject.SetActive(true);

        public void Hide() => gameObject.SetActive(false);

        private void OnContinueClicked()
        {
            if (wasSuccess)
            {
                OnContinueSuccess?.Invoke();
            }
            else
            {
                OnContinueFailure?.Invoke();
                Hide();
            }
        }

        /// <summary>
        /// Adopts and applies the given scheme (IPanelColorScheme)
        /// </summary>
        public void ApplyColorScheme(PanelColorScheme scheme)
        {
            colorScheme = scheme;
            ApplyColorScheme();
        }

        private void ApplyColorScheme()
        {
            if (colorScheme == null) return;
            if (panelBackground != null) panelBackground.color = colorScheme.panelBackground;
            if (headerText != null) headerText.color = colorScheme.headerText;
            if (subtitleText != null) subtitleText.color = colorScheme.subtitleText;
            if (infoBoxBackground != null) infoBoxBackground.color = colorScheme.instructionRowBackground;
            if (nameText != null) nameText.color = colorScheme.digitText;
            if (emailText != null) emailText.color = colorScheme.subtitleText;
            if (errorText != null) errorText.color = colorScheme.errorText;
            if (continueButtonImage != null) continueButtonImage.color = colorScheme.buttonBackground;
            if (continueButtonText != null) continueButtonText.color = colorScheme.buttonText;
            if (continueButton != null)
            {
                ColorBlock cb = continueButton.colors;
                cb.normalColor = Color.white;
                cb.highlightedColor = colorScheme.buttonHighlighted;
                cb.pressedColor = colorScheme.buttonPressed;
                cb.disabledColor = colorScheme.buttonDisabled;
                continueButton.colors = cb;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            ApplyColorScheme();
        }
#endif
    }
}
