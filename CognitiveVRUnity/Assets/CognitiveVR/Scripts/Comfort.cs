using UnityEngine;
using System.Collections;

/// <summary>
/// sends low framerate transaction
/// sends comfort score (fps + average hmd rotation)
/// </summary>

namespace CognitiveVR.Components
{
    /// CPU performance level (int 0-2). Lower performance levels save more power.
    /// GPU performance level (int 0-2). Lower performance levels save more power.
    /// PowerSaving? The CPU and GPU are currently throttled to save power and/or reduce the temperature.

    [AddComponentMenu("Cognitive3D/Components/Comfort")]
    public class Comfort : CognitiveVRAnalyticsComponent
    {
        [ClampSetting(5f,60f)]
        [Tooltip("Number of seconds used to average to determine comfort level. Lower means more smaller samples and more detail")]
        public float ComfortTrackingInterval = 6;

        [Tooltip("Ignore sending Comfort at set intervals. Only send FPS events below the threshold")]
        public bool OnlySendComfortOnLowFPS = true;

        [ClampSetting(10,240)]
        [Tooltip("Falling below and rising above this threshold will send events")]
        public int LowFramerateThreshold = 60;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            Core.UpdateEvent += CognitiveVR_Manager_OnUpdate;
            timeleft = ComfortTrackingInterval;
            if (GameplayReferences.HMD != null)
                lastRotation = GameplayReferences.HMD.rotation;
        }

        private void CognitiveVR_Manager_OnUpdate(float deltaTime)
        {
            if (GameplayReferences.HMD == null) { return; }
            UpdateFramerate();
            if (OnlySendComfortOnLowFPS == false)
            {
                UpdateHMDRotation();
            }

            timeleft -= deltaTime;
            if (timeleft <= 0.0f)
            {
                IntervalEnd();
                timeleft = ComfortTrackingInterval;
            }
        }

        float timeleft;
        float accum;
        int frames;
        bool lowFramerate;
        float lastFps;
        void UpdateFramerate()
        {
            accum += Time.timeScale / Time.deltaTime;
            ++frames;
        }

        Quaternion lastRotation;
        float accumRotation;
        int rotFrames;
        float lastRps;
        void UpdateHMDRotation()
        {
            accumRotation += Quaternion.Angle(GameplayReferences.HMD.rotation, lastRotation) / Time.deltaTime;
            lastRotation = GameplayReferences.HMD.rotation;
            ++rotFrames;
        }

        void IntervalEnd()
        {
            // Interval ended - update GUI text and start new interval
            lastFps = accum / frames;
            accum = 0.0F;
            frames = 0;

            if (lastFps < LowFramerateThreshold && !lowFramerate)
            {
                lowFramerate = true;
                new CustomEvent("cvr.performance").SetProperty("fps", lastFps).Send();
                Util.logDebug("low framerate");
            }
            else if (lastFps > LowFramerateThreshold && lowFramerate)
            {
                lowFramerate = false;
                new CustomEvent("cvr.performance").Send();
            }

            if (OnlySendComfortOnLowFPS) { return; }

            lastRps = accumRotation / rotFrames;
            accumRotation = 0.0F;
            rotFrames = 0;

            new CustomEvent("cvr.comfort")
                .SetProperty("fps", lastFps)
                .SetProperty("rps", lastRps)
#if CVR_OCULUS
                    .SetProperty("cpulevel", OVRPlugin.cpuLevel)
                    .SetProperty("gpulevel", OVRPlugin.gpuLevel)
                    .SetProperty("powersaving", OVRPlugin.powerSaving)
#endif
                    .Send();
            Util.logDebug("comfort fps " + lastFps + " rps " + lastRps);
        }

        public override string GetDescription()
        {
#if CVR_OCULUS
            return "Sends transaction when framerate falls below a threshold\nSends comfort score (FPS + Average HMD rotation rate). Also includes cpu and gpu levels and device power saving mode";
#else
            return "Sends transaction when framerate falls below a threshold\nSends comfort score (FPS + Average HMD rotation rate)";
#endif
        }

        void OnDestroy()
        {
            Core.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
        }
    }
}