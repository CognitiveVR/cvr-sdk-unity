using UnityEngine;
using System.Collections;
using Cognitive3D;

/// <summary>
/// Sends Frames Per Second (FPS) as a sensor
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Frame Rate")]
    public class Framerate : AnalyticsComponentBase
    {
        [ClampSetting(0.1f,10f)]
        [Tooltip("Number of seconds used to average to determine framerate. Lower means more smaller samples and more detail")]
        public float FramerateTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
        //the number of frames in the interval
        private int intervalFrameCount;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete)
#endif            
            {
                Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
                Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            }
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            intervalFrameCount++;
            currentTime += deltaTime;
            if (currentTime > FramerateTrackingInterval)
            {
                IntervalEnd();
            }
        }

        void IntervalEnd()
        {
            float framesPerSecond = intervalFrameCount / currentTime;
            SensorRecorder.RecordDataPoint("FPS", framesPerSecond);
            intervalFrameCount = 0;
            currentTime = 0;
        }

        public override string GetDescription()
        {
            return "Record Frames per Second (FPS) as a sensor";
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

        void OnDestroy()
        {
            Cognitive3D_Manager_OnPreSessionEnd();
        }
    }
}