using System.Collections.Generic;
using UnityEngine;
using System;

#if C3D_OCULUS
using OVR;
#endif

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Passthrough")]
    public class Passthrough : AnalyticsComponentBase
    {
        private float previousPassthroughEnabled;
        private float lastEventTime;

        private readonly float PassthroughSendInterval = 1;
        private float currentTime;

        private readonly float SupportGracePeriod = 5f;

        private bool IsPassthroughSupported;
        private Coroutine resolveSupportRoutine;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;

            // Resolve capability immediately if XR is ready (the common case). If it isn't yet,
            // retry briefly in a coroutine so a slow XR/feature init doesn't permanently disable tracking
            if (!TrySetPassthroughSupported())
            {
                resolveSupportRoutine = StartCoroutine(ResolveSupportRoutine());
            }

            previousPassthroughEnabled = GetPassthroughStatus();
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            // of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (!isActiveAndEnabled) { return; }
            if (!IsPassthroughSupported) { return; }

            currentTime += deltaTime;
            if (currentTime <= PassthroughSendInterval) { return; }
            currentTime = 0;

            var currentPassthroughEnabled = GetPassthroughStatus();
            bool changed = currentPassthroughEnabled != previousPassthroughEnabled;

            SensorRecorder.RecordDataPoint("c3d.app.passthroughEnabled", currentPassthroughEnabled);

            if (changed)
            {
                new CustomEvent("c3d.passthrough_layer_changed")
                    .SetProperties(new Dictionary<string, object>
                    {
                        {"Duration of Previous State",  Time.time - lastEventTime},
                        {"New State", currentPassthroughEnabled }
                    })
                    .Send();
                lastEventTime = Time.time;
                previousPassthroughEnabled = currentPassthroughEnabled;
            }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            if (resolveSupportRoutine != null)
            {
                StopCoroutine(resolveSupportRoutine);
                resolveSupportRoutine = null;
            }
            IsPassthroughSupported = false;

            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        /// <summary>
        /// Records the capability session property if passthrough is supported right now.
        /// Returns true once resolved so the caller can stop retrying
        /// </summary>
        private bool TrySetPassthroughSupported()
        {
            if (!GetPassthroughSupported()) { return false; }

            IsPassthroughSupported = true;
            Cognitive3D_Manager.SetSessionProperty("c3d.app.passthrough.supported", true);
            return true;
        }

        /// <summary>
        /// Capability can lag XR/feature init by a beat, so retry briefly after session begin.
        /// If it never resolves within the grace period the device can't do passthrough
        /// </summary>
        private System.Collections.IEnumerator ResolveSupportRoutine()
        {
            float elapsed = 0f;
            while (elapsed < SupportGracePeriod)
            {
                yield return new WaitForSeconds(PassthroughSendInterval);
                elapsed += PassthroughSendInterval;
                if (TrySetPassthroughSupported()) { yield break; }
            }
            Cognitive3D_Manager.SetSessionProperty("c3d.app.passthrough.supported", false);
        }

#if C3D_OCULUS
        private OVRPassthroughLayer passthroughLayerRef;
#endif

        float GetPassthroughStatus()
        {
#if C3D_OCULUS
            var capability = OVRManager.GetPassthroughCapabilities();
            if (!capability.SupportsPassthrough) return 0f;
            if (OVRManager.instance != null && !OVRManager.instance.isInsightPassthroughEnabled) return 0f;

            if (passthroughLayerRef == null)
            {
                passthroughLayerRef = GameObject.FindFirstObjectByType<OVRPassthroughLayer>();
                return 0f;
            }
            else
            {
                return passthroughLayerRef.isActiveAndEnabled ? 1f : 0f;
            }
#elif C3D_VIVEWAVE
            return Wave.Native.Interop.WVR_IsPassthroughOverlayVisible() ? 1f : 0f;
#elif C3D_PICOXR && COGNITIVE3D_PICOXR_3_0_OR_NEWER
            return Unity.XR.PXR.PXR_Manager.EnableVideoSeeThrough ? 1f : 0f;
#else

#if COGNITIVE3D_AR_FOUNDATION_6_2_OR_NEWER
            var cameraSubsystem = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<UnityEngine.XR.ARSubsystems.XRCameraSubsystem>();
            if (cameraSubsystem != null)
            {
                return cameraSubsystem.running ? 1f : 0f;
            }
#endif

#if COGNITIVE3D_VIVE_OPENXR_2_5_OR_NEWER
            var ids = VIVE.OpenXR.Passthrough.PassthroughAPI.GetCurrentPassthroughLayerIDs();
            if (ids != null && ids.Count > 0)
            {
                return 1f;
            }
#endif
            return 0f;
#endif
        }

        /// <summary>
        /// Whether the current device/runtime is capable of passthrough at all.
        /// Reported once as a session property so a session with no passthrough sensor can be
        /// distinguished from a session on hardware that cannot do passthrough
        /// </summary>
        bool GetPassthroughSupported()
        {
#if C3D_OCULUS
            return OVRManager.GetPassthroughCapabilities().SupportsPassthrough;
#elif C3D_VIVEWAVE
            return (Wave.Native.Interop.WVR_GetSupportedFeatures() & (ulong)Wave.Native.WVR_SupportedFeature.WVR_SupportedFeature_PassthroughOverlay) != 0;
#elif C3D_PICOXR && COGNITIVE3D_PICOXR_3_0_OR_NEWER
            return Unity.XR.PXR.PXR_Plugin.System.UPxr_GetConfigInt(Unity.XR.PXR.ConfigType.SupportQuickSeethrough) != 0;
#else

#if COGNITIVE3D_AR_FOUNDATION_6_2_OR_NEWER
            var cameraSubsystem = UnityEngine.XR.Management.XRGeneralSettings.Instance?.Manager?.activeLoader?.GetLoadedSubsystem<UnityEngine.XR.ARSubsystems.XRCameraSubsystem>();
            if (cameraSubsystem != null)
            {
                return true;
            }
#endif

#if COGNITIVE3D_VIVE_OPENXR_2_5_OR_NEWER
            if (VIVE.OpenXR.Passthrough.PassthroughAPI.GetCurrentPassthroughLayerIDs() != null)
            {
                return true;
            }
#endif
            return false;
#endif
        }

        public override string GetDescription()
        {
#if C3D_OCULUS || C3D_VIVEWAVE || COGNITIVE3D_AR_FOUNDATION_6_2_OR_NEWER || COGNITIVE3D_VIVE_OPENXR_2_5_OR_NEWER
            return "Records a sensor value determining if passthrough is enabled.";
#else
            return "Passthrough properties can only be accessed on a supported platform (Oculus, Vive Wave, VIVE OpenXR, or AR Foundation).";
#endif
        }


        public override bool GetWarning()
        {
#if C3D_OCULUS || C3D_VIVEWAVE || COGNITIVE3D_AR_FOUNDATION_6_2_OR_NEWER || COGNITIVE3D_VIVE_OPENXR_2_5_OR_NEWER
            return false;
#else
            return true;
#endif
        }
    }
}
