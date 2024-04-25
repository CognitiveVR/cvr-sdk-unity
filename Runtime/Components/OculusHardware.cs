using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

#if C3D_OCULUS
using Unity.XR.Oculus;
#endif

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Oculus Hardware")]
    public class OculusHardware : AnalyticsComponentBase
    {
#if C3D_OCULUS
        XRDisplaySubsystem currentActiveSubsystem;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            StartCoroutine(OneSecondTick());
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
        }

        IEnumerator OneSecondTick()
        {
            var wait = new WaitForSeconds(1);
            yield return wait;

            currentActiveSubsystem = GetActiveDisplaySubsystem();
            if (currentActiveSubsystem != null && currentActiveSubsystem.SubsystemDescriptor.id.Contains("oculus"))
            {
                while (Cognitive3D.Cognitive3D_Manager.IsInitialized)
                {
                    yield return wait;
                    RecordOculusStats();
                }
            }
            else if (currentActiveSubsystem != null && currentActiveSubsystem.SubsystemDescriptor.id.Contains("OpenXR"))
            {
                Debug.LogWarning("Oculus Hardware sensors cannot be accessed while using OpenXR plugin");
            }
        }

        void RecordOculusStats()
        {
            try
            {
                //battery level handled by a different component
                Cognitive3D.SensorRecorder.RecordDataPoint("c3d.battery.temp", Stats.AdaptivePerformance.BatteryTemp);
                Cognitive3D.SensorRecorder.RecordDataPoint("c3d.cpuLevel", Stats.AdaptivePerformance.CPULevel);
                Cognitive3D.SensorRecorder.RecordDataPoint("c3d.gpuLevel", Stats.AdaptivePerformance.GPULevel);
                Cognitive3D.SensorRecorder.RecordDataPoint("c3d.isPowerSavingMode", (Stats.AdaptivePerformance.PowerSavingMode ? 1 : 0));
            }
            catch
            {
            }
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
            return "Oculus Hardware sensors can only be accessed when using the Oculus Integration SDK";
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

        private static XRDisplaySubsystem activeDisplay;

        private static XRDisplaySubsystem GetActiveDisplaySubsystem()
        {
            if (activeDisplay != null)
                return activeDisplay;

            List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();

            // GetSubsystems() doesn't exist for lower versions
            // https://docs.unity3d.com/ScriptReference/SubsystemManager.GetSubsystems.html
#if UNITY_2020_2_OR_NEWER
            SubsystemManager.GetSubsystems(displays);
#else
            SubsystemManager.GetInstances(displays);
#endif

            foreach (XRDisplaySubsystem xrDisplaySubsystem in displays)
            {
                if (xrDisplaySubsystem.running)
                {
                    activeDisplay = xrDisplaySubsystem;
                    return activeDisplay;
                }
            }

            Debug.LogError("No active display subsystem was found.");
            return activeDisplay;
        }
    }

}