using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;

/// <summary>
/// WARNING - NOT FULLY TESTED!
/// 
/// send battery level of mobile device post initialization and on quit
/// on unsupported platforms (pc, laptop, vive, iOS) does not send battery level
/// </summary>

namespace CognitiveVR.Components
{
    [AddComponentMenu("Cognitive3D/Components/Battery Level")]
    public class BatteryLevel : CognitiveVRAnalyticsComponent
    {
#if !CVR_OCULUS
        float batteryLevel; //0-100 battery level
#endif
        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.None) { return; }
            base.CognitiveVR_Init(initError);
            SendBatteryLevel();
            Core.QuitEvent += CognitiveVR_Manager_OnQuit;
        }

        void CognitiveVR_Manager_OnQuit()
        {
            SendBatteryLevel();
        }

        void SendBatteryLevel()
        {

#if CVR_OCULUS
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

#if !CVR_OCULUS
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
#if UNITY_ANDROID && CVR_OCULUS
            return "Send the battery level of Android device after initialization and on quit\nAlso includes battery temperature and status";
#elif UNITY_ANDROID
            return "Send the battery level of Android device after initialization and on quit";
#else
            return "Current platform does not support this component. Must be set to Android";
#endif
        }

        void OnDestroy()
        {
            Core.QuitEvent -= CognitiveVR_Manager_OnQuit;
        }
    }
}