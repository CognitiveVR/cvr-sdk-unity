using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D.Components;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;

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

        string folderPath;
        string currentFilePath;        

        protected override void OnSessionBegin()
        {
            folderPath = Application.persistentDataPath + "/c3dlocal/CrashLogs";
            currentFilePath = folderPath + "/BackupCrashLog-" + (int)Util.Timestamp() + ".log";

            // Creating a folder for crash logs in local cache directory
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            CreateAndroidPluginInstance();
            InitAndroidPlugin();

            LogFileHasContent();

            Cognitive3D_Manager.OnLevelLoaded += SetTrackingScene;
            Cognitive3D_Manager.OnPreSessionEnd += OnPreSessionEnd;
        }

       private void OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnLevelLoaded -= SetTrackingScene;
            Cognitive3D_Manager.OnPreSessionEnd -= OnPreSessionEnd;
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
                    currentFilePath
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

        // Checks for crash logs. If there are no crash logs, file gets deleted.
        private void LogFileHasContent()
        {
            // Check if the folder exists
            if (plugininstance != null && Directory.Exists(folderPath))
            {
                // Get all files in the folder
                string[] files = Directory.GetFiles(folderPath);

                if (files != null && files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        // Read all lines from the log file
                        string[] lines = System.IO.File.ReadAllLines(file);

                        // Check if line 6 exists for crash logs and is not null or empty
                        if (lines.Length >= 6 && !string.IsNullOrEmpty(lines[5]))
                        {
                            // Reading time of crash from logfile
                            string crashTimestamp; 
                            if (lines.Length >= 7)
                            {
                                crashTimestamp = Util.ExtractUnixTime(lines[6]);
                            }
                            else
                            {
                                crashTimestamp = Util.ExtractUnixTime(lines[5]);
                            }

                            plugininstance.Call("serializeCrashEvents", 
                                lines[0],
                                lines[1],
                                lines[2],
                                crashTimestamp,
                                CognitiveStatics.PostEventData(lines[3], int.Parse(lines[4])),
                                string.Join("\n", lines.Skip(5).ToArray()),
                                file
                            );

                            plugininstance.Call("serializeCrashGaze", 
                                lines[0],
                                lines[1],
                                lines[2],
                                CognitiveStatics.PostGazeData(lines[3], int.Parse(lines[4]))
                            );

                            // If response code is 200, the file gets deleted (handled in plugin). Otherwise, send in future sessions
                        }
                        else
                        {
                            // If it's not current session crash log file and has no crash logs, delete it
                            if (currentFilePath != file)
                            {
                                // No crash logs
                                plugininstance.Call("deleteLogFile", file);
                            }
                        }
                    }
                }
            }
        }
#endif   

    public override string GetDescription()
        {
#if UNITY_ANDROID
            return "Captures crash logs on Android devices. It is not functional during Unity Editor sessions.";
#else
            return "Android crash logging plugin only works when the build target is set to Android.";
#endif
        }
        
        public override bool GetWarning()
        {
#if UNITY_ANDROID
            return false;
#else
            return true;
#endif
        }
    }
}
