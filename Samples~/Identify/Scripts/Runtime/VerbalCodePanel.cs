using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Cognitive3D.Auth
{
    /// <summary>
    /// Displays a randomly generated code for supervised VR sessions. The participant
    /// reads it aloud to an instructor, who enters it to link the session to them.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Auth/Verbal Code Panel")]
    public class VerbalCodePanel : IdentificationPanelBase, IPanelColorScheme
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text instructionText;
        [SerializeField] private TMP_Text[] digitTexts;
        [SerializeField] private TMP_Text dashText;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;

        [Header("Theming")]
        [Tooltip("Optional color scheme. Changes apply in editor and on Activate().")]
        [SerializeField] private PanelColorScheme colorScheme;

        [Header("Additional UI References (for theming)")]
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image[] digitBoxBackgrounds;
        [SerializeField] private Outline[] digitBoxOutlines;
        [SerializeField] private Image instructionRowBackground;
        [SerializeField] private Image buttonImage;

        [Header("Verbal Code Settings")]
        [Tooltip("Number of digits in the generated code.")]
        [SerializeField] private int codeLength = 6;

        [Tooltip("Generate a new code each time the panel is activated.")]
        [SerializeField] private bool regenerateOnActivate = true;

        [Header("Events")]
        [Tooltip("Fired when the participant taps Ready, after the participant ID is set. " +
                 "Wire scene transitions or other post-identification actions here.")]
        public UnityEvent OnReady;

        private string currentCode;

        public override string ParticipantIdValue => currentCode;

        protected override string GetMethodName() => "verbal_code";

        private bool interactionConfigured;

        public override void Activate()
        {
            base.Activate();
            if (colorScheme != null)
                ApplyColorScheme(colorScheme);
            if (regenerateOnActivate || string.IsNullOrEmpty(currentCode))
                GenerateNewCode();
            SetupButton();
            ConfigureInteraction();
        }

        private void ConfigureInteraction()
        {
            if (interactionConfigured) return;
            interactionConfigured = true;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null && readyButton != null)
            {
                PanelInteractionSetup.Configure(
                    canvas,
                    readyButton.gameObject,
                    interactionMode,
                    OnReadyClicked,
                    pointerSettings,
                    transform
                );
            }
        }

        /// <summary>
        /// Applies a color scheme at runtime.
        /// </summary>
        public void ApplyColorScheme(PanelColorScheme scheme)
        {
            if (scheme == null) return;

            if (panelBackground != null) panelBackground.color = scheme.panelBackground;
            if (headerText != null) headerText.color = scheme.headerText;
            if (subtitleText != null) subtitleText.color = scheme.subtitleText;
            if (instructionText != null) instructionText.color = scheme.instructionText;
            if (dashText != null) dashText.color = scheme.dashText;
            if (instructionRowBackground != null) instructionRowBackground.color = scheme.instructionRowBackground;

            if (digitTexts != null)
            {
                foreach (var dt in digitTexts)
                    if (dt != null) dt.color = scheme.digitText;
            }

            if (digitBoxBackgrounds != null)
            {
                foreach (var db in digitBoxBackgrounds)
                    if (db != null) db.color = scheme.digitBoxBackground;
            }

            if (digitBoxOutlines != null)
            {
                foreach (var ol in digitBoxOutlines)
                    if (ol != null) ol.effectColor = scheme.digitBoxOutline;
            }

            if (buttonImage != null) buttonImage.color = scheme.buttonBackground;
            if (readyButtonText != null) readyButtonText.color = scheme.buttonText;

            if (readyButton != null)
            {
                ColorBlock cb = readyButton.colors;
                cb.highlightedColor = scheme.buttonHighlighted;
                cb.pressedColor = scheme.buttonPressed;
                cb.disabledColor = scheme.buttonDisabled;
                readyButton.colors = cb;

                var vb = readyButton.gameObject.GetComponent<VirtualButton>();
                if (vb != null)
                {
                    vb.normalColor = scheme.buttonBackground;
                    vb.hoverColor = scheme.buttonHighlighted;
                    vb.pressedColor = scheme.buttonPressed;
                    vb.disabledColor = scheme.buttonDisabled;
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (colorScheme != null)
                ApplyColorScheme(colorScheme);
        }
#endif

        /// <summary>
        /// Returns the current 6-digit code (no dash).
        /// </summary>
        public string GetCurrentCode() => currentCode;

        /// <summary>
        /// Generates and displays a new code.
        /// </summary>
        public void RegenerateCode() => GenerateNewCode();

        private void GenerateNewCode()
        {
            currentCode = VerbalCodeGenerator.GenerateCode(codeLength);
            DisplayCode(currentCode);
        }

        private void DisplayCode(string code)
        {
            if (digitTexts == null) return;
            for (int i = 0; i < digitTexts.Length && i < code.Length; i++)
            {
                if (digitTexts[i] != null)
                    digitTexts[i].text = code[i].ToString();
            }
        }

        private void SetupButton()
        {
            if (readyButton == null) return;
            readyButton.interactable = true;
            readyButton.onClick.RemoveAllListeners();
            readyButton.onClick.AddListener(OnReadyClicked);
        }

        private void OnReadyClicked()
        {
            if (readyButton != null)
                readyButton.interactable = false;
            ConfirmIdentification();
            OnReady?.Invoke();
        }
    }
}
