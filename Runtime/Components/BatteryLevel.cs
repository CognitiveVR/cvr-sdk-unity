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
        public override void Cognitive3D_Init()
        {
            base.Cognitive3D_Init();
            SendBatteryLevel();
            Cognitive3D_Manager.OnQuit += Cognitive3D_Manager_OnQuit;
        }

        void Cognitive3D_Manager_OnQuit()
        {
            SendBatteryLevel();
        }

        void SendBatteryLevel()
        {

#if C3D_OCULUS
            Util.logDebug("batterylevel " + OVRPlugin.batteryLevel);
            new CustomEvent("cvr.battery")
                .SetProperty("batterylevel", OVRPlugin.batteryLevel)
                .SetProperty("batterytemperature", OVRPlugin.batteryTemperature)
                .SetProperty("batterystatus", OVRPlugin.batteryStatus)
                .Send();
#else
            new CustomEvent("cvr.battery").SetProperty("batterylevel", GetBatteryLevel()).Send();
#endif
        }

#if !C3D_OCULUS
        public float GetBatteryLevel()
        {
            return SystemInfo.batteryLevel * 100;
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
#if UNITY_ANDROID && C3D_OCULUS
            return "Send the battery level of Android device after initialization and on quit\nAlso includes battery temperature and status";
#elif UNITY_ANDROID
            return "Send the battery level of Android device after initialization and on quit";
#else
            return "Current platform does not support this component. Must be set to Android";
#endif
        }

        void OnDestroy()
        {
            Cognitive3D_Manager.OnQuit -= Cognitive3D_Manager_OnQuit;
        }
    }
}