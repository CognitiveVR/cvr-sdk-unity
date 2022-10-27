using UnityEngine;
using System.Collections;
using Cognitive3D;

/// <summary>
/// sends fps as a sensor
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Frame Rate")]
    public class Framerate : AnalyticsComponentBase
    {
        [ClampSetting(0.1f,10f)]
        [Tooltip("Number of seconds used to average to determine framerate. Lower means more smaller samples and more detail")]
        public float FramerateTrackingInterval = 1;

        public override void Cognitive3D_Init()
        {
            //if (initError != Error.None) { return; }
            base.Cognitive3D_Init();
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete)
#endif            
            {
                Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            }
            timeleft = FramerateTrackingInterval;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
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
            if (frames != 0)
            {
                float lastFps = accum / frames;
                SensorRecorder.RecordDataPoint("FPS", lastFps);
            }
            else
            {
                Util.logError("Framerate interval ended with 0 frames!");
            }
        }

        public override string GetDescription()
        {
            return "Record framerate over time as a sensor";
        }

        void OnDestroy()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
        }
    }
}