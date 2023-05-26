using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Cognitive3D
{
    public class Cognitive3D_Preferences : ScriptableObject
    {
        static bool IsSet = false;
        static Cognitive3D_Preferences instance;
        public static Cognitive3D_Preferences Instance
        {
            get
            {
                if (IsSet)
                    return instance;

                if (instance == null)
                {
                    instance = Resources.Load<Cognitive3D_Preferences>("Cognitive3D_Preferences");
                    if (instance == null)
                    {
#if UNITY_EDITOR
                        if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                        {
                            Debug.LogError("Cognitive3D_Preferences asset is missing!");
                        }
#else
                        Debug.LogError("Cognitive3D_Preferences asset is missing!");
#endif
                        instance = CreateInstance<Cognitive3D_Preferences>();
                    }
                    IsSet = true;
                    S_GazeSnapshotCount = instance.GazeSnapshotCount;
                    S_DynamicSnapshotCount = instance.DynamicSnapshotCount;
                    S_DynamicObjectSearchInParent = instance.DynamicObjectSearchInParent;
                    S_EventDataThreshold = instance.EventDataThreshold;
                    S_SensorSnapshotCount = instance.SensorSnapshotCount;
                }
                return instance;
            }
        }

        //static for faster access
        public const float SnapshotInterval = 0.1f;
        public static int S_GazeSnapshotCount;
        public static int S_DynamicSnapshotCount;
        public static int S_EventDataThreshold;
        public static int S_SensorSnapshotCount;
        public static bool S_DynamicObjectSearchInParent;

        public string Protocol = "https";
        public string Gateway = "data.cognitive3d.com";
        public string Dashboard = "app.cognitive3d.com";
        public string Viewer = "viewer.cognitive3d.com/scene/";
        public string Documentation = "docs.cognitive3d.com";

        //used to show Scene Setup Window each time SDK is installed into a project - instead of using editor preferences
        public bool EditorHasDisplayedPopup = false;

        public GazeType GazeType = GazeType.Physics;

        public bool IsApplicationKeyValid
        {
            get
            {
                return !string.IsNullOrEmpty(ApplicationKey);
            }
        }

        public string ApplicationKey = string.Empty;
        public string AttributionKey;

        public bool EnableLogging = true;
        public bool EnableDevLogging = false;

        [Header("Player Tracking")]
        //player tracking

        public bool EnableGaze = true;
        public bool DynamicObjectSearchInParent = true;
        public bool TrackGPSLocation;
        public float GPSAccuracy = 2;
        public bool RecordFloorPosition = true;

        public int GazeLayerMask = -1;
        public int DynamicLayerMask = -1;
        public QueryTriggerInteraction TriggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Send Data")]
        //min batch size
        //if number of data points reaches this value, send immediately

        public int GazeSnapshotCount = 64;
        public int SensorSnapshotCount = 128;
        public int DynamicSnapshotCount = 128;
        public int EventDataThreshold = 64;
        public int FixationSnapshotCount = 64;

        public int AutomaticSendTimer = 10;

        //defualt 100MB cache size
        public long LocalDataCacheSize = 1024 * 1024 * 100;
        public bool LocalStorage = true;
        public bool UploadCacheOnEndPlay = true;

        public bool IncludeDisabledDynamicObjects = true;
        public int TextureResize = 1;
        public bool ExportSceneLODLowest = true;

        public List<SceneSettings> sceneSettings = new List<SceneSettings>();
        //use scene path instead of sceneName, if possible
        
        /// <summary>
        /// adds scene data if it doesn't already exist in scene settings
        /// </summary>
        /// <param name="scene"></param>
        public static void AddSceneSettings(UnityEngine.SceneManagement.Scene scene)
        {
            if (scene == null || string.IsNullOrEmpty(scene.name)) { return; }
            var foundSettings = Instance.sceneSettings.Find(x => x.ScenePath == scene.path);
            if (foundSettings != null)
            {
                foundSettings.SceneName = scene.name;
                foundSettings.ScenePath = scene.path;
            }
            else
            {
                instance.sceneSettings.Add(new SceneSettings(scene.name, scene.path));
            }
        }

        public static void AddSceneSettings(Cognitive3D_Preferences newInstance, string name, string path)
        {
            //skip. this should onyl be called automatically at the construction of preferences
            newInstance.sceneSettings.Add(new SceneSettings(name, path));
        }

        public static SceneSettings FindScene(string sceneName)
        {
            return Instance.sceneSettings.Find(x => x.SceneName == sceneName);
        }

        public static SceneSettings FindSceneByPath(string scenePath)
        {
            return Instance.sceneSettings.Find(x => x.ScenePath == scenePath);
        }

        public static SceneSettings FindSceneById(string sceneid)
        {
            return Instance.sceneSettings.Find(x => x.SceneId == sceneid);
        }
        /// <summary>
        /// return the scene settings for whichever scene is currently open and active
        /// </summary>
        /// <returns></returns>
        public static SceneSettings FindCurrentScene()
        {
            SceneSettings returnSettings = FindSceneByPath(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
            return returnSettings;
        }

        //this is created for a scene as part of the scene export step - VersionNumber may equal 0 before the scene has been uploaded
        [Serializable]
        public class SceneSettings
        {
            public string SceneName = "";
            [UnityEngine.Serialization.FormerlySerializedAs("SceneKey")]
            public string SceneId = "";
            public string ScenePath = "";
            public string LastRevision; //utc timestamp on upload
            public int VersionNumber; //post session data
            public int VersionId; //attribution. exitpoll?

            public SceneSettings(string name, string path)
            {
                SceneName = name;
                ScenePath = path;
            }
        }
    }
}
