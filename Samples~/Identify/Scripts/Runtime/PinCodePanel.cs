using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Displays a numeric keypad for entering a PIN code for user identification.
    /// Supports 4 or 6 digit PINs. Each digit appears in a display box as typed.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Identify/Pin Code Panel")]
    public class PinCodePanel : IdentificationPanelBase, IPanelColorScheme
    {
        [Header("UI References")]
        [Tooltip("Container holding the PIN keypad UI (digit boxes, number pad, action buttons). " +
                 "Hidden when the confirmation panel is shown, so the confirmation — a sibling under " +
                 "this same root — stays visible instead of the whole panel being deactivated.")]
        [SerializeField] private GameObject pinView;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private Image[] digitDisplayBoxes;
        [SerializeField] private TMP_Text[] digitDisplayTexts;
        [SerializeField] private Button[] numberButtons;   // 0-9 (index = digit value)
        [SerializeField] private Button clearButton;
        [SerializeField] private Button enterButton;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text clearButtonText;
        [SerializeField] private TMP_Text enterButtonText;
        [SerializeField] private TMP_Text errorText;

        [Header("Theming")]
        [SerializeField] private PanelColorScheme colorScheme;

        [Header("Additional UI References (for theming)")]
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image[] keypadButtonImages;
        [SerializeField] private Image clearButtonImage;
        [SerializeField] private Image enterButtonImage;
        [Tooltip("Background image of the back button (tinted to the scheme's back-button background).")]
        [SerializeField] private Image backButtonImage;
        [Tooltip("Arrow icon of the back button (tinted to the scheme's back-button icon color).")]
        [SerializeField] private Image backButtonIcon;
        [SerializeField] private Outline[] digitDisplayOutlines;

        [Header("Pin Code Settings")]
        [Tooltip("Token resolver for converting pin code to participant info")]
        [SerializeField] private TokenResolver tokenResolver;
        [Tooltip("Optional confirmation panel shown after a token resolve. Populated with the " +
                 "result in code, then activated. Leave empty to rely solely on the events below.")]
        [SerializeField] private ConfirmationPanel confirmationPanel;

        [Tooltip("Number of digits required (4 or 6).")]
        [SerializeField] private int pinLength = 4;

        [Tooltip("Show the Back button. Off by default (standalone use). QRCodePanel turns this " +
                 "on when it spawns the PIN panel as its fallback, so the user can return to QR.")]
        [SerializeField] private bool showBackButton = false;

        [Header("Events")]
        [Tooltip("Fired when the entered PIN resolves successfully. Wire scene transitions or other success actions here.")]
        public UnityEvent OnPinSuccess;

        [Tooltip("Fired when the entered PIN fails to resolve. The panel stays visible so the user can retry.")]
        public UnityEvent OnPinFailure;
        public UnityEvent OnBackRequested;

        private string currentPin = "";
        private string resolvedParticipantId;
        private bool interactionConfigured;
        private bool isErrorState;

        public override string ParticipantIdValue => resolvedParticipantId;

        protected override string GetMethodName() => "pin_code";

        public override void Activate()
        {
            base.Activate();
            // Reset to the keypad view: show the keypad, hide any prior confirmation.
            if (pinView != null) pinView.SetActive(true);
            if (confirmationPanel != null) confirmationPanel.Hide();

            ClearPin();
            SetupButtons();

            if (colorScheme != null) ApplyColorScheme(colorScheme);

            ConfigureInteraction();
        }

        /// <summary>
        /// Hides only the keypad UI (pinView) so a sibling confirmation panel can still display.
        /// Falls back to disabling the whole GameObject if no pinView is assigned.
        /// </summary>
        public override void Deactivate()
        {
            if (pinView != null)
                pinView.SetActive(false);
            else
                base.Deactivate();
        }

        // =============================================
        // Pin Code Callbacks
        // =============================================

        private void OnTokenDecoded(string token)
        {
            if (tokenResolver != null)
            {
                tokenResolver.ResolveToken(token, OnTokenResolved);
            }
            else
            {
                Debug.LogWarning("[COGNITIVE3D] TokenResolver not assigned");
            }
        }

        private void OnTokenResolved(TokenResult result)
        {
            if (result == null || !result.Success)
            {
                string error = result?.ErrorMessage ?? "Incorrect PIN. Please try again.";
                Debug.LogWarning("[COGNITIVE3D] Token resolution failed: " + error);

                if (confirmationPanel != null)
                {
                    confirmationPanel.PopulateFailure(error);
                }

                ShowError(error);
                OnPinFailure?.Invoke();
                return;
            }

            resolvedParticipantId = result.ParticipantId;

            if (!string.IsNullOrEmpty(result.ParticipantId))
            {
                Cognitive3D.Cognitive3D_Manager.SetParticipantId(result.ParticipantId);
                if (!string.IsNullOrEmpty(result.ParticipantName))
                    Cognitive3D.Cognitive3D_Manager.SetParticipantFullName(result.ParticipantName);
                if (!string.IsNullOrEmpty(result.ParticipantEmail))
                    Cognitive3D.Cognitive3D_Manager.SetParticipantProperty("email", result.ParticipantEmail);
                Cognitive3D.Cognitive3D_Manager.SetSessionProperty("c3d.identify.method", "pin_code");
            }

            ClearError();

            // Hide the keypad but keep this root active so the sibling confirmation panel can
            // display. ConfirmIdentification() below routes through Deactivate(), which hides only pinView.
            if (pinView != null) pinView.SetActive(false);

            if (confirmationPanel != null)
            {
                confirmationPanel.PopulateSuccess(result);
            }

            ConfirmIdentification();
            OnPinSuccess?.Invoke();
        }

        // =============================================
        // Button Setup
        // =============================================

        private void SetupButtons()
        {
            // Each button is wired twice, UGUI onClick and a VirtualButton.OnConfirm, so only
            // the one matching the active interaction mode fires. Enter/Back re-arm harmlessly to allow retries

            // Number buttons 0-9
            if (numberButtons != null)
            {
                for (int i = 0; i < numberButtons.Length; i++)
                {
                    if (numberButtons[i] == null) continue;
                    int digit = i;
                    numberButtons[i].onClick.RemoveAllListeners();
                    numberButtons[i].onClick.AddListener(() => OnDigitPressed(digit));
                    PanelInteractionSetup.ConfigureButton(
                        numberButtons[i].gameObject, interactionMode, () => OnDigitPressed(digit), repeatable: true);
                }
            }

            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(ClearPin);
                PanelInteractionSetup.ConfigureButton(
                    clearButton.gameObject, interactionMode, ClearPin, repeatable: true);
            }

            if (enterButton != null)
            {
                enterButton.onClick.RemoveAllListeners();
                enterButton.onClick.AddListener(OnEnterPressed);
                PanelInteractionSetup.ConfigureButton(
                    enterButton.gameObject, interactionMode, OnEnterPressed, repeatable: true);
                SetButtonInteractable(enterButton, false);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => OnBackRequested?.Invoke());
                PanelInteractionSetup.ConfigureButton(
                    backButton.gameObject, interactionMode, () => OnBackRequested?.Invoke(), repeatable: true);
                backButton.gameObject.SetActive(showBackButton);
            }
        }

        /// <summary>
        /// Shows or hides the Back button. Re-applied on every Activate() via SetupButtons()
        /// </summary>
        public void SetBackButtonVisible(bool visible)
        {
            showBackButton = visible;
            if (backButton != null)
                backButton.gameObject.SetActive(visible);
        }

        // =============================================
        // Input Handling
        // =============================================

        public void OnDigitPressed(int digit)
        {
            // If in error state, clear everything and start fresh
            if (isErrorState)
            {
                currentPin = "";
                ClearError();
            }

            if (currentPin.Length >= pinLength) return;

            currentPin += digit.ToString();
            UpdateDisplay();

            SetButtonInteractable(enterButton, currentPin.Length == pinLength);

            // Force canvas update to ensure text renders immediately
            Canvas.ForceUpdateCanvases();
        }

        public void ClearPin()
        {
            currentPin = "";
            UpdateDisplay();

            SetButtonInteractable(enterButton, false);

            Canvas.ForceUpdateCanvases();
        }

        public void OnEnterPressed()
        {
            if (currentPin.Length != pinLength) return;

            ClearError();
            SetKeypadInteractable(false);

            if (tokenResolver != null)
            {
                OnTokenDecoded(currentPin);
            }
            else
            {
                // No resolver, use raw PIN as participant ID
                resolvedParticipantId = currentPin;
                ConfirmIdentification();
            }
        }

        private void SetKeypadInteractable(bool interactable)
        {
            if (numberButtons != null)
                foreach (var btn in numberButtons)
                    SetButtonInteractable(btn, interactable);
            SetButtonInteractable(clearButton, interactable);
            SetButtonInteractable(enterButton, interactable);
        }

        /// <summary>
        /// Sets interactability on both the UGUI Button and its VirtualButton so the button is
        /// gated consistently regardless of the active interaction mode
        /// </summary>
        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button == null) return;
            button.interactable = interactable;

            var vb = button.GetComponent<VirtualButton>();
            if (vb != null) vb.Interactable = interactable;
        }

        // =============================================
        // Display
        // =============================================

        private void UpdateDisplay()
        {
            if (digitDisplayTexts == null) return;

            // Build list of active display indices (skip disabled GameObjects)
            int pinIdx = 0;
            for (int i = 0; i < digitDisplayTexts.Length; i++)
            {
                if (digitDisplayTexts[i] == null) continue;
                if (!digitDisplayTexts[i].gameObject.activeInHierarchy) continue;

                if (pinIdx < currentPin.Length)
                    digitDisplayTexts[i].text = currentPin[pinIdx].ToString();
                else
                    digitDisplayTexts[i].text = "";
                pinIdx++;
            }

            // Visual feedback: highlight the active box
            if (digitDisplayBoxes != null && colorScheme != null)
            {
                int boxIdx = 0;
                for (int i = 0; i < digitDisplayBoxes.Length; i++)
                {
                    if (digitDisplayBoxes[i] == null) continue;
                    if (!digitDisplayBoxes[i].gameObject.activeInHierarchy) continue;

                    if (boxIdx == currentPin.Length && currentPin.Length < pinLength)
                        digitDisplayBoxes[i].color = colorScheme.digitBoxBackground * 1.3f;
                    else
                        digitDisplayBoxes[i].color = colorScheme.digitBoxBackground;
                    boxIdx++;
                }
            }

            if (digitDisplayOutlines != null)
            {
                int olIdx = 0;
                for (int i = 0; i < digitDisplayOutlines.Length; i++)
                {
                    if (digitDisplayOutlines[i] == null) continue;
                    if (!digitDisplayOutlines[i].gameObject.activeInHierarchy) continue;

                    if (olIdx == currentPin.Length && currentPin.Length < pinLength)
                        digitDisplayOutlines[i].effectColor = new Color32(80, 160, 255, 255);
                    else if (colorScheme != null)
                        digitDisplayOutlines[i].effectColor = colorScheme.digitBoxOutline;
                    olIdx++;
                }
            }
        }

        // =============================================
        // Error State
        // =============================================

        private void ShowError(string message)
        {
            isErrorState = true;

            if (errorText != null)
            {
                errorText.gameObject.SetActive(true);
                errorText.text = message;
                if (colorScheme != null)
                    errorText.color = colorScheme.errorText;
            }

            if (digitDisplayBoxes != null && colorScheme != null)
            {
                for (int i = 0; i < digitDisplayBoxes.Length; i++)
                {
                    if (digitDisplayBoxes[i] == null) continue;
                    if (!digitDisplayBoxes[i].gameObject.activeInHierarchy) continue;

                    digitDisplayBoxes[i].color = colorScheme.errorBoxBackground;
                }
            }

            if (digitDisplayOutlines != null && colorScheme != null)
            {
                for (int i = 0; i < digitDisplayOutlines.Length; i++)
                {
                    if (digitDisplayOutlines[i] == null) continue;
                    if (!digitDisplayOutlines[i].gameObject.activeInHierarchy) continue;

                    digitDisplayOutlines[i].effectColor = colorScheme.errorBoxOutline;
                }
            }

            if (digitDisplayTexts != null && colorScheme != null)
            {
                for (int i = 0; i < digitDisplayTexts.Length; i++)
                {
                    if (digitDisplayTexts[i] == null) continue;
                    if (!digitDisplayTexts[i].gameObject.activeInHierarchy) continue;

                    if (!string.IsNullOrEmpty(digitDisplayTexts[i].text))
                    {
                        digitDisplayTexts[i].text = "\u25CF"; // ● dot
                        digitDisplayTexts[i].color = colorScheme.errorDotColor;
                    }
                }
            }

            // Re-enable keypad for retry
            SetKeypadInteractable(true);
            SetButtonInteractable(enterButton, false);
        }

        private void ClearError()
        {
            if (!isErrorState) return;
            isErrorState = false;

            if (errorText != null)
                errorText.gameObject.SetActive(false);

            if (colorScheme != null)
                ApplyColorScheme(colorScheme);
        }

        // =============================================
        // Interaction
        // =============================================

        private void ConfigureInteraction()
        {
            if (interactionConfigured) return;
            interactionConfigured = true;

            // Configure the raycaster on EVERY canvas this panel owns so a runtime-set interaction
            // mode (e.g. inherited from QR) is honored across all of them, not just the first
            PanelInteractionSetup.ConfigureAllRaycasters(gameObject, interactionMode);
            PanelInteractionSetup.EnsurePointerForPanel(interactionMode, pointerSettings, transform);
        }

        // =============================================
        // Public API
        // =============================================

        public string GetCurrentPin() => currentPin;
        public int PinLength => pinLength;

        /// <summary>
        /// The token resolver used to validate the entered PIN. Settable so a spawning
        /// panel (e.g. QRCodePanel) can share its resolver configuration
        /// </summary>
        public TokenResolver TokenResolver { get => tokenResolver; set => tokenResolver = value; }

        /// <summary>The confirmation panel shown after a successful PIN. Exposed so a spawning
        /// panel can forward its Continue action.</summary>
        public ConfirmationPanel ConfirmationPanel => confirmationPanel;

        // =============================================
        // Theming
        // =============================================

        public void ApplyColorScheme(PanelColorScheme scheme)
        {
            if (scheme == null) return;

            if (panelBackground != null) panelBackground.color = scheme.panelBackground;
            if (headerText != null) headerText.color = scheme.headerText;
            if (subtitleText != null) subtitleText.color = scheme.subtitleText;

            if (digitDisplayBoxes != null)
                foreach (var img in digitDisplayBoxes)
                    if (img != null) img.color = scheme.digitBoxBackground;

            if (digitDisplayOutlines != null)
                foreach (var ol in digitDisplayOutlines)
                    if (ol != null) ol.effectColor = scheme.digitBoxOutline;

            if (digitDisplayTexts != null)
                foreach (var txt in digitDisplayTexts)
                    if (txt != null) txt.color = scheme.digitText;

            if (keypadButtonImages != null)
                foreach (var img in keypadButtonImages)
                    if (img != null) img.color = scheme.digitBoxBackground;

            if (clearButtonImage != null) clearButtonImage.color = scheme.instructionRowBackground;
            if (clearButtonText != null) clearButtonText.color = scheme.subtitleText;
            if (enterButtonImage != null) enterButtonImage.color = scheme.buttonBackground;
            if (enterButtonText != null) enterButtonText.color = scheme.buttonText;

            // Number keypad buttons, digit box colors as the resting tint.
            if (numberButtons != null)
            {
                foreach (var btn in numberButtons)
                    ApplyButtonColors(btn, scheme.digitBoxBackground, scheme);
            }

            // Clear button, blends with the instruction row.
            ApplyButtonColors(clearButton, scheme.instructionRowBackground, scheme);

            // Enter button, primary action color.
            ApplyButtonColors(enterButton, scheme.buttonBackground, scheme);

            // Back button, subtle ghost styling. ColorBlock + VirtualButton are synced so it
            // themes and gives hover/press feedback across every interaction mode.
            if (backButtonImage != null) backButtonImage.color = scheme.backButtonBackground;
            if (backButtonIcon != null) backButtonIcon.color = scheme.backButtonIcon;
            ApplyButtonColors(backButton, scheme.backButtonBackground, scheme);
        }

        /// <summary>
        /// Syncs both the UGUI Button ColorBlock and any sibling VirtualButton colors so the
        /// button looks consistent across XRI-pointer and Cognitive3D-pointer interaction modes.
        /// </summary>
        private static void ApplyButtonColors(Button button, Color normal, PanelColorScheme scheme)
        {
            if (button == null) return;

            ColorBlock cb = button.colors;
            cb.normalColor = normal;
            cb.highlightedColor = scheme.buttonHighlighted;
            cb.pressedColor = scheme.buttonPressed;
            cb.disabledColor = scheme.buttonDisabled;
            button.colors = cb;

            var vb = button.GetComponent<VirtualButton>();
            if (vb != null)
            {
                vb.normalColor = normal;
                vb.hoverColor = scheme.buttonHighlighted;
                vb.pressedColor = scheme.buttonPressed;
                vb.disabledColor = scheme.buttonDisabled;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (colorScheme != null)
                ApplyColorScheme(colorScheme);
        }
#endif
    }
}
