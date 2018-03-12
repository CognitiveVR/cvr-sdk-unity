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
                    S_EvaluateGazeRealtime = instance.EvaluateGazeRealtime;
                    S_GazeSnapshotCount = instance.GazeSnapshotCount;
                    S_DynamicSnapshotCount = instance.DynamicSnapshotCount;
                    S_DynamicObjectSearchInParent = instance.DynamicObjectSearchInParent;
                    S_TrackGazePoint = instance.TrackGazePoint;
                    //S_GazePointFromDirection = instance.GazePointFromDirection;
                    S_VideoSphereDynamicObjectId = instance.VideoSphereDynamicObjectId;
                    S_GazeDirectionMultiplier = instance.GazeDirectionMultiplier;
                    S_TransactionSnapshotCount = instance.TransactionSnapshotCount;
                    S_SensorSnapshotCount = instance.SensorSnapshotCount;
                }
                return instance;
            }
        }

        public static float S_SnapshotInterval;
        public static bool S_EvaluateGazeRealtime;
        public static int S_GazeSnapshotCount;
        public static int S_DynamicSnapshotCount;
        public static int S_TransactionSnapshotCount;
        public static int S_SensorSnapshotCount;

        public static bool S_DynamicObjectSearchInParent;
        public static bool S_TrackGazePoint;
        public static bool S_GazePointFromDirection;
        public static int S_VideoSphereDynamicObjectId;
        public static float S_GazeDirectionMultiplier;

        //timestamp and session id
        //private static double _timeStamp;
        /*public static double TimeStamp
        {
            get
            {
                return CoreSubsystem.SessionTimeStamp;
            }
        }

        //private static string _sessionId;
        public static string SessionID
        {
            get
            {
                return CoreSubsystem.SessionID;
            }
        }*/

        /// <summary>
        /// companyname1234-productname. used in sceneexportwindow
        /// </summary>
        /*public string CompanyProduct
        {
            get
            {
                return CustomerID.Substring(0, CustomerID.Length - 5);
            }
        }

        /// <summary>
        /// companyname1234-productname-test
        /// </summary>
        public string CustomerID = "";

        public bool IsCustomerIDValid
        {
            get
            {
                return CustomerID.Length > 7; //at least a-b-test
            }
        }*/

        //public ReleaseType ReleaseType;


        //used to display dummy organization on account settings window. should never be used to determine current selection
        //public string OrgName;
        //used to display dummy product on account settings window. should never be used to determine current selection
        //public string ProductName;

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
        public bool TrackGazePoint = true;

        public int PlayerDataType = 0; //0 is 3d content with rendered gaze. 1 is video player with gaze from direction
        public int VideoSphereDynamicObjectId = 1000;
        public float GazeDirectionMultiplier = 1.0f;

        [Header("Send Data")]
        //public bool DebugWriteToFile = false;

        public bool EvaluateGazeRealtime = true; //evaluate gaze data at real time and send when threshold reached. otherwise, send when manually called
        public int GazeSnapshotCount = 64;

        public bool WriteJsonRealtime = true; //sends data when these thresholds are reached. if false, only send when manually called or OnQuit,HMDRemove,LevelLoad or HotKey
        //should be able to save snapshots and send when manually called
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

        [Header("Scene Export")]

        public List<SceneSettings> sceneSettings = new List<SceneSettings>();
        //use scene path instead of sceneName, if possible
        public SceneSettings FindScene(string sceneName)
        {
            return sceneSettings.Find(x => x.SceneName == sceneName);
        }

        public SceneSettings FindSceneByPath(string scenePath)
        {
            return sceneSettings.Find(x => x.ScenePath == scenePath);
        }

        public static string TrackingSceneName { get; private set; }
        public static void SetTrackingSceneName(string sceneName)
        {
            TrackingSceneName = sceneName;
        }

        /// <summary>
        /// return the scene settings for whichever scene should be receiving gaze,event,dynamic and sensor data. can return null
        /// </summary>
        /// <returns></returns>
        public static SceneSettings FindTrackingScene()
        {
            SceneSettings returnSettings = null;

            returnSettings = Instance.FindScene(TrackingSceneName);

            return returnSettings;
        }

        public static SceneSettings FindCurrentScene()
        {
            SceneSettings returnSettings = null;

            returnSettings = Instance.FindSceneByPath(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);

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

    namespace Json
    {
        //these are filled/emptied from json, so may not be directly referenced
#pragma warning disable 0649
        [System.Serializable]
        public class Organization
        {
            public string id;
            public string name;
            public string prefix;
        }
        [System.Serializable]
        public class Product
        {
            public string id;
            public string name;
            public string orgId;
            public string customerId = "";
        }
        [System.Serializable]
        public class UserData
        {
            public string userId;
            public string email;
            public Organization[] organizations = new Organization[] { };
            public Product[] products = new Product[] { };

            public static UserData Empty
            {
                get
                {
                    return new UserData();
                }
            }

            public Product AddProduct(string newProductName, string newCustomerId, string newOrganizationId, string newProductId = "")
            {
                List<Product> productList = new List<Product>();
                productList.AddRange(products);
                Product newProduct = new Product() { name = newProductName, orgId = newOrganizationId, customerId = newCustomerId, id = newProductId };
                productList.Add(newProduct);
                products = productList.ToArray();
                return newProduct;
            }
        }
#pragma warning restore 0649
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