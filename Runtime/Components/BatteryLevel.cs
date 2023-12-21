using UnityEngine;

/// <summary>
/// Sends HMD Battery level as a sensor at a set interval
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : AnalyticsComponentBase
    {
        private BatteryStatus batteryStatus;
#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        private float currentTime = 0;
        private readonly float BatteryLevelSendInterval = 10.0f;

        protected override void OnSessionBegin()
        {   
            batteryStatus = SystemInfo.batteryStatus;
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
                if (!Cognitive3D_Manager.IsInitialized) { return; }
                if (currentTime > BatteryLevelSendInterval)
                {
                    currentTime = 0;
                    SendBatteryLevel();
                    SendBatteryStatus();
                }
            }
            else
            {
                Debug.LogWarning("Battery Level component is disabled. Please enable in inspector.");
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

#if !UNITY_EDITOR && !UNITY_STANDALONE_WIN
        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
#endif

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
