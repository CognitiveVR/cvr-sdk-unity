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
        int lastRSSI;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
        }

        private void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            if (AndroidPlugin.isInitialized && AndroidPlugin.plugininstance != null)
            {
                lastRSSI = AndroidPlugin.plugininstance.Call<int>("getWifiSignalStrength");

                if (lastRSSI != prevRSSI)
                {
                    SensorRecorder.RecordDataPoint("WifiRSSI", lastRSSI);
                    prevRSSI = lastRSSI;
                }
            }
        }

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif
    }
}
