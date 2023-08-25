using UnityEngine;

/// <summary>
/// Sends HMD Battery level as a sensor at a set interval
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : AnalyticsComponentBase
    {
        private BatteryStatus batteryStatus;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        private float lastDataTimestamp;
        private const float sendInterval = 1.0f;

        protected override void OnSessionBegin()
        {   
            batteryStatus = SystemInfo.batteryStatus;
            SendBatteryLevel();
            lastDataTimestamp = Time.time;
        }

        private void Update()
        {
            if (!Cognitive3D_Manager.IsInitialized) { return; }
            if (Time.time >= lastDataTimestamp + sendInterval)
            {
                SendBatteryLevel();
                SendBatteryStatus();
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

        void SendBatteryStatus()
        {
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                BatteryStatus currentStatus = SystemInfo.batteryStatus;
                if (currentStatus != batteryStatus)
                {
                    new CustomEvent("c3d.battery status changed")
                        .SetProperty("Previous Battery Status", batteryStatus.ToString())
                        .SetProperty("New Battery Status", currentStatus.ToString())
                        .Send();
                    batteryStatus = currentStatus;
                }
                Cognitive3D.SensorRecorder.RecordDataPoint("HMD Battery Status", (int)currentStatus);
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
            return "Sends the battery level and charging status of Android device on an interval as a sensor";
#else
            return "Current platform does not support this component. Must be set to Android";
#endif
        }
    }
}
