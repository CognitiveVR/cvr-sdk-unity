#if C3D_IDENTIFY_PCA
using System;
using System.Collections;
using Meta.XR;
using Unity.Collections;
using UnityEngine;

namespace Cognitive3D.Identify
{
    /// <summary>
    /// Camera source backed by Meta's Passthrough Camera API (PassthroughCameraAccess, from MRUK).
    /// This is the supported path on Quest: the component handles permission, initialization and
    /// frame delivery internally, so it avoids the dead-first-frame problem and the multi-second
    /// device-enumeration delay that raw WebCamTexture hits on Quest.
    /// </summary>
    internal class MetaPassthroughCameraSource : IIdentifyCameraSource
    {
        private const PassthroughCameraAccess.CameraPositionType Position =
            PassthroughCameraAccess.CameraPositionType.Left;

        private PassthroughCameraAccess access;
        private bool ownsAccess; // true only when we created the component and must destroy it

        public bool IsReady => access != null && access.IsPlaying;
        public Vector2Int Resolution => access != null ? access.CurrentResolution : Vector2Int.zero;
        public Texture PreviewTexture => access != null && access.IsPlaying ? access.GetTexture() : null;

        public IEnumerator Initialize(MonoBehaviour host, Action<string> onError)
        {
            if (!PassthroughCameraAccess.IsSupported)
            {
                onError?.Invoke("Passthrough camera not supported on this device");
                yield break;
            }

            var permission = OVRPermissionsRequester.Permission.PassthroughCameraAccess;
            if (!OVRPermissionsRequester.IsPermissionGranted(permission))
            {
                OVRPermissionsRequester.Request(new[] { permission });

                float timeout = 30f;
                while (!OVRPermissionsRequester.IsPermissionGranted(permission) && timeout > 0f)
                {
                    timeout -= 0.25f;
                    yield return new WaitForSeconds(0.25f);
                }

                if (!OVRPermissionsRequester.IsPermissionGranted(permission))
                {
                    onError?.Invoke("Camera permission denied");
                    yield break;
                }
            }

            // Only one PassthroughCameraAccess is allowed per camera position, so reuse an existing
            // one (e.g. a Passthrough Camera building block already in the scene) instead of creating
            // a second. Only create - and later destroy - our own when the scene has none.
            access = FindExisting(Position);
            if (access != null)
            {
                ownsAccess = false;
                if (!access.isActiveAndEnabled)
                {
                    access.gameObject.SetActive(true);
                    access.enabled = true;
                }
            }
            else
            {
                ownsAccess = true;
                // Configure while inactive so CameraPosition is set before OnEnable runs.
                var go = new GameObject("Cognitive3D Identify Passthrough Camera");
                go.SetActive(false);
                go.transform.SetParent(host.transform, false);
                access = go.AddComponent<PassthroughCameraAccess>();
                access.CameraPosition = Position;
                go.SetActive(true);
            }

            float startTimeout = 20f;
            while (!access.IsPlaying && startTimeout > 0f)
            {
                startTimeout -= Time.deltaTime;
                yield return null;
            }

            if (!access.IsPlaying)
                onError?.Invoke("Camera failed to start");
        }

        public bool TryGetLatestFrame(ref Color32[] buffer)
        {
            if (access == null || !access.IsPlaying)
                return false;

            NativeArray<Color32> colors = access.GetColors();
            if (!colors.IsCreated || colors.Length == 0)
                return false;

            if (buffer == null || buffer.Length != colors.Length)
                buffer = new Color32[colors.Length];
            colors.CopyTo(buffer);
            return true;
        }

        public void Dispose()
        {
            // Only destroy the component if we created it; a scene-owned one keeps living.
            if (access != null && ownsAccess)
                UnityEngine.Object.Destroy(access.gameObject);
            access = null;
        }

        private static PassthroughCameraAccess FindExisting(PassthroughCameraAccess.CameraPositionType position)
        {
            var all = UnityEngine.Object.FindObjectsByType<PassthroughCameraAccess>(FindObjectsSortMode.None);
            foreach (var a in all)
            {
                if (a.CameraPosition == position)
                    return a;
            }
            return null;
        }
    }
}
#endif
