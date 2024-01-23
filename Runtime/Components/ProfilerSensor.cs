using Cognitive3D.Components;
using Unity.Profiling;
using UnityEngine;

namespace Cognitive3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Profiler Sensor")]
    public class ProfilerSensor : AnalyticsComponentBase
    {
        // This API doesn't exist for lower versions
        // https://docs.unity3d.com/ScriptReference/Unity.Profiling.ProfilerRecorder.html
#if UNITY_2020_2_OR_NEWER
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder systemMemoryRecorder;
        private ProfilerRecorder mainThreadTimeRecorder;

        private readonly string TOTAL_USED_MEMORY = "Total Used Memory";
        private readonly string MAIN_THREAD_TIME = "Main Thread";
        private readonly string DRAW_CALLS_COUNT = "Draw Calls Count";

        readonly float ProfilerSensorRecordingInterval = 1.0f;
        float currentTime = 0;

        private readonly float NANOSECOND_TO_MILLISECOND_MULTIPLIER = 1e-6f;
        private readonly float BYTES_TO_MEGABYTES_DIVIDER = 1024 * 1024;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();

            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, TOTAL_USED_MEMORY);
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, MAIN_THREAD_TIME);
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, DRAW_CALLS_COUNT);

            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
                // We don't want these lines to execute if component disabled
                // Without this condition, these lines will execute regardless
                //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()  
                if (isActiveAndEnabled)
                {
                    currentTime += deltaTime;

                    // Send data at 1Hz
                    if (currentTime > ProfilerSensorRecordingInterval)
                    {
                        currentTime = 0;
                        // number of draw calls as count
                        long drawCalls = drawCallsRecorder.LastValue;

                        // memory usage in bytes, we are converting to MB
                        // casting to float to handle decimal places
                        float systemMemory = (float)systemMemoryRecorder.LastValue / BYTES_TO_MEGABYTES_DIVIDER;

                        // thread time in nanoseconds, we are converting to milliseconds
                        // casting to float to handle decimal places
                        float mainThreadTime = (float)mainThreadTimeRecorder.LastValue * NANOSECOND_TO_MILLISECOND_MULTIPLIER;

                        SensorRecorder.RecordDataPoint("c3d.profiler.drawCallsCount", drawCalls);
                        SensorRecorder.RecordDataPoint("c3d.profiler.systemMemoryInMB", systemMemory);
                        SensorRecorder.RecordDataPoint("c3d.profiler.mainThreadTimeInMs", mainThreadTime);
                    }
                }
                else
                {
                    Debug.LogWarning("Profiler Sensor component is disabled. Please enable in inspector.");
                }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            systemMemoryRecorder.Dispose();
            mainThreadTimeRecorder.Dispose();
            drawCallsRecorder.Dispose();

            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

        #region Inspector Utils

        public override string GetDescription()
        {
#if UNITY_2020_2_OR_NEWER
            return "Sends sensor data points for number of Draw Calls, System Memory Usage, and Main Thread Time";
#else
            return "This component requires Unity 2020.2 or newer.";
#endif
        }

        public override bool GetWarning()
        {
#if UNITY_2020_2_OR_NEWER
            return false;
#else
            return true;
#endif
        }

#endregion
    }
}
