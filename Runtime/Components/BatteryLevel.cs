using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cognitive3D;

/// <summary>
/// WARNING - NOT FULLY TESTED!
/// 
/// send battery level of mobile device post initialization and on quit
/// on unsupported platforms (pc, laptop, vive, iOS) does not send battery level
/// </summary>

//TODO add picovr sdk Pvr_UnitySDKAPI.System.UPvr_GetHmdBatteryStatus()
//SystemInfo.batteryLevel works. returns -1 for invalid systems
//could also check left/right hand battery level with InputDevice.TryGetFeature commonusage.batteryLevel

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : AnalyticsComponentBase
    {
        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            SendBatteryLevel();
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnQuit;
        }

        void Cognitive3D_Manager_OnQuit()
        {
            SendBatteryLevel();
        }

        void SendBatteryLevel()
        {
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                new CustomEvent("cvr.battery").SetProperty("batterylevel", SystemInfo.batteryLevel * 100).Send();
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

        void OnDestroy()
        {
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnQuit;
        }
    }
}