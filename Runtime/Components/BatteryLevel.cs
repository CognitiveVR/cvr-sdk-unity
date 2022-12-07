using UnityEngine;

/// <summary>
/// WARNING - NOT FULLY TESTED!
/// 
/// send battery level of mobile device post initialization and on quit
/// on unsupported platforms (pc, laptop, vive, iOS) does not send battery level
/// </summary>

//TODO add picovr sdk Pvr_UnitySDKAPI.System.UPvr_GetHmdBatteryStatus()

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : AnalyticsComponentBase
    {
        private float lastDataTimestamp;
        private const float sendInterval = 1.0f;

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        private void Start()
        {
            SendBatteryLevel();
            lastDataTimestamp = Time.time;
        }

        private void Update()
        {
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
            return "Send the battery level of Android device after initialization and on quit";
#else
            return "Current platform does not support this component. Must be set to Android";
#endif
        }
    }
}
