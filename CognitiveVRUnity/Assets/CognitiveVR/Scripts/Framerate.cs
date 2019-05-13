using UnityEngine;
using System.Collections;
using CognitiveVR;

/// <summary>
/// sends fps as a sensor
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Frame Rate")]
    public class Framerate : CognitiveVRAnalyticsComponent
    {
        [ClampSetting(0.1f,10f)]
        [Tooltip("Number of seconds used to average to determine framerate. Lower means more smaller samples and more detail")]
        public float FramerateTrackingInterval = 1;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            Core.UpdateEvent += CognitiveVR_Manager_OnUpdate;
            timeleft = FramerateTrackingInterval;
        }

        private void CognitiveVR_Manager_OnUpdate(float deltaTime)
        {
            UpdateFramerate();

            timeleft -= deltaTime;
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

        public override string GetDescription()
        {
            return "Record framerate over time as a sensor";
        }

        void OnDestroy()
        {
            Core.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
        }
    }
}