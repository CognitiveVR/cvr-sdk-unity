using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cognitive3D;

/// <summary>
/// Sends Frames Per Second (FPS) as a sensor
/// </summary>


//TODO research and consider Coefficient of Variation
namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Frame Rate")]
    public class Framerate : AnalyticsComponentBase
    {
        readonly float FramerateTrackingInterval = 1;
        //counts up the deltatime to determine when the interval ends
        private float currentTime;
        //the number of frames in the interval
        private int intervalFrameCount;

        readonly List<float> deltaTimes = new List<float>(120);
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
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
            deltaTimes.Add(Time.unscaledDeltaTime);
            if (currentTime > FramerateTrackingInterval)
            {
                IntervalEnd();
            }
        }

        void IntervalEnd()
        {
            float framesPerSecond = intervalFrameCount / currentTime;

            int lowerCount5Percent = Mathf.CeilToInt(deltaTimes.Count * 0.05f);
            int lowerCount1Percent = Mathf.CeilToInt(deltaTimes.Count * 0.01f);

            deltaTimes.Sort();
            deltaTimes.Reverse();

            float lowerTotal5Percent = 0;
            for (int i = 0; i < lowerCount5Percent; i++)
            {
                lowerTotal5Percent += deltaTimes[i];
            }
            float lowerTotal1Percent = 0;
            for (int i = 0; i < lowerCount1Percent; i++)
            {
                lowerTotal1Percent += deltaTimes[i];
            }

            float min5Percent = lowerTotal5Percent / (float)lowerCount5Percent;
            float min1Percent = lowerTotal1Percent / (float)lowerCount1Percent;
            float finalLow5Percent = 1.0f / min5Percent;
            float finalLow1Percent = 1.0f / min1Percent;

            SensorRecorder.RecordDataPoint("c3d.fps.avg", framesPerSecond);
            SensorRecorder.RecordDataPoint("c3d.fps.5pl", finalLow5Percent);
            SensorRecorder.RecordDataPoint("c3d.fps.1pl", finalLow1Percent);

            intervalFrameCount = 0;
            currentTime = 0;
            deltaTimes.Clear();
        }

        public override string GetDescription()
        {
            return "Record Frames per Second (FPS) as a sensor, including average, 1% lows and 5% lows";
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
