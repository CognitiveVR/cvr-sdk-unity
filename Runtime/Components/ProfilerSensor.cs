using Cognitive3D.Components;
using Unity.Profiling;

namespace Cognitive3D
{
    public class ProfilerSensor : AnalyticsComponentBase
    {
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder systemMemoryRecorder;
        private ProfilerRecorder mainThreadTimeRecorder;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
            mainThreadTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            long drawCalls = drawCallsRecorder.LastValue;
            long systemMemory = systemMemoryRecorder.LastValue;
            long mainThreadTime = mainThreadTimeRecorder.LastValue;
            SensorRecorder.RecordDataPoint("c3d.profiler.drawCalls", drawCalls);
            SensorRecorder.RecordDataPoint("c3d.profiler.systemMemory", systemMemory);
            SensorRecorder.RecordDataPoint("c3d.profiler.mainThreadTime", mainThreadTime);
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
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
