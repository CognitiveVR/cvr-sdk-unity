using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        /// Starts audio recording on the plugin.
        /// </summary>
        private void AndroidPlugin_OnInstanceCreated()
        {
            AndroidPlugin.OnInstanceCreated -= AndroidPlugin_OnInstanceCreated;
            // Request permission if not already granted
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }
            // Audio Recording
            AndroidPlugin.Instance.Call("startAudioRecording");
        }

        /// <summary>
        /// Called when the application is quitting. Stops audio recording on the plugin.
        /// Also handling pause/resume cases on Plugin side where Unity pause/resume callbacks may not fire correctly.
        /// </summary>
        void OnApplicationQuit()
        {
            AndroidPlugin.Instance?.Call("stopAudioRecording");
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
    return "The audio recorder is supported only when the build target is set to Android.";
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
