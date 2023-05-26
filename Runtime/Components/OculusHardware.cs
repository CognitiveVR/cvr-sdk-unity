using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if C3D_OCULUS
using Unity.XR.Oculus;
#endif

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Oculus Hardware")]
    public class OculusHardware : AnalyticsComponentBase
    {
#if C3D_OCULUS
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            StartCoroutine(OneSecondTick());
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
        }

        IEnumerator OneSecondTick()
        {
            var wait = new WaitForSeconds(1);
            while (Cognitive3D.Cognitive3D_Manager.IsInitialized)
            {
                yield return wait;
                RecordOculusStats();
            }
        }

        void RecordOculusStats()
        {            
            //battery level handled by a different component
            Cognitive3D.SensorRecorder.RecordDataPoint("c3d.battery.temp", Stats.AdaptivePerformance.BatteryTemp);
            Cognitive3D.SensorRecorder.RecordDataPoint("c3d.cpuLevel", Stats.AdaptivePerformance.CPULevel);
            Cognitive3D.SensorRecorder.RecordDataPoint("c3d.gpuLevel", Stats.AdaptivePerformance.GPULevel);
            Cognitive3D.SensorRecorder.RecordDataPoint("c3d.isPowerSavingMode", (Stats.AdaptivePerformance.PowerSavingMode ? 1 : 0));
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            StopAllCoroutines();
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

        public override string GetDescription()
        {
#if C3D_OCULUS
            return "Records Battery level, CPU level, GPU level and Power Saving mode states";
#else
            return "Oculus Hardware senosrs can only be accessed when using the Oculus Integration SDK";
#endif
        }
        public override bool GetWarning()
        {
#if C3D_OCULUS
            return false;
#else
            return true;
#endif
        }
    }

}