using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Cognitive3D.Components
{
    [RequireComponent(typeof(AndroidPlugin))]
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/MicrophoneAudioRecorder")]
    public class MicrophoneAudioRecorder : AnalyticsComponentBase
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        /// <summary>
        /// Called when the session starts. Sets up a listener for when the Android plugin instance is ready.
        /// </summary>
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            if (AndroidPlugin.isInitialized)
            {
                AndroidPlugin_OnInstanceCreated();
            }
            else
            {
                AndroidPlugin.OnInstanceCreated += AndroidPlugin_OnInstanceCreated;
            }
        }

        /// <summary>
        /// Triggered once the Android plugin instance is created.
        /// Runs XRPF checks, then starts the microphone-permission flow
        /// that enables session-long audio recording on the plugin if granted.
        /// </summary>
        private void AndroidPlugin_OnInstanceCreated()
        {
            AndroidPlugin.OnInstanceCreated -= AndroidPlugin_OnInstanceCreated;
#if XRPF
            // Early check for XRPF audio permission
            if (!XRPF.PrivacyFramework.Agreement.IsAudioDataAllowed)
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.device.audio_tracking.enabled", false);
                return; // Don't proceed with setting up audio recording
            }
#endif
            StartCoroutine(RequestMicrophoneAndEnableRecording());
        }

        private IEnumerator RequestMicrophoneAndEnableRecording()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var callbacks = new PermissionCallbacks();
                bool done = false;
                callbacks.PermissionGranted += _ => done = true;
                callbacks.PermissionDenied += _ => done = true;
                callbacks.PermissionDeniedAndDontAskAgain += _ => done = true;

                Permission.RequestUserPermission(Permission.Microphone, callbacks);
                while (!done) yield return null;
            }

            bool granted = Permission.HasUserAuthorizedPermission(Permission.Microphone);
            Cognitive3D_Manager.SetSessionProperty("c3d.device.audio_tracking.enabled", granted);

            if (granted)
            {
                AndroidPlugin.Instance.Call("setAudioRecordingEnabled", true);
            }
        }

        /// <summary>
        /// Called when the application is quitting. Disables audio recording on the plugin,
        /// which releases the microphone. Pause/resume during the session is handled on the
        /// Java side via ActivityLifecycleCallbacks while the flag is enabled.
        /// </summary>
        void OnApplicationQuit()
        {
            AndroidPlugin.Instance?.Call("setAudioRecordingEnabled", false);
        }
#endif

        /// <summary>
        /// Description to display in inspector
        /// </summary>
        /// <returns> A string representing the description </returns>
        public override string GetDescription()
        {
#if UNITY_ANDROID
    return "Captures user speech via the device’s microphone on Android. This feature is unavailable when running in the Unity Editor.";
#else
    return "The microphone audio recorder is supported only when the build target is set to Android.";
#endif
        }

        /// <summary>
        /// Warning for incompatible platform to display on inspector
        /// </summary>
        public override bool GetWarning()
        {
#if UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }
    }
}
