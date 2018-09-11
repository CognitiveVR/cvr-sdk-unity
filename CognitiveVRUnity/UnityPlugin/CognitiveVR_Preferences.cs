using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CognitiveVR
{
    public class CognitiveVR_Preferences : ScriptableObject
    {
        static bool IsSet = false;
        static CognitiveVR_Preferences instance;
        public static CognitiveVR_Preferences Instance
        {
            get
            {
                if (IsSet)
                    return instance;

                if (instance == null)
                {
                    instance = Resources.Load<CognitiveVR_Preferences>("CognitiveVR_Preferences");
                    if (instance == null)
                    {
                        Debug.LogWarning("Could not find CognitiveVR_Preferences in Resources. Settings will be incorrect!");
                        instance = CreateInstance<CognitiveVR_Preferences>();
                    }
                    IsSet = true;
                    S_SnapshotInterval = instance.SnapshotInterval;
                    //S_EvaluateGazeRealtime = instance.EvaluateGazeRealtime;
                    S_GazeSnapshotCount = instance.GazeSnapshotCount;
                    S_DynamicSnapshotCount = instance.DynamicSnapshotCount;
                    S_DynamicObjectSearchInParent = instance.DynamicObjectSearchInParent;
                    //S_TrackGazePoint = instance.TrackGazePoint;
                    //S_GazePointFromDirection = instance.GazePointFromDirection;
                    //S_VideoSphereDynamicObjectId = instance.VideoSphereDynamicObjectId;
                    //S_GazeDirectionMultiplier = instance.GazeDirectionMultiplier;
                    S_TransactionSnapshotCount = instance.TransactionSnapshotCount;
                    S_SensorSnapshotCount = instance.SensorSnapshotCount;
                }
                return instance;
            }
        }

        public static float S_SnapshotInterval;
        public static int S_GazeSnapshotCount;
        public static int S_DynamicSnapshotCount;
        public static int S_TransactionSnapshotCount;
        public static int S_SensorSnapshotCount;

        public static bool S_DynamicObjectSearchInParent;

        public static void SetLobbyId(string lobbyId)
        {
            LobbyId = lobbyId;
        }
        public static string LobbyId { get; private set; }

        public string Protocol = "https";
        public string Gateway = "data.cognitive3d.com";
        public string Dashboard = "app.cognitive3d.com";
        public string Viewer = "sceneexplorer.com/scene/";

        public GazeType GazeType = GazeType.Command;
        //0 is multipass, 1 is single pass, 2 is singlepass instanced
        public int RenderPassType;

        public bool IsAPIKeyValid
        {
            get
            {
                return !string.IsNullOrEmpty(APIKey);
            }
        }

        public string APIKey;

        public bool EnableLogging = true;

        [Header("Player Tracking")]
        //player tracking
        
        public float SnapshotInterval = 0.1f;
        public bool DynamicObjectSearchInParent = true;
        public bool TrackGPSLocation;
        public float GPSInterval = 1;
        public float GPSAccuracy = 2;
        public bool SyncGPSWithGaze;
        public bool RecordFloorPosition = true;

        [Header("Send Data")]
        public int GazeSnapshotCount = 64;
        public int SensorSnapshotCount = 64; //beyond this threshold? write to json (if not realtime) and send
        public int DynamicSnapshotCount = 64;
        public int TransactionSnapshotCount = 64;

        public bool SendDataOnQuit = true;
        public bool SendDataOnHMDRemove = true;
        public bool SendDataOnLevelLoad = true;
        public bool SendDataOnHotkey = true;
        public bool HotkeyShift = true;
        public bool HotkeyCtrl = false;
        public bool HotkeyAlt = false;
        public KeyCode SendDataHotkey = KeyCode.F9;

        //defualt 10MB cache size
        public long LocalDataCacheSize = 1024 * 1024 * 10;
        public bool LocalStorage = true;
        public int ReadLocalCacheCount = 2;

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

        public static void AddSceneSettings(CognitiveVR_Preferences newInstance, string name, string path)
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
            SceneSettings returnSettings = null;

            returnSettings = FindSceneByPath(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

            return returnSettings;
        }

        [Serializable]
        public class SceneSettings
        {
            public string SceneName = "";
            [UnityEngine.Serialization.FormerlySerializedAs("SceneKey")]
            public string SceneId = "";
            public string ScenePath = "";
            public long LastRevision;
            public int VersionNumber = 1;
            public int VersionId;

            public SceneSettings(string name, string path)
            {
                SceneName = name;
                ScenePath = path;
            }
        }
    }

    [System.Serializable]
    public class ExportSettings
    {
        public bool ExportStaticOnly = false;
        public float MinExportGeoSize = 1;
        public int ExplorerMinimumFaceCount = 100;
        public int ExplorerMaximumFaceCount = 8000;
        public int TextureQuality = 4;
        public string DiffuseTextureName = "_MainTex";

        public static ExportSettings LowSettings = new ExportSettings() { MinExportGeoSize = 0.5f, ExplorerMaximumFaceCount = 8000, ExplorerMinimumFaceCount = 1000, TextureQuality = 4 };
        public static ExportSettings DefaultSettings = new ExportSettings() { MinExportGeoSize = 0.25f, ExplorerMaximumFaceCount = 16000, ExplorerMinimumFaceCount = 2000, TextureQuality = 2 };
        public static ExportSettings HighSettings = new ExportSettings() { MinExportGeoSize = 0, ExplorerMaximumFaceCount = 131072, ExplorerMinimumFaceCount = 65536, TextureQuality = 1 };

        public static bool Match(ExportSettings a, ExportSettings b)
        {
            if (!Mathf.Approximately(a.MinExportGeoSize, b.MinExportGeoSize)) { return false; }
            if (a.ExportStaticOnly != b.ExportStaticOnly) { return false; }
            if (a.ExplorerMinimumFaceCount != b.ExplorerMinimumFaceCount) { return false; }
            if (a.ExplorerMaximumFaceCount != b.ExplorerMaximumFaceCount) { return false; }
            if (a.TextureQuality != b.TextureQuality) { return false; }
            if (a.DiffuseTextureName != b.DiffuseTextureName) { return false; }
            return true;
        }

        public static ExportSettings Copy(ExportSettings target)
        {
            return new ExportSettings() { ExportStaticOnly = target.ExportStaticOnly,
                MinExportGeoSize = target.MinExportGeoSize,
                ExplorerMaximumFaceCount = target.ExplorerMaximumFaceCount,
                ExplorerMinimumFaceCount = target.ExplorerMinimumFaceCount,
                TextureQuality = target.TextureQuality,
                DiffuseTextureName = target.DiffuseTextureName
            };
        }
    }
}