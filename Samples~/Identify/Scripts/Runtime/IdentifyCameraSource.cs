using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Supplies camera frames for QR decoding plus a preview texture. Implementations wrap a
    /// platform camera: the Meta Passthrough Camera API on Quest, generic WebCamTexture elsewhere.
    /// </summary>
    internal interface IIdentifyCameraSource
    {
        bool IsReady { get; }
        Vector2Int Resolution { get; }
        Texture PreviewTexture { get; }

        // Requests permission and starts the camera. Yields until ready or failed; reports failure via onError.
        IEnumerator Initialize(MonoBehaviour host, Action<string> onError);

        // Copies the latest frame into buffer (reallocated if the size changed). False if none is available.
        bool TryGetLatestFrame(ref Color32[] buffer);

        void Dispose();
    }

    /// <summary>
    /// Generic camera source using Unity's WebCamTexture. Fallback for non-Meta Android XR devices
    /// that expose the passthrough/front camera as a WebCamDevice.
    /// </summary>
    internal class WebCamIdentifyCameraSource : IIdentifyCameraSource
    {
        private const string HeadsetCameraPermission = "horizonos.permission.HEADSET_CAMERA";
        private const string AndroidCameraPermission = "android.permission.CAMERA";

        private readonly int requestedWidth;
        private readonly int requestedHeight;
        private readonly int requestedFps;
        private readonly string preferredCameraNameContains;

        private WebCamTexture webCamTexture;

        public WebCamIdentifyCameraSource(int width, int height, int fps, string preferredName)
        {
            requestedWidth = width;
            requestedHeight = height;
            requestedFps = fps;
            preferredCameraNameContains = preferredName;
        }

        public bool IsReady => webCamTexture != null && webCamTexture.isPlaying && webCamTexture.width > 16;
        public Vector2Int Resolution => webCamTexture != null
            ? new Vector2Int(webCamTexture.width, webCamTexture.height)
            : Vector2Int.zero;
        public Texture PreviewTexture => webCamTexture;

        public IEnumerator Initialize(MonoBehaviour host, Action<string> onError)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
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
                    onError?.Invoke("Camera permission denied");
                    yield break;
                }
            }

            // Devices may not be enumerated the instant permission is granted; poll briefly.
            WebCamDevice[] devices = WebCamTexture.devices;
            float deviceWait = 0f;
            while ((devices == null || devices.Length == 0) && deviceWait < 10f)
            {
                deviceWait += 0.25f;
                yield return new WaitForSeconds(0.25f);
                devices = WebCamTexture.devices;
            }

            if (devices == null || devices.Length == 0)
            {
                onError?.Invoke("No camera available");
                yield break;
            }

            string chosen = ChooseCamera(devices);
            webCamTexture = new WebCamTexture(chosen, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();

            // Wait for the first frame (width stays <=16 until the camera delivers one).
            float waitTime = 0f;
            while ((webCamTexture.width <= 16 || !webCamTexture.didUpdateThisFrame) && waitTime < 10f)
            {
                waitTime += 0.1f;
                yield return new WaitForSeconds(0.1f);
            }

            if (webCamTexture.width <= 16)
                onError?.Invoke("Camera failed to start");
#else
            onError?.Invoke("WebCamTexture camera is only available on Android XR devices");
            yield break;
#endif
        }

        public bool TryGetLatestFrame(ref Color32[] buffer)
        {
            if (webCamTexture == null || !webCamTexture.isPlaying ||
                webCamTexture.width <= 16 || !webCamTexture.didUpdateThisFrame)
                return false;

            int count = webCamTexture.width * webCamTexture.height;
            if (buffer == null || buffer.Length != count)
                buffer = new Color32[count];
            webCamTexture.GetPixels32(buffer);
            return true;
        }

        public void Dispose()
        {
            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying) webCamTexture.Stop();
                UnityEngine.Object.Destroy(webCamTexture);
                webCamTexture = null;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool HasAnyCameraPermission()
        {
            return Permission.HasUserAuthorizedPermission(HeadsetCameraPermission)
                || Permission.HasUserAuthorizedPermission(AndroidCameraPermission);
        }
#endif

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
    }
}
