using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D.Components;

namespace Cognitive3D
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cognitive3D/Components/Cognitive3D_AndroidPlugin")]
    public class AndroidPlugin : AnalyticsComponentBase
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject plugin;
        AndroidJavaObject plugininstance;
        string pluginName = "com.c3d.androidjavaplugin.Plugin";

        string filePath;
        string JSONfilePath;

        protected override void OnSessionBegin()
        {
            filePath = Application.persistentDataPath + "/c3dlocal/BackupCrashLogs.log";
            JSONfilePath = Application.persistentDataPath + "/c3dlocal/CrashLogs.json";

            // if (Cognitive3D_Preferences.Instance.useCrashLoggerAndroidPlugin)
            // {
                InitCognitive3DPlugin();
                SetCognitive3DPlugin();

                LogFileHasContent();
            // }
        }

        private void InitCognitive3DPlugin()
        {
            plugin = new AndroidJavaClass(pluginName);

            if (plugin != null)
            {
                // Create an instance of the Java class
                plugininstance = new AndroidJavaObject(pluginName);
            }
        }

        public void SetCognitive3DPlugin()
        {
            if (plugininstance != null)
            {

                plugininstance.Call("initSessionData", 
                    Cognitive3D_Manager.DeviceId, 
                    Util.Timestamp(Time.frameCount), 
                    Cognitive3D_Manager.SessionID
                );

                plugininstance.Call("initAndroidPlugin", 
                    GetCurrentActivity(), 
                    CognitiveStatics.ApplicationKey, 
                    filePath, 
                    JSONfilePath, 
                    CognitiveStatics.PostEventData(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber),
                    CognitiveStatics.PostGazeData(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber)
                );
            }
        }

        private AndroidJavaObject GetCurrentActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        private bool LogFileHasContent()
        {
            if (plugininstance != null && System.IO.File.Exists(filePath))
            {
                // Read all lines from the log file
                string[] lines = System.IO.File.ReadAllLines(filePath);

                // Check if line 4 exists and is not null or empty
                if (lines.Length >= 4 && !string.IsNullOrEmpty(lines[3]))
                {
                    // Reading time of crash from logfile
                    Util.TryExtractUnixTime(lines[5], out string crashTimeStamp);

                    plugininstance.Call("serializeCrashEvents", 
                        lines[0],
                        lines[1],
                        lines[2],
                        crashTimeStamp,
                        string.Join("\n", lines[3..])
                    );

                    plugininstance.Call("serializeCrashGaze", 
                        lines[0],
                        lines[1],
                        lines[2]
                    );

                    // Redirect and write new session data
                    plugininstance.Call("redirectErrorLogs");
                    return true;
                }
            }

            // Redirect and write new session data
            plugininstance.Call("redirectErrorLogs");
            return false;
        }
#endif      
    }
}
