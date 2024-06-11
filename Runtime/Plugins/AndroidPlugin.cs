using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D.Components;
using UnityEngine.SceneManagement;

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

        protected override void OnSessionBegin()
        {
            filePath = Application.persistentDataPath + "/c3dlocal/BackupCrashLogs.log";

            if (Cognitive3D_Preferences.Instance.useAndroidCrashLoggingPlugin)
            {
                CreateAndroidPluginInstance();
                InitAndroidPlugin();

                LogFileHasContent();

                Cognitive3D_Manager.OnLevelLoaded += SetTrackingScene;
                Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
            }
        }

       private void OnPreSessionEnd()
        {
            if (Cognitive3D_Preferences.Instance.useAndroidCrashLoggingPlugin)
            {
                Cognitive3D_Manager.OnLevelLoaded -= SetTrackingScene;
                Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
            }
        }

        private void CreateAndroidPluginInstance()
        {
            plugin = new AndroidJavaClass(pluginName);

            if (plugin != null)
            {
                // Create an instance of the Java class
                plugininstance = new AndroidJavaObject(pluginName);
            }
        }

        public void InitAndroidPlugin()
        {
            if (plugininstance != null)
            {

                plugininstance.Call("initSessionData", 
                    CognitiveStatics.ApplicationKey, 
                    Cognitive3D_Manager.DeviceId, 
                    Util.Timestamp(Time.frameCount), 
                    Cognitive3D_Manager.SessionID,
                    Cognitive3D_Manager.TrackingSceneId,
                    Cognitive3D_Manager.TrackingSceneVersionNumber,
                    CognitiveStatics.PostEventData(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber),
                    CognitiveStatics.PostGazeData(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber)
                );

                plugininstance.Call("initAndroidPlugin", 
                    GetCurrentActivity(), 
                    filePath
                );
            }
        }

        private AndroidJavaObject GetCurrentActivity()
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        private void SetTrackingScene(Scene scene, LoadSceneMode mode, bool didChangeSceneId)
        {
            if (didChangeSceneId)
            {
                plugininstance.Call("onTrackingSceneChanged", 
                    Cognitive3D_Manager.TrackingSceneId, 
                    Cognitive3D_Manager.TrackingSceneVersionNumber,
                    CognitiveStatics.PostEventData(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber),
                    CognitiveStatics.PostGazeData(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber)
                );
            }
        }

        private bool LogFileHasContent()
        {
            if (plugininstance != null && System.IO.File.Exists(filePath))
            {
                // Read all lines from the log file
                string[] lines = System.IO.File.ReadAllLines(filePath);

                // Check if line 4 exists and is not null or empty
                if (lines.Length >= 6 && !string.IsNullOrEmpty(lines[5]))
                {
                    // Reading time of crash from logfile
                    Util.TryExtractUnixTime(lines[7], out string crashTimestamp);

                    plugininstance.Call("serializeCrashEvents", 
                        lines[0],
                        lines[1],
                        lines[2],
                        crashTimestamp,
                        CognitiveStatics.PostEventData(lines[3], int.Parse(lines[4])),
                        string.Join("\n", lines[5..])
                    );

                    plugininstance.Call("serializeCrashGaze", 
                        lines[0],
                        lines[1],
                        lines[2],
                        CognitiveStatics.PostGazeData(lines[3], int.Parse(lines[4]))
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
