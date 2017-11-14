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

    public class Comfort : CognitiveVRAnalyticsComponent
    {
        [DisplaySetting(5f,60f)]
        [Tooltip("Number of seconds used to average to determine comfort level. Lower means more smaller samples and more detail")]
        public float ComfortTrackingInterval = 6;

        [DisplaySetting]
        [Tooltip("Ignore sending Comfort at set intervals. Only send FPS events below the threshold")]
        public bool OnlySendComfortOnLowFPS = true;

        [DisplaySetting(10,240)]
        [Tooltip("Falling below and rising above this threshold will send events")]
        public int LowFramerateThreshold = 60;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_OnUpdate;
            timeleft = ComfortTrackingInterval;
            if (CognitiveVR_Manager.HMD != null)
                lastRotation = CognitiveVR_Manager.HMD.rotation;
        }

        private void CognitiveVR_Manager_OnUpdate()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            UpdateFramerate();
            if (OnlySendComfortOnLowFPS == false)
            {
                UpdateHMDRotation();
            }

            timeleft -= Time.deltaTime;
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
        string fpsTransactionID;
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
            accumRotation += Quaternion.Angle(CognitiveVR_Manager.HMD.rotation, lastRotation) / Time.deltaTime;
            lastRotation = CognitiveVR_Manager.HMD.rotation;
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
                fpsTransactionID = Util.GetUniqueId();
                Instrumentation.Transaction("cvr.performance", fpsTransactionID).setProperty("fps", lastFps).begin();
                Util.logDebug("low framerate");
            }
            else if (lastFps > LowFramerateThreshold && lowFramerate)
            {
                lowFramerate = false;
                Instrumentation.Transaction("cvr.performance", fpsTransactionID).end();
            }

            if (OnlySendComfortOnLowFPS) { return; }

            lastRps = accumRotation / rotFrames;
            accumRotation = 0.0F;
            rotFrames = 0;

            Instrumentation.Transaction("cvr.comfort", fpsTransactionID)
                .setProperty("fps", lastFps)
                .setProperty("rps", lastRps)
#if CVR_OCULUS
                    .setProperty("cpulevel", OVRPlugin.cpuLevel)
                    .setProperty("gpulevel", OVRPlugin.gpuLevel)
                    .setProperty("powersaving", OVRPlugin.powerSaving)
#endif
                    .beginAndEnd();
            Util.logDebug("comfort fps " + lastFps + " rps " + lastRps);
        }

        public static string GetDescription()
        {
            return "Sends transaction when framerate falls below a threshold\nSends comfort score (FPS + Average HMD rotation rate)\nWith Oculus Utilities, includes cpu and gpu levels and device power saving mode";
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
        }
    }
}