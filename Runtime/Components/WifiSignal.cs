using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D.Components;

namespace Cognitive3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Cognitive3D_WifiSignal")]
    public class WifiSignal : AnalyticsComponentBase
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        int prevRSSI;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnTick += Cognitive3D_Manager_OnTick;
        }

        private void Cognitive3D_Manager_OnTick()
        {
            if (AndroidPlugin.isInitialized && AndroidPlugin.plugininstance != null)
            {
                var currentRSSI = AndroidPlugin.plugininstance.Call<int>("getWifiSignalStrength");

                // Sends sensor data on change
                if (currentRSSI != prevRSSI)
                {
                    SensorRecorder.RecordDataPoint("WifiRSSI", currentRSSI);
                    prevRSSI = currentRSSI;
                }
            }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnTick -= Cognitive3D_Manager_OnTick;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

public override string GetDescription()
        {
#if UNITY_ANDROID
            return "Records Wi-Fi RSSI on Android devices, providing the received signal strength in dBm to indicate Wi-Fi connection quality. It is not functional during Unity Editor sessions.";
#else
            return "Wi-Fi signal strength sensor can only be accessed when using the Android platform";
#endif
        }
        
        public override bool GetWarning()
        {
#if UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }
    }
}
