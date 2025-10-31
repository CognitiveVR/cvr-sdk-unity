using System;
using UnityEngine;
using System.Collections.Concurrent;
using UnityEngine.Serialization;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/AppAudioRecorder")]
    public class AppAudioRecorder : AnalyticsComponentBase
    {
        public string audioChannelName = "default";
#if UNITY_ANDROID && !UNITY_EDITOR
        // Audio settings - Send 48kHz directly to Android
        private const int CHANNELS = 1; // Mono

        // AAC frame size for proper encoding
        private const int AAC_FRAME_SIZE = 1024;
        private const int BYTES_PER_SAMPLE = 2; // 16-bit PCM

        private bool isInitialized = false;
        private bool wasRecordingBeforePause = false;

        // Audio silence detection
        private float silenceThreshold = 0.001f;
        private float silenceDuration = 0f;
        private float maxSilenceBeforeFlush = 2f; // Flush after 2 seconds of silence
        private bool hasActiveAudio = false;

        // Cache delta time for audio thread access
        private float cachedDeltaTime = 0f;

        // Audio data queue for thread safety
        private ConcurrentQueue<AudioData> audioQueue = new ConcurrentQueue<AudioData>();

        private struct AudioData
        {
            public float[] samples;
            public long timestamp;
        }

        private void Awake()
        {
            // Validate audioChannel
            if (string.IsNullOrEmpty(audioChannelName))
            {
                audioChannelName = $"channel_{GetInstanceID()}";
                Util.logWarning($"AudioCapture: No channel specified, using: {audioChannelName}");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (!AndroidPlugin.isInitialized)
            {
                AndroidPlugin.OnInstanceCreated += AndroidPlugin_OnInstanceCreated;
                return;
            }

            if (!isInitialized)
            {
                InitializeRecording();
            }
        }

        private void AndroidPlugin_OnInstanceCreated()
        {
            AndroidPlugin.OnInstanceCreated -= AndroidPlugin_OnInstanceCreated;
            InitializeRecording();
        }

        /// <summary>
        /// Initializes the Android audio codec for recording
        /// </summary>
        private void InitializeRecording()
        {
            try
            {
                AndroidPlugin.Instance.Call("initCodec", audioChannelName);
                isInitialized = true;
                Cognitive3D_Manager.SetSessionProperty("c3d.device.audio_tracking.enabled", true);

                // Reset silence tracking
                silenceDuration = 0f;
                hasActiveAudio = false;
            }
            catch (System.Exception e)
            {
                Util.logError($"AudioCapture ({audioChannelName}): Failed to initialize Android codec - {e.Message}");
            }
        }

        /// <summary>
        /// Called automatically by Unity's audio system for each audio frame
        /// </summary>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (!isInitialized) return;

            // Convert to mono if stereo input using AudioUtil
            float[] processedData = (channels == 2 && CHANNELS == 1)
                ? AudioUtil.ConvertStereoToMono(data)
                : (float[])data.Clone();

            // Check if audio contains significant signal
            bool hasSignal = HasAudioSignal(processedData);

            // Send 48kHz data directly - no downsampling
            if (processedData != null && processedData.Length > 0)
            {
                audioQueue.Enqueue(new AudioData
                {
                    samples = processedData,
                    timestamp = 0 // Will be set in Update()
                });
            }

            // Update silence tracking
            UpdateSilenceTracking(hasSignal);
        }

        private void Update()
        {
            if (!isInitialized) return;

            // Cache delta time for audio thread access
            cachedDeltaTime = Time.unscaledDeltaTime;

            long currentTime = GetCurrentTimeMs();

            while (audioQueue.TryDequeue(out var audioData))
            {
                ProcessAudioData(audioData.samples, currentTime);
            }

            // Check if we should flush due to prolonged silence
            CheckForSilenceFlush(currentTime);
        }

        private void ProcessAudioData(float[] samples, long timestamp)
        {
            int offset = 0;

            while (offset < samples.Length)
            {
                int samplesToProcess = Mathf.Min(AAC_FRAME_SIZE, samples.Length - offset);
                byte[] chunkBuffer = new byte[samplesToProcess * BYTES_PER_SAMPLE];

                // Convert to PCM16 using AudioUtil
                int bytesWritten = AudioUtil.ConvertToPCM16(samples, offset, samplesToProcess, chunkBuffer);

                // Convert byte[] to sbyte[] for Android JNI
                sbyte[] signedBuffer = new sbyte[bytesWritten];
                Buffer.BlockCopy(chunkBuffer, 0, signedBuffer, 0, bytesWritten);

                // Send 48kHz PCM data to Android
                try
                {
                    AndroidPlugin.Instance?.Call("handlePCM", signedBuffer, audioChannelName, bytesWritten, timestamp);
                }
                catch (System.Exception e)
                {
                    Util.logError($"AudioCapture ({audioChannelName}): Failed to send PCM data - {e.Message}");
                }

                offset += samplesToProcess;
            }
        }

        private long GetCurrentTimeMs()
        {
            return (long)Util.TimestampMS();
        }

        private bool HasAudioSignal(float[] samples)
        {
            if (samples == null || samples.Length == 0) return false;

            // Check if any sample exceeds the silence threshold
            for (int i = 0; i < samples.Length; i++)
            {
                if (Mathf.Abs(samples[i]) > silenceThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateSilenceTracking(bool hasSignal)
        {
            if (hasSignal)
            {
                silenceDuration = 0f;
                hasActiveAudio = true;
            }
            else if (hasActiveAudio)
            {
                silenceDuration += cachedDeltaTime;
            }
        }

        private void CheckForSilenceFlush(long currentTime)
        {
            if (hasActiveAudio && silenceDuration >= maxSilenceBeforeFlush)
            {
                // We've had active audio but now silence for too long - flush
                FlushRecording(currentTime);
                hasActiveAudio = false;
                silenceDuration = 0f;
            }
        }

        private void FlushRecording(long timestamp)
        {
            if (isInitialized && AndroidPlugin.Instance != null)
            {
                try
                {
                    AndroidPlugin.Instance.Call("finalizeRecording", audioChannelName, timestamp);
                }
                catch (System.Exception e)
                {
                    Util.logError($"AudioCapture ({audioChannelName}): Failed to flush recording - {e.Message}");
                }
            }
        }

        private void OnDestroy()
        {
            if (isInitialized && AndroidPlugin.Instance != null)
            {
                try
                {
                    AndroidPlugin.Instance.Call("finalizeRecording", audioChannelName, GetCurrentTimeMs());
                    AndroidPlugin.Instance.Call("cleanup", audioChannelName);
                }
                catch (System.Exception e)
                {
                    Util.logError($"AudioCapture ({audioChannelName}): Failed to cleanup - {e.Message}");
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // App is being paused (headset going to sleep)
                if (isInitialized)
                {
                    wasRecordingBeforePause = true;
                    try
                    {
                        // Finalize current recording session
                        AndroidPlugin.Instance?.Call("finalizeRecording", audioChannelName, GetCurrentTimeMs());
                        AndroidPlugin.Instance?.Call("cleanup", audioChannelName);
                        isInitialized = false;
                    }
                    catch (System.Exception e)
                    {
                        Util.logError($"AudioCapture ({audioChannelName}): Failed to finalize on pause - {e.Message}");
                    }
                }
                else
                {
                    wasRecordingBeforePause = false;
                }
            }
            else
            {
                // App is being resumed (headset waking up)
                if (wasRecordingBeforePause && AndroidPlugin.Instance != null)
                {
                    // Wait a frame to ensure Unity is fully resumed
                    StartCoroutine(ResumeRecordingCoroutine());
                }
            }
        }

        private System.Collections.IEnumerator ResumeRecordingCoroutine()
        {
            // Wait one frame to ensure Unity audio system is ready
            yield return null;

            try
            {
                // Clear any queued audio data from before pause
                while (audioQueue.TryDequeue(out _)) { }

                // Reinitialize recording
                InitializeRecording();
            }
            catch (System.Exception e)
            {
                Util.logError($"AudioCapture ({audioChannelName}): Failed to resume recording - {e.Message}");
            }
        }

        // Public method to manually stop/start recording
        public void StopRecording()
        {
            if (isInitialized)
            {
                try
                {
                    AndroidPlugin.Instance?.Call("finalizeRecording", audioChannelName, GetCurrentTimeMs());
                    AndroidPlugin.Instance?.Call("cleanup", audioChannelName);
                    isInitialized = false;
                }
                catch (System.Exception e)
                {
                    Util.logError($"AudioCapture ({audioChannelName}): Failed to stop recording - {e.Message}");
                }
            }
        }

        public void StartRecording()
        {
            if (!isInitialized && AndroidPlugin.Instance != null)
            {
                InitializeRecording();
            }
        }
#endif

        /// <summary>
        /// Description to display in inspector
        /// </summary>
        /// <returns> A string representing the description </returns>
        public override string GetDescription()
        {
#if UNITY_ANDROID
    return "Captures game audio via the audio source component. This feature is unavailable when running in the Unity Editor.";
#else
    return "The game audio recorder is supported only when the build target is set to Android.";
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
