using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Cognitive3D
{
    public class AndroidPlugin : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject plugin;
        AndroidJavaObject plugininstance;

        string filePath;
        string JSONfilePath;

        protected void OnEnable()
        {
            Cognitive3D_Manager.OnSessionBegin += OnSessionBegin;


            //if this component is enabled late, run startup as if session just began
            if (Cognitive3D_Manager.IsInitialized)
                OnSessionBegin();
        }

        protected void OnSessionBegin()
        {
            filePath = Application.persistentDataPath + "/c3dlocal/BackupCrashLogs.log";
            JSONfilePath = Application.persistentDataPath + "/c3dlocal/CrashLogs.json";

            InitCognitive3DPlugin();
            SetCognitive3DPlugin();

            LogFileHasContent();
        }

        private void InitCognitive3DPlugin()
        {
            plugin = new AndroidJavaClass("com.c3d.androidjavaplugin.Plugin");

            if (plugin != null)
            {
                // Create an instance of the Java class
                plugininstance = new AndroidJavaObject("com.c3d.androidjavaplugin.Plugin");
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

                plugininstance.Call("initCognitive3DAndroidPlugin", 
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
                    plugininstance.Call("serializeCrashEvents", 
                        lines[0],
                        lines[1],
                        lines[2],
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
