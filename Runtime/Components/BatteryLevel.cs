using UnityEngine;

/// <summary>
/// Sends HMD Battery level as a sensor at a set interval
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : AnalyticsComponentBase
    {

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN

        private float lastDataTimestamp;
        private const float sendInterval = 1.0f;

        protected override void OnSessionBegin()
        {
            SendBatteryLevel();
            lastDataTimestamp = Time.time;
        }

        private void Update()
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Time.time >= lastDataTimestamp + sendInterval)
            {
                SendBatteryLevel();
                lastDataTimestamp = Time.time;
            }
        }
#endif
        void SendBatteryLevel()
        {
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                if (SystemInfo.batteryLevel != -1)
                {
                    Cognitive3D.SensorRecorder.RecordDataPoint("HMD Battery Level", SystemInfo.batteryLevel * 100);
                }
            }
        }

        public override bool GetWarning()
        {
#if UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }

        public override string GetDescription()
        {
#if UNITY_ANDROID
            return "Sends the battery level of Android device on an interval as a sensor";
#else
            return "Current platform does not support this component. Must be set to Android";
#endif
        }
    }
}
