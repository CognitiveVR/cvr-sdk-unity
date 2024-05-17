using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sends Frames Per Second (FPS) as a sensor
/// </summary>


//TODO research and consider Coefficient of Variation
namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Frame Rate")]
    public class Framerate : AnalyticsComponentBase
    {
        readonly float FramerateTrackingInterval = 1;

        /// <summary>
        /// Counts up the deltatime to determine when the interval ends
        /// </summary>
        private float currentTime;

        /// <summary>
        /// The number of frames in the interval
        /// </summary>
        private int intervalFrameCount;

#if C3D_OCULUS
        /// <summary>
        /// ASW caps framerate to half of device refresh rate
        /// We are defining a +- tolerance for the capepd framerate to allow for error etc.
        /// </summary>
        private readonly float TOLERANCE_FOR_CAPPED_FPS = 2;
#endif

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
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()  
            if (isActiveAndEnabled)
            {
                intervalFrameCount++;
                currentTime += deltaTime;
                deltaTimes.Add(Time.unscaledDeltaTime);
                if (currentTime > FramerateTrackingInterval)
                {
                    IntervalEnd();
                }
            }
            else
            {
                Debug.LogWarning("Framerate component is disabled. Please enable in inspector.");
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

            float fpsMultiplier = 1;
#if C3D_OCULUS
            // We cannot do this once and cache since this can be enabled/disabled per frame
            if (OVRManager.GetSpaceWarp())
            {
                SensorRecorder.RecordDataPoint("c3d.app.meta.spaceWarp", 1);
                Cognitive3D_Manager.SetSessionProperty("c3d.app.meta.wasSpaceWarpUsed", true);
                // If FPS is approximately half of device refresh rate (plus tolerance)
                if (framesPerSecond <= (OVRPlugin.systemDisplayFrequency / 2) + TOLERANCE_FOR_CAPPED_FPS)
                {
                    fpsMultiplier = 2;
                }
            }
            else
            {
                SensorRecorder.RecordDataPoint("c3d.app.meta.spaceWarp", 0);
                fpsMultiplier = 1;
            }
#endif
            SensorRecorder.RecordDataPoint("c3d.fps.avg", framesPerSecond * fpsMultiplier);
            SensorRecorder.RecordDataPoint("c3d.fps.5pl", finalLow5Percent * fpsMultiplier);
            SensorRecorder.RecordDataPoint("c3d.fps.1pl", finalLow1Percent * fpsMultiplier);
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
