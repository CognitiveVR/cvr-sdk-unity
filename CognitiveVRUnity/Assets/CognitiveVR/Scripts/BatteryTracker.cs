using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WARNING - NOT FULLY TESTED!
/// 
/// send battery level of mobile device post initialization and on quit
/// on unsupported platforms (pc, laptop, vive, iOS) does not send battery level
/// </summary>

namespace CognitiveVR
{
    public class BatteryTracker : CognitiveVRAnalyticsComponent
    {
        float batteryLevel; //0-100 battery level
        public override void CognitiveVR_Init(Error initError)
        {
            base.CognitiveVR_Init(initError);
            SendBatteryLevel();
            CognitiveVR_Manager.OnQuit += CognitiveVR_Manager_OnQuit;
        }

        void CognitiveVR_Manager_OnQuit()
        {
            SendBatteryLevel();
        }

        void SendBatteryLevel()
        {
            if (GetBatteryLevel())
            {
                Util.logDebug("batterylevel " + batteryLevel);
                Instrumentation.Transaction("battery").setProperty("batterylevel", batteryLevel).beginAndEnd();
            }
        }

        public bool GetBatteryLevel()
        {
#if CVR_OCULUS
            //TODO return oculus battery level
#endif
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
                                            batteryLevel =((float)level / (float)scale) * 100.0f;
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

        public static bool GetWarning()
        {
            if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
                return false;
            return true;
        }

        public static string GetDescription()
        {
            return "Send the battery level of Android device after initialization and on quit" + (GetWarning() ? "\nPlatform not set to Android!": "");
        }
    }
}