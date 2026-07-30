using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Displays a QR code scanning panel that scans a token via the headset camera
    /// and resolves it into participant info via TokenResolver
    /// </summary>
    [AddComponentMenu("Cognitive3D/Identify/QR Code Panel")]
    public class QRCodePanel : IdentificationPanelBase, IPanelColorScheme
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text statusBarText;
        [SerializeField] private Button fallbackButton;
        [SerializeField] private TMP_Text fallbackButtonText;
        [SerializeField] private GameObject scanningIndicator;
        [Tooltip("Container holding the QR-only UI (camera feed, status, fallback button). Toggled when switching to PIN.")]
        [SerializeField] private GameObject qrView;

        [Header("Theming")]
        [SerializeField] private PanelColorScheme colorScheme;
        [Header("Additional UI References (for theming)")]
        [SerializeField] private Image panelBackground;
        [SerializeField] private Image statusBarBackground;
        [Tooltip("Inner (hollow) fill of the ghost fallback button. Tinted to the panel background.")]
        [SerializeField] private Image fallbackButtonImage;
        [Tooltip("Outer ring of the ghost fallback button. Tinted to the accent outline color.")]
        [SerializeField] private Outline fallbackButtonOutline;

        [Header("QR Code Settings")]
        [Tooltip("Token resolver for converting QR tokens to participant info.")]
        [SerializeField] private TokenResolver tokenResolver;
        [Tooltip("Optional confirmation panel shown after a token resolve. Populated with the " +
                 "result in code, then activated. Leave empty to rely solely on the events below.")]
        [SerializeField] private ConfirmationPanel confirmationPanel;
        [Tooltip("Automatically stop scanning after the first successful decode.")]
        [SerializeField] private bool stopOnFirstDecode = true;

        [Header("Fallback Panel")]
        [Tooltip("Prefab spawned when the user taps the fallback button (e.g. the PIN panel). " +
                 "Instantiated once at runtime and reused for the rest of the session.")]
        [SerializeField] private IdentificationPanelBase fallbackPanelPrefab;

        // Runtime instance of the fallback prefab (spawned lazily on first use).
        private IdentificationPanelBase fallbackInstance;

        [Header("Events")]
        [Tooltip("Fired when the scanned QR resolves successfully. Wire your confirmation UI here (enable).")]
        public UnityEvent OnQRSuccess;
        [Tooltip("Fired when the scanned QR fails to resolve. The panel resumes scanning so the user can retry.")]
        public UnityEvent OnQRFailure;

        private QRCodeScanner scanner;
        private string resolvedParticipantId;
        private bool interactionConfigured;

        public override string ParticipantIdValue => resolvedParticipantId;

        protected override string GetMethodName() => "qr_code";

        public override void Activate()
        {
            base.Activate();

            if (colorScheme != null) ApplyColorScheme(colorScheme);

            // Reset to the scan view: show the QR view, restore the scan chrome, hide any prior confirmation
            if (qrView != null) qrView.SetActive(true);
            SetScanViewActive(true);
            if (confirmationPanel != null) confirmationPanel.Hide();
            if (fallbackInstance != null) fallbackInstance.gameObject.SetActive(false);

            if (!IsScanningSupported())
            {
                ShowFallback();
                return;
            }

            SetupScanner();
            UpdateStatus("Searching for QR code...", "Passthrough camera active \u2014 scanning...");
            SetScanState(ScanState.Searching);
            scanner.StartScanning();
            ConfigureInteraction();
        }

        public override void Deactivate()
        {
            if (scanner != null && scanner.IsScanning)
                scanner.StopScanning();

            // Hide only the QR view (not the whole GameObject) so a sibling confirmation panel
            // stays visible after ConfirmIdentification; falls back to base.Deactivate() if unassigned
            if (qrView != null)
                qrView.SetActive(false);
            else
                base.Deactivate();
        }

        // =============================================
        // Scanner Setup
        // =============================================

        private void SetupScanner()
        {
            if (scanner != null) return;

            scanner = gameObject.GetComponent<QRCodeScanner>();
            if (scanner == null)
                scanner = gameObject.AddComponent<QRCodeScanner>();

            scanner.OnQRCodeDecoded += OnTokenDecoded;
            scanner.OnScanError += OnScanError;
        }

        private void OnDestroy()
        {
            if (scanner != null)
            {
                scanner.OnQRCodeDecoded -= OnTokenDecoded;
                scanner.OnScanError -= OnScanError;
            }

            // Drop the back-button listener so the spawned fallback doesn't call into a
            // destroyed QR panel if it outlives this one
            var pin = fallbackInstance as PinCodePanel;
            if (pin != null)
                pin.OnBackRequested.RemoveListener(ShowQrView);
        }

        // =============================================
        // Scan Callbacks
        // =============================================

        private void OnTokenDecoded(string token)
        {
            if (stopOnFirstDecode && scanner != null)
                scanner.StopScanning();

            UpdateStatus("QR Code found!", "Resolving token...");
            SetScanState(ScanState.Found);

            if (scanningIndicator != null)
                scanningIndicator.SetActive(false);

            TokenResolver resolver = GetTokenResolver();
            if (resolver != null)
            {
                resolver.ResolveToken(token, OnTokenResolved);
            }
            else
            {
                Debug.LogWarning("[COGNITIVE3D] No TokenResolver assigned. Using raw QR token as participant ID.");
                resolvedParticipantId = token;
                OnIdentificationReady();
            }
        }

        private void OnTokenResolved(TokenResult result)
        {
            if (result == null || !result.Success)
            {
                string error = result?.ErrorMessage ?? "Token resolution failed.";
                UpdateStatus("Error: " + error, "Try scanning again...");
                SetScanState(ScanState.Error);
                Debug.LogWarning("[COGNITIVE3D] Token resolution failed: " + error);

                // Hand off to the confirmation (failure) view
                SetScanViewActive(false);
                if (confirmationPanel != null)
                {
                    confirmationPanel.PopulateFailure(error);
                }

                OnQRFailure?.Invoke();
                return;
            }

            resolvedParticipantId = result.ParticipantId;

            if (!string.IsNullOrEmpty(result.ParticipantName))
                Cognitive3D_Manager.SetParticipantFullName(result.ParticipantName);

            if (result.Properties != null)
            {
                foreach (var kvp in result.Properties)
                    Cognitive3D_Manager.SetSessionProperty(kvp.Key, kvp.Value);
            }

            UpdateStatus("Identified!", "");
            SetScanState(ScanState.Found);

            // Hand off to the confirmation (success) view
            SetScanViewActive(false);

            if (confirmationPanel != null)
            {
                confirmationPanel.PopulateSuccess(result);
            }

            OnIdentificationReady();
        }

        private void OnIdentificationReady()
        {
            OnQRSuccess?.Invoke();
            ConfirmIdentification();
        }

        private void OnScanError(string error)
        {
            UpdateStatus("Camera error", error);
            Debug.LogError("[COGNITIVE3D] QR Scanner error: " + error);
        }

        // =============================================
        // Helpers
        // =============================================

        private TokenResolver GetTokenResolver()
        {
            if (tokenResolver == null) return null;

            return tokenResolver;
        }

        private void UpdateStatus(string status, string statusBar)
        {
            if (statusText != null)
                statusText.text = status;
            if (statusBarText != null)
                statusBarText.text = statusBar;
        }

        private enum ScanState { Searching, Found, Error }

        // Tints the status text to signal the current scan state.
        private void SetScanState(ScanState state)
        {
            if (state == ScanState.Searching) StartSearchingAnimation();
            else StopSearchingAnimation();

            if (colorScheme == null || statusText == null) return;

            switch (state)
            {
                case ScanState.Found: statusText.color = colorScheme.buttonBackground; break; // positive
                case ScanState.Error: statusText.color = colorScheme.errorText; break;
                default:              statusText.color = colorScheme.instructionText; break;   // searching
            }
        }

        private Coroutine searchingRoutine;

        private void StartSearchingAnimation()
        {
            StopSearchingAnimation();
            if (isActiveAndEnabled && statusText != null)
                searchingRoutine = StartCoroutine(AnimateSearching());
        }

        private void StopSearchingAnimation()
        {
            if (searchingRoutine != null)
            {
                StopCoroutine(searchingRoutine);
                searchingRoutine = null;
            }
        }

        // Cycles the trailing dots while searching: "Searching for QR code" . / .. / ...
        private System.Collections.IEnumerator AnimateSearching()
        {
            const string label = "Searching for QR code";
            var wait = new WaitForSeconds(0.4f);
            int dots = 1;
            while (true)
            {
                statusText.text = label + new string('.', dots);
                dots = dots % 3 + 1;
                yield return wait;
            }
        }

        private bool IsScanningSupported()
        {
#if UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }

        private void ShowFallback()
        {
            UpdateStatus("QR scanning not available on this platform.", "");

            if (scanningIndicator != null)
                scanningIndicator.SetActive(false);
            
            // Show the fallback panel (PIN entry) if assigned, otherwise just show the fallback button
            ShowPinView();
        }

        private void ConfigureInteraction()
        {
            if (interactionConfigured) return;
            interactionConfigured = true;

            // Set the raycaster on EVERY canvas this panel owns (incl. the confirmation panel),
            // so the interaction mode is honored across all of them, not just the scan canvas
            PanelInteractionSetup.ConfigureAllRaycasters(gameObject, interactionMode);

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = qrView.GetComponentInChildren<Canvas>(false);
            if (canvas != null && fallbackButton != null)
            {
                PanelInteractionSetup.Configure(
                    canvas,
                    fallbackButton.gameObject,
                    interactionMode,
                    OnFallbackClicked,
                    pointerSettings,
                    transform
                );
            }
        }

        private void OnFallbackClicked()
        {
            ShowPinView();
        }

        /// <summary>
        /// Switch from QR scanning to the fallback (PIN) panel. The fallback prefab is
        /// spawned on first use and reused thereafter
        /// </summary>
        public void ShowPinView()
        {
            if (scanner != null && scanner.IsScanning) scanner.StopScanning();
            StopSearchingAnimation();
            if (qrView != null) qrView.SetActive(false);

            var instance = EnsureFallbackInstance();
            if (instance != null)
                instance.gameObject.SetActive(true); // activateOnEnable runs its Activate()
        }

        /// <summary>
        /// Lazily instantiates the fallback prefab (once) and wires its "back" action, if any,
        /// to return to QR scanning. Returns the reusable instance
        /// </summary>
        private IdentificationPanelBase EnsureFallbackInstance()
        {
            if (fallbackInstance != null) return fallbackInstance;
            if (fallbackPanelPrefab == null) return null;

            // Spawn under a temporary INACTIVE holder so Awake/OnEnable/Activate don't run until
            // we've copied our settings in, otherwise Activate() runs with the prefab's own defaults
            var holder = new GameObject("~IdentifyFallbackSpawn");
            holder.SetActive(false);

            fallbackInstance = Instantiate(fallbackPanelPrefab, holder.transform);

            // Inherit QR's interaction mode, pointer style, and anchoring (before activation)
            fallbackInstance.CopySharedSettingsFrom(this);

            // If the fallback is a PIN panel, show its Back button, route it back to QR, and
            // give it the same token-resolver config as QR
            var pin = fallbackInstance as PinCodePanel;
            if (pin != null)
            {
                pin.SetBackButtonVisible(true);
                pin.OnBackRequested.AddListener(ShowQrView);

                if (tokenResolver != null)
                {
                    if (pin.TokenResolver != null)
                        pin.TokenResolver.CopyConfigFrom(tokenResolver); // PIN keeps its own resolver, same config
                    else
                        pin.TokenResolver = tokenResolver;               // no PIN resolver → reuse QR's
                }

                // Forward the PIN confirmation's Continue action to QR's, so the user only wires it
                // once (on the QR confirmation) and the PIN path does the same thing.
                if (pin.ConfirmationPanel != null && confirmationPanel != null)
                    pin.ConfirmationPanel.OnContinueSuccess.AddListener(confirmationPanel.OnContinueSuccess.Invoke);
            }

            // Move it out of the temporary holder (still inactive); ShowPinView activates it later,
            // running Awake/OnEnable/Activate with the copied settings.
            fallbackInstance.gameObject.SetActive(false);
            fallbackInstance.transform.SetParent(null, false);
            Destroy(holder);

            return fallbackInstance;
        }

        /// <summary>
        /// Switch from the PIN keypad back to QR scanning
        /// </summary>
        public void ShowQrView()
        {
            if (fallbackInstance != null) fallbackInstance.gameObject.SetActive(false);
            if (qrView != null) qrView.SetActive(true);
            SetScanViewActive(true);

            if (!IsScanningSupported()) { ShowFallback(); return; }

            SetupScanner();
            UpdateStatus("Searching for QR code...", "Passthrough camera active \u2014 scanning...");
            SetScanState(ScanState.Searching);
            scanner.StartScanning();
        }

        /// <summary>
        /// Dismiss the confirmation (failure) view and resume scanning. Wire to
        /// QRConfirmationPanel.OnContinueFailure so the scan view returns only on user retry
        /// </summary>
        public void ResumeScanning()
        {
            SetScanViewActive(true);
            UpdateStatus("Searching for QR code...", "Passthrough camera active \u2014 scanning...");
            SetScanState(ScanState.Searching);

            if (!IsScanningSupported()) { ShowFallback(); return; }

            SetupScanner();
            if (scanner != null && !scanner.IsScanning)
                scanner.StartScanning();
        }

        /// <summary>
        /// Toggles the QR scan-view chrome (fallback button, scanning indicator, camera feed)
        /// </summary>
        private void SetScanViewActive(bool active)
        {
            if (fallbackButton != null)    fallbackButton.gameObject.SetActive(active);
            if (scanningIndicator != null) scanningIndicator.SetActive(active);
        }

        // =============================================
        // Theming
        // =============================================

        public void ApplyColorScheme(PanelColorScheme scheme)
        {
            if (scheme == null) return;

            if (panelBackground != null) panelBackground.color = scheme.panelBackground;
            if (headerText != null) headerText.color = scheme.headerText;
            if (subtitleText != null) subtitleText.color = scheme.subtitleText;
            if (statusText != null) statusText.color = scheme.instructionText;
            if (statusBarBackground != null) statusBarBackground.color = scheme.instructionRowBackground;
            if (statusBarText != null) statusBarText.color = scheme.instructionText;
            if (fallbackButtonOutline != null) fallbackButtonOutline.effectColor = scheme.digitBoxOutline;
            if (fallbackButtonImage != null) fallbackButtonImage.color = scheme.panelBackground;
            if (fallbackButtonText != null) fallbackButtonText.color = scheme.digitText;

            ApplyGhostButtonColors(fallbackButton, scheme);
        }

        /// <summary>
        /// Syncs a ghost button's per-state colors to both the UGUI Button ColorBlock and its
        /// VirtualButton, so the scheme drives the button consistently across interaction modes
        /// </summary>
        private static void ApplyGhostButtonColors(Button button, PanelColorScheme scheme)
        {
            if (button == null) return;

            ColorBlock cb = button.colors;
            cb.normalColor = scheme.panelBackground;
            cb.highlightedColor = scheme.digitBoxBackground;
            cb.pressedColor = scheme.digitBoxOutline;
            cb.disabledColor = scheme.buttonDisabled;
            button.colors = cb;

            var vb = button.GetComponent<VirtualButton>();
            if (vb != null)
            {
                vb.normalColor = scheme.panelBackground;
                vb.hoverColor = scheme.digitBoxBackground;
                vb.pressedColor = scheme.digitBoxOutline;
                vb.disabledColor = scheme.buttonDisabled;
            }
        }
    }
}
