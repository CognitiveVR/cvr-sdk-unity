using System;
using UnityEngine;
using System.Collections.Concurrent;

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/AppAudioRecorder")]
    public class AppAudioRecorder : AnalyticsComponentBase
    {
        public string audioChannelName = "default";

#if UNITY_ANDROID && !UNITY_EDITOR
        // Audio settings
        private const int CHANNELS = 1; // Mono
        private const int AAC_FRAME_SIZE = 1024;
        private const int BYTES_PER_SAMPLE = 2; // 16-bit PCM

        private bool isInitialized = false;
        private bool wasRecordingBeforePause = false;

        // Audio data queue for thread safety
        private ConcurrentQueue<AudioData> audioQueue = new ConcurrentQueue<AudioData>();

        private struct AudioData
        {
            public float[] samples;
            public long timestamp;
        }

        private void Awake()
        {
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
#if XRPF
            if (!XRPF.PrivacyFramework.Agreement.IsAudioDataAllowed)
            {
                Cognitive3D_Manager.SetSessionProperty("c3d.device.audio_tracking.enabled", false);
                return;
            }
#endif
            try
            {
                AndroidPlugin.Instance.Call("initCodec", audioChannelName);
                isInitialized = true;
                Cognitive3D_Manager.SetSessionProperty("c3d.device.audio_tracking.enabled", true);
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

            // Convert to mono if stereo input
            float[] processedData = (channels == 2 && CHANNELS == 1)
                ? AudioUtil.ConvertStereoToMono(data)
                : (float[])data.Clone();

            if (processedData != null && processedData.Length > 0)
            {
                audioQueue.Enqueue(new AudioData
                {
                    samples = processedData,
                    timestamp = GetCurrentTimeMs()
                });
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            while (audioQueue.TryDequeue(out var audioData))
            {
                ProcessAudioData(audioData.samples, audioData.timestamp);
            }
        }

        private void ProcessAudioData(float[] samples, long timestamp)
        {
            int offset = 0;

            while (offset < samples.Length)
            {
                int samplesToProcess = Mathf.Min(AAC_FRAME_SIZE, samples.Length - offset);
                byte[] chunkBuffer = new byte[samplesToProcess * BYTES_PER_SAMPLE];

                int bytesWritten = AudioUtil.ConvertToPCM16(samples, offset, samplesToProcess, chunkBuffer);

                sbyte[] signedBuffer = new sbyte[bytesWritten];
                Buffer.BlockCopy(chunkBuffer, 0, signedBuffer, 0, bytesWritten);

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

        /// <summary>
        /// Flushes any remaining audio samples in the queue to Android before finalizing
        /// </summary>
        private void FlushRemainingAudio()
        {
            if (AndroidPlugin.Instance == null) return;

            long currentTime = GetCurrentTimeMs();
            while (audioQueue.TryDequeue(out var audioData))
            {
                ProcessAudioData(audioData.samples, currentTime);
            }
        }

        private void OnDestroy()
        {
            if (isInitialized && AndroidPlugin.Instance != null)
            {
                try
                {
                    // Flush remaining audio before finalizing
                    FlushRemainingAudio();
                    AndroidPlugin.Instance.Call("finalizeRecording", audioChannelName, GetCurrentTimeMs());
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
                if (isInitialized)
                {
                    wasRecordingBeforePause = true;
                    try
                    {
                        // Flush remaining audio before finalizing
                        FlushRemainingAudio();
                        AndroidPlugin.Instance?.Call("finalizeRecording", audioChannelName, GetCurrentTimeMs());
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
                if (wasRecordingBeforePause && AndroidPlugin.Instance != null)
                {
                    StartCoroutine(ResumeRecordingCoroutine());
                }
            }
        }

        private System.Collections.IEnumerator ResumeRecordingCoroutine()
        {
            yield return null;

            try
            {
                // Discard any audio captured between finalizeRecording() and full pause.
                // This audio is orphaned (no active codec session) and would have stale timestamps.
                // Valid audio was already flushed in OnApplicationPause(true) before finalization.
                while (audioQueue.TryDequeue(out _)) { }
                InitializeRecording();
            }
            catch (System.Exception e)
            {
                Util.logError($"AudioCapture ({audioChannelName}): Failed to resume recording - {e.Message}");
            }
        }

        public void StopRecording()
        {
            if (isInitialized)
            {
                try
                {
                    // Flush remaining audio before finalizing
                    FlushRemainingAudio();
                    AndroidPlugin.Instance?.Call("finalizeRecording", audioChannelName, GetCurrentTimeMs());
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

        public override string GetDescription()
        {
#if UNITY_ANDROID
            return "Captures game audio via the audio source component. This feature is unavailable when running in the Unity Editor.";
#else
            return "The game audio recorder is supported only when the build target is set to Android.";
#endif
        }

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
