using UnityEngine;
using System.Collections;

/// <summary>
/// sends low framerate transaction
/// sends comfort score (fps + average hmd rotation)
/// </summary>

namespace CognitiveVR
{
    public class ComfortTracker : CognitiveVRAnalyticsComponent
    {
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.OnUpdate += CognitiveVR_Manager_OnUpdate;
        }

        private void CognitiveVR_Manager_OnUpdate()
        {
            UpdateFramerate();
            UpdateHMDRotation();
        }

        float updateInterval = 3;

        float timeleft;
        float accum;
        int frames;
        bool lowFramerate;
        string fpsTransactionID;
        float lastFps;
        void UpdateFramerate()
        {
            timeleft -= Time.deltaTime;
            accum += Time.timeScale / Time.deltaTime;
            ++frames;

            // Interval ended - update GUI text and start new interval
            if (timeleft <= 0.0)
            {
                lastFps = accum / frames;
                timeleft = updateInterval;
                accum = 0.0F;
                frames = 0;

                if (lastFps < CognitiveVR.CognitiveVR_Preferences.Instance.LowFramerateThreshold && !lowFramerate)
                {
                    lowFramerate = true;
                    fpsTransactionID = System.Guid.NewGuid().ToString();
                    Instrumentation.Transaction("performance", fpsTransactionID).setProperty("fps", lastFps).begin();
                    Util.logDebug("low framerate");
                }
                else if (lastFps > CognitiveVR.CognitiveVR_Preferences.Instance.LowFramerateThreshold && lowFramerate)
                {
                    lowFramerate = false;
                    Instrumentation.Transaction("performance", fpsTransactionID).end();
                }
            }
        }

        Quaternion lastRotation;
        float accumRotation;
        float rotTimeLeft;
        int rotFrames;
        float lastRps;

        void UpdateHMDRotation()
        {
            rotTimeLeft -= Time.deltaTime;
            accumRotation += Quaternion.Angle(CognitiveVR_Manager.HMD.rotation, lastRotation) / Time.deltaTime;
            lastRotation = CognitiveVR_Manager.HMD.rotation;
            ++rotFrames;

            // Interval ended - update GUI text and start new interval
            if (rotTimeLeft <= 0.0)
            {
                lastRps = accumRotation / rotFrames;
                rotTimeLeft = updateInterval;
                accumRotation = 0.0F;
                rotFrames = 0;
                
                Instrumentation.Transaction("comfort", fpsTransactionID).setProperty("fps", lastFps).setProperty("rps", lastRps).begin();
                Util.logDebug("comfort fps " + lastFps + " rps " + lastRps);
            }
        }

        public static string GetDescription()
        {
            return "Sends transaction when framerate falls below a threshold\nSends comfort score (FPS + Average HMD rotation rate)";
        }
    }
}