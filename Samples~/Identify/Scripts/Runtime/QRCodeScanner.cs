using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using ZXing;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Scans QR codes from the headset camera and decodes them in C# with ZXing.Net.
    ///
    /// Camera frames come from an <see cref="IIdentifyCameraSource"/>:
    ///   - Meta Quest: the Meta Passthrough Camera API, when the Meta MRUK SDK is present (C3D_IDENTIFY_PCA).
    ///   - Other Android XR: Unity's WebCamTexture as a fallback.
    /// </summary>
    [AddComponentMenu("Cognitive3D/Identify/QR Code Scanner")]
    public class QRCodeScanner : MonoBehaviour
    {
        [Header("Camera (WebCamTexture fallback only)")]
        [Tooltip("Requested camera resolution for the WebCamTexture fallback. The device picks the closest supported size.")]
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 960;
        [SerializeField] private int requestedFps = 30;
        [Tooltip("Substring used to prefer a specific camera device name (WebCamTexture fallback, case-insensitive).")]
        [SerializeField] private string preferredCameraNameContains = "passthrough";

        [Header("Decoding")]
        [Tooltip("Seconds between decode attempts. Lower = more responsive, higher = less CPU.")]
        [SerializeField] private float decodeInterval = 0.4f;

        // Fired when a QR code is successfully decoded. Parameter is the decoded string
        public event Action<string> OnQRCodeDecoded;

        // Fired when the camera preview texture becomes available (assign to a RawImage)
        public event Action<Texture> OnPreviewFrameUpdated;

        // Fired if the scanner encounters an error (e.g., no camera permission)
        public event Action<string> OnScanError;

        public bool IsScanning { get; private set; }

        private IIdentifyCameraSource cameraSource;
        private Coroutine scanCoroutine;

        // ZXing decoder, configured for QR codes only
        private readonly BarcodeReader barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        // Background decode hand-off (decode runs off the main thread to avoid VR hitches)
        private Color32[] frameBuffer;
        private volatile bool decodeInProgress;
        private volatile string pendingResult;

        /// <summary>
        /// Begins scanning for QR codes using the device camera.
        /// </summary>
        public void StartScanning()
        {
            if (IsScanning) return;
            IsScanning = true;
            scanCoroutine = StartCoroutine(ScanRoutine());
        }

        /// <summary>
        /// Stops scanning and releases the camera.
        /// </summary>
        public void StopScanning()
        {
            if (!IsScanning) return;
            IsScanning = false;

            if (scanCoroutine != null)
            {
                StopCoroutine(scanCoroutine);
                scanCoroutine = null;
            }

            cameraSource?.Dispose();
            cameraSource = null;
        }

        private void OnDestroy() => StopScanning();

        private IIdentifyCameraSource CreateCameraSource()
        {
#if C3D_IDENTIFY_PCA
            return new MetaPassthroughCameraSource();
#else
            return new WebCamIdentifyCameraSource(requestedWidth, requestedHeight, requestedFps, preferredCameraNameContains);
#endif
        }

        private IEnumerator ScanRoutine()
        {
            cameraSource = CreateCameraSource();

            bool failed = false;
            yield return cameraSource.Initialize(this, msg =>
            {
                failed = true;
                IsScanning = false;
                OnScanError?.Invoke(msg);
                Debug.LogError("[COGNITIVE3D] QRCodeScanner: " + msg);
            });

            if (failed || !IsScanning || cameraSource == null || !cameraSource.IsReady)
            {
                cameraSource?.Dispose();
                cameraSource = null;
                yield break;
            }

            Debug.Log("QRCodeScanner: Camera ready at " +
                cameraSource.Resolution.x + "x" + cameraSource.Resolution.y);
            OnPreviewFrameUpdated?.Invoke(cameraSource.PreviewTexture);

            yield return DecodeLoop();
        }

        private IEnumerator DecodeLoop()
        {
            var wait = new WaitForSeconds(decodeInterval);

            while (IsScanning && cameraSource != null)
            {
                // A decode finished on the background thread. Handle the result on the main thread.
                string result = pendingResult;
                if (!string.IsNullOrEmpty(result))
                {
                    pendingResult = null;
                    Debug.Log("[COGNITIVE3D] QRCodeScanner: QR code decoded");
                    OnQRCodeDecoded?.Invoke(result);

                    // The callback may have stopped scanning (which disposes the source). Bail out.
                    if (!IsScanning || cameraSource == null)
                        yield break;
                }

                // Kick off the next decode if one isn't already running and a fresh frame exists.
                if (!decodeInProgress && cameraSource.IsReady && cameraSource.TryGetLatestFrame(ref frameBuffer))
                {
                    Vector2Int size = cameraSource.Resolution;
                    int w = size.x;
                    int h = size.y;
                    Color32[] frame = frameBuffer;

                    decodeInProgress = true;
                    Task.Run(() =>
                    {
                        try
                        {
                            var r = barcodeReader.Decode(frame, w, h);
                            if (r != null && !string.IsNullOrEmpty(r.Text))
                                pendingResult = r.Text;
                        }
                        catch (Exception)
                        {
                            // Decode failed for this frame; skip it. Logging is avoided here because
                            // this runs on a background thread.
                        }
                        finally
                        {
                            decodeInProgress = false;
                        }
                    });
                }

                yield return wait;
            }
        }
    }
}
