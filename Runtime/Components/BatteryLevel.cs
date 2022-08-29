﻿using UnityEngine;
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

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : AnalyticsComponentBase
    {
#if !C3D_OCULUS
        float batteryLevel; //0-100 battery level
#endif
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

            if (GetBatteryLevel())
            {
                Util.logDebug("batterylevel " + batteryLevel);

                new CustomEvent("cvr.battery").SetProperty("batterylevel", batteryLevel).Send();
            }
#endif
        }

#if !C3D_OCULUS
        public bool GetBatteryLevel()
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        if (null != unityPlayer)
                        {
                            using (AndroidJavaObject currActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                            {
                                if (null != currActivity)
                                {
                                    using (AndroidJavaObject intentFilter = new AndroidJavaObject("android.content.IntentFilter", new object[] { "android.intent.action.BATTERY_CHANGED" }))
                                    {
                                        using (AndroidJavaObject batteryIntent = currActivity.Call<AndroidJavaObject>("registerReceiver", new object[] { null, intentFilter }))
                                        {
                                            int level = batteryIntent.Call<int>("getIntExtra", new object[] { "level", -1 });
                                            int scale = batteryIntent.Call<int>("getIntExtra", new object[] { "scale", -1 });

                                            // Error checking that probably isn't needed but I added just in case.
                                            if (level == -1 || scale == -1)
                                            {
                                                batteryLevel = 50f;
                                                return false;
                                            }
                                            batteryLevel = ((float)level / (float)scale) * 100.0f;
                                            return true;
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            batteryLevel = 100f;
            return false;
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