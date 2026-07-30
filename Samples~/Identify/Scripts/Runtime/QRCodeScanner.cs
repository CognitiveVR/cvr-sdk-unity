using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif
using ZXing;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Scans QR codes using the headset passthrough camera via Unity's WebCamTexture,
    /// decoding frames in C# with ZXing.Net.
    ///
    /// Works on devices that expose the passthrough/front camera as a WebCamDevice:
    ///   - Meta Quest 3 / 3S / Pro (Horizon OS v74+), permission "horizonos.permission.HEADSET_CAMERA"
    ///   - Android XR (GalaxyXR) permission "android.permission.CAMERA"
    /// </summary>
    [AddComponentMenu("Cognitive3D/Identify/QR Code Scanner")]
    public class QRCodeScanner : MonoBehaviour
    {
        [Header("Camera")]
        [Tooltip("Requested camera resolution. The device picks the closest supported size.")]
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 960;
        [SerializeField] private int requestedFps = 30;
        [Tooltip("Substring used to prefer a specific camera device name (case-insensitive). " +
                 "Falls back to any world-facing device, then the first device.")]
        [SerializeField] private string preferredCameraNameContains = "passthrough";

        [Header("Decoding")]
        [Tooltip("Seconds between decode attempts. Lower = more responsive, higher = less CPU.")]
        [SerializeField] private float decodeInterval = 0.4f;

        /// <summary>
        /// Fired when a QR code is successfully decoded. Parameter is the decoded string
        /// </summary>
        public event Action<string> OnQRCodeDecoded;

        /// <summary>
        /// Fired when the camera preview texture becomes available (assign to a RawImage)
        /// </summary>
        public event Action<Texture> OnPreviewFrameUpdated;

        /// <summary>
        /// Fired if the scanner encounters an error (e.g., no camera permission)
        /// </summary>
        public event Action<string> OnScanError;

        public bool IsScanning { get; private set; }

        // Meta passthrough camera permission (Horizon OS v74+). Custom permission strings
        // work with the UnityEngine.Android.Permission APIs.
        private const string HeadsetCameraPermission = "horizonos.permission.HEADSET_CAMERA";
        private const string AndroidCameraPermission = "android.permission.CAMERA";

        private WebCamTexture webCamTexture;
        private Coroutine scanCoroutine;
        private Texture2D editorPreviewTexture;

        // ZXing decoder, configured for QR codes only.
        private readonly BarcodeReader barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new[] { BarcodeFormat.QR_CODE }
            }
        };

        // Background decode hand-off (decode runs off the main thread to avoid VR hitches).
        private Color32[] decodeBuffer;
        private volatile bool decodeInProgress;
        private volatile string pendingResult;

        /// <summary>
        /// Begins scanning for QR codes using the device camera.
        /// </summary>
        public void StartScanning()
        {
            if (IsScanning) return;
            IsScanning = true;

#if !UNITY_EDITOR && UNITY_ANDROID
            scanCoroutine = StartCoroutine(RequestPermissionsAndScan());
#endif
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

            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying) webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }
        }

        private void OnDestroy()
        {
            StopScanning();
            if (editorPreviewTexture != null)
                Destroy(editorPreviewTexture);
        }

        // =============================================
        // WebCamTexture scanning (device)
        // =============================================

#if UNITY_ANDROID && !UNITY_EDITOR
        private IEnumerator RequestPermissionsAndScan()
        {
            if (!HasAnyCameraPermission())
            {
                Permission.RequestUserPermission(HeadsetCameraPermission);
                Permission.RequestUserPermission(AndroidCameraPermission);

                float timeout = 30f;
                while (!HasAnyCameraPermission() && timeout > 0f)
                {
                    timeout -= 0.25f;
                    yield return new WaitForSeconds(0.25f);
                }

                if (!HasAnyCameraPermission())
                {
                    Debug.LogError("[COGNITIVE3D] Camera permission denied. " +
                        "Add \"horizonos.permission.HEADSET_CAMERA\" (Meta) and/or " +
                        "\"android.permission.CAMERA\" (PICO) to AndroidManifest.xml and grant at runtime.");
                    IsScanning = false;
                    OnScanError?.Invoke("Camera permission denied");
                    yield break;
                }
            }

            yield return StartCoroutine(StartWebCam());
        }

        private static bool HasAnyCameraPermission()
        {
            return Permission.HasUserAuthorizedPermission(HeadsetCameraPermission)
                || Permission.HasUserAuthorizedPermission(AndroidCameraPermission);
        }
#endif

        private IEnumerator StartWebCam()
        {
            // On Meta Quest the passthrough cameras often aren't enumerated the instant the
            // HEADSET_CAMERA permission is granted, so poll for 10 seconds before giving up
            // rather than failing on the first empty read
            WebCamDevice[] devices = WebCamTexture.devices;
            float deviceWait = 0f;
            while ((devices == null || devices.Length == 0) && deviceWait < 10f && IsScanning)
            {
                deviceWait += 0.25f;
                yield return new WaitForSeconds(0.25f);
                devices = WebCamTexture.devices;
            }

            if (devices == null || devices.Length == 0)
            {
                Debug.LogError("[COGNITIVE3D] QRCodeScanner: No camera devices found after waiting. " +
                    "On Quest this requires Horizon OS v74+ on Quest 3/3S/Pro with the HEADSET_CAMERA permission granted.");
                IsScanning = false;
                OnScanError?.Invoke("No camera available");
                yield break;
            }

            string chosen = ChooseCamera(devices);

            webCamTexture = new WebCamTexture(chosen, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();

            // Wait for the camera to initialize (width stays <=16 until the first frame arrives).
            float waitTime = 0f;
            while ((webCamTexture.width <= 16 || !webCamTexture.didUpdateThisFrame) && waitTime < 10f && IsScanning)
            {
                waitTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (webCamTexture.width <= 16)
            {
                Debug.LogError("[COGNITIVE3D] QRCodeScanner: Camera did not produce frames after 10s.");
                IsScanning = false;
                OnScanError?.Invoke("Camera failed to start");
                yield break;
            }

            Debug.Log("QRCodeScanner: Camera ready at " +
                webCamTexture.width + "x" + webCamTexture.height);
            OnPreviewFrameUpdated?.Invoke(webCamTexture);

            yield return StartCoroutine(DecodeLoop());
        }

        private string ChooseCamera(WebCamDevice[] devices)
        {
            // 1) Preferred name substring (e.g. "passthrough").
            if (!string.IsNullOrEmpty(preferredCameraNameContains))
            {
                foreach (var d in devices)
                {
                    if (d.name != null &&
                        d.name.IndexOf(preferredCameraNameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                        return d.name;
                }
            }

            // 2) A world-facing camera (not the user-facing one).
            foreach (var d in devices)
            {
                if (!d.isFrontFacing) return d.name;
            }

            // 3) Fallback: first device.
            return devices[0].name;
        }

        private IEnumerator DecodeLoop()
        {
            var wait = new WaitForSeconds(decodeInterval);

            while (IsScanning && webCamTexture != null)
            {
                // A decode finished on the background thread. Handle the result on the main thread
                string result = pendingResult;
                if (!string.IsNullOrEmpty(result))
                {
                    pendingResult = null;
                    Debug.Log("QR code decoded: " + result);
                    OnQRCodeDecoded?.Invoke(result);

                    // The callback may have stopped scanning (e.g. stopOnFirstDecode),
                    // which disposes webCamTexture. Bail out before touching it again
                    if (!IsScanning || webCamTexture == null)
                        yield break;
                }

                // Kick off the next decode if one isn't already running and a fresh frame exists
                if (!decodeInProgress && webCamTexture.didUpdateThisFrame)
                {
                    int w = webCamTexture.width;
                    int h = webCamTexture.height;

                    if (decodeBuffer == null || decodeBuffer.Length != w * h)
                        decodeBuffer = new Color32[w * h];

                    // GetPixels32 must run on the main thread
                    webCamTexture.GetPixels32(decodeBuffer);

                    decodeInProgress = true;
                    Color32[] frame = decodeBuffer;
                    Task.Run(() =>
                    {
                        try
                        {
                            var r = barcodeReader.Decode(frame, w, h);
                            if (r != null && !string.IsNullOrEmpty(r.Text))
                                pendingResult = r.Text;
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("QRCodeScanner: Decode error: " + e.Message);
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
