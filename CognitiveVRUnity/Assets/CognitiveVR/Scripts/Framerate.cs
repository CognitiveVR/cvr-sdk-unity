using UnityEngine;
using System.Collections;
using CognitiveVR;

/// <summary>
/// sends fps as a sensor
/// </summary>

namespace CognitiveVR.Components
{
    public class Framerate : CognitiveVRAnalyticsComponent
    {
        [DisplaySetting(0.1f,10f)]
        [Tooltip("Number of seconds used to average to determine framerate. Lower means more smaller samples and more detail")]
        public float FramerateTrackingInterval = 1;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_OnUpdate;
            timeleft = FramerateTrackingInterval;
        }

        private void CognitiveVR_Manager_OnUpdate()
        {
            if (CognitiveVR_Manager.HMD == null) { return; }
            UpdateFramerate();

            timeleft -= Time.deltaTime;
            if (timeleft <= 0.0f)
            {
                IntervalEnd();
                timeleft = FramerateTrackingInterval;
            }
        }

        float timeleft;
        float accum;
        int frames;
        void UpdateFramerate()
        {
            accum += Time.timeScale / Time.deltaTime;
            ++frames;
        }

        void IntervalEnd()
        {
            // Interval ended - update GUI text and start new interval
            float lastFps = accum / frames;
            SensorRecorder.RecordDataPoint("FPS", lastFps);
        }

        public static string GetDescription()
        {
            return "Display framerate on SceneExplorer over time.";
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
        }
    }
}