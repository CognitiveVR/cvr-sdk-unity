using UnityEngine;
using System.Collections;

/// <summary>
/// sends low framerate transaction
/// sends comfort score (fps + average hmd rotation)
/// </summary>

namespace CognitiveVR
{
    /// CPU performance level (int 0-2). Lower performance levels save more power.
    /// GPU performance level (int 0-2). Lower performance levels save more power.
    /// PowerSaving? The CPU and GPU are currently throttled to save power and/or reduce the temperature.

    public class ComfortTracker : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.OnUpdate += CognitiveVR_Manager_OnUpdate;
            updateInterval = CognitiveVR_Preferences.Instance.ComfortTrackingInterval;
            timeleft = updateInterval;
            if (CognitiveVR_Manager.HMD != null)
                lastRotation = CognitiveVR_Manager.HMD.rotation;
        }

        private void CognitiveVR_Manager_OnUpdate()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            UpdateFramerate();
            if (!CognitiveVR_Preferences.Instance.OnlySendComfortOnLowFPS)
            {
                UpdateHMDRotation();
            }

            timeleft -= Time.deltaTime;
            if (timeleft <= 0.0f)
            {
                IntervalEnd();
                timeleft = updateInterval;
            }
        }

        float updateInterval = 6;

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

            if (lastFps < CognitiveVR.CognitiveVR_Preferences.Instance.LowFramerateThreshold && !lowFramerate)
            {
                lowFramerate = true;
                fpsTransactionID = System.Guid.NewGuid().ToString();
                Instrumentation.Transaction("cvr.performance", fpsTransactionID).setProperty("fps", lastFps).begin();
                Util.logDebug("low framerate");
            }
            else if (lastFps > CognitiveVR.CognitiveVR_Preferences.Instance.LowFramerateThreshold && lowFramerate)
            {
                lowFramerate = false;
                Instrumentation.Transaction("cvr.performance", fpsTransactionID).end();
            }

            if (CognitiveVR_Preferences.Instance.OnlySendComfortOnLowFPS) { return; }

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
    }
}