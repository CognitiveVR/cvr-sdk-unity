using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using System;
using Cognitive3D;
using Cognitive3D.Components;

namespace Cognitive3D
{
    public class AndroidPlugin : MonoBehaviour
    {
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

            SetCognitive3DPlugin();
        }

        public void SetCognitive3DPlugin()
        {
            plugin = new AndroidJavaClass("com.c3d.androidjavaplugin.Plugin");

            if (plugin != null)
            {
                // Create an instance of the Java class
                plugininstance = new AndroidJavaObject("com.c3d.androidjavaplugin.Plugin");

                plugininstance.Call("InitSessionData", 
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
    }
}
