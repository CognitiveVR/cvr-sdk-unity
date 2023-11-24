using Cognitive3D.Components;
using Unity.Profiling;

namespace Cognitive3D
{
    public class ProfilerSensor : AnalyticsComponentBase
    {
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder systemMemoryRecorder;
        private ProfilerRecorder mainThreadTimeRecorder;
        private readonly string TOTAL_USED_MEMORY = "Total Used Memory";
        private readonly string MAIN_THREAD_TIME = "Main Thread";
        private readonly string DRAW_CALLS_COUNT = "Draw Calls Count";

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
            // number of draw calls as count
            long drawCalls = drawCallsRecorder.LastValue;

            // memory usage in bytes, we are converting to MB
            long systemMemory = (systemMemoryRecorder.LastValue / (1024 * 1024));

            // thread time in nanoseconds, we are converting to milliseconds
            long mainThreadTime = mainThreadTimeRecorder.LastValue * (long) (1e-6);

            SensorRecorder.RecordDataPoint("c3d.profiler.drawCalls", drawCalls);
            SensorRecorder.RecordDataPoint("c3d.profiler.systemMemory", systemMemory);
            SensorRecorder.RecordDataPoint("c3d.profiler.mainThreadTime", mainThreadTime);
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            systemMemoryRecorder.Dispose();
            mainThreadTimeRecorder.Dispose();
            drawCallsRecorder.Dispose();
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }

#region Inspector Utils
        public override string GetDescription()
        {
            return "Sends sensor data points for number of Draw Calls, System Memory Usage, and Main Thread Time";
        }
#endregion

    }
}
