using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CognitiveVR
{
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
            public string customerId;
        }
        [System.Serializable]
        public class UserData
        {
            public string userId;
            public string email;
            public string fullName;
            public string phone;
            public string[] roles = new string[] { };
            public Organization[] organizations = new Organization[] { };
            public Product[] products = new Product[] { };
            public bool isSuperAdmin;

            public static UserData Empty
            {
                get
                {
                    return new UserData();
                }
            }
        }
#pragma warning restore 0649
    }

    [System.Serializable]
    public class ExportSettings
    {
        public bool ExportStaticOnly = true;
        public float MinExportGeoSize = 1;
        public int ExplorerMinimumFaceCount = 200;
        public int ExplorerMaximumFaceCount = 5000;
        public int TextureQuality = 4;

        public static bool Match(ExportSettings a, ExportSettings b)
        {
            if (!Mathf.Approximately(a.MinExportGeoSize, b.MinExportGeoSize)) { return false; }
            if (a.ExportStaticOnly != b.ExportStaticOnly) { return false; }
            if (a.ExplorerMinimumFaceCount != b.ExplorerMinimumFaceCount) { return false; }
            if (a.ExplorerMaximumFaceCount != b.ExplorerMaximumFaceCount) { return false; }
            if (a.TextureQuality != b.TextureQuality) { return false; }
            return true;
        }

        public static ExportSettings Copy(ExportSettings target)
        {
            return new ExportSettings() { ExportStaticOnly = target.ExportStaticOnly,
                MinExportGeoSize = target.MinExportGeoSize,
                ExplorerMaximumFaceCount = target.ExplorerMaximumFaceCount,
                ExplorerMinimumFaceCount = target.ExplorerMinimumFaceCount,
                TextureQuality = target.TextureQuality };
        }
    }

    public class CognitiveVR_Preferences : ScriptableObject
    {
        [Serializable]
        public class SceneKeySetting
        {
            public string SceneName = "";
            public string SceneKey = "";
            public string ScenePath = "";
            //public bool Track = false;
            public long LastRevision;

            public SceneKeySetting(string name, string path)
            {
                SceneName = name;
                ScenePath = path;
            }
        }

        static CognitiveVR_Preferences instance;
        public static CognitiveVR_Preferences Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<CognitiveVR_Preferences>("CognitiveVR_Preferences");
                    if (instance == null)
                        instance = CreateInstance<CognitiveVR_Preferences>();
                }
                return instance;
            }
        }

        /// <summary>
        /// companyname1234-productname-test
        /// </summary>
        public string CustomerID = "";

        /// <summary>
        /// companyname1234-productname
        /// </summary>
        public string CompanyProductName
        {
            get
            {
                string customerid = CustomerID;
                if (customerid.EndsWith("-test") || customerid.EndsWith("-prod"))
                {
                    customerid = customerid.Substring(0, customerid.Length - 5);
                }
                return customerid;
            }
        }

        [HideInInspector]
        public string sessionID;

        [Header("User")]
        public string UserName;
        public CognitiveVR.Json.UserData UserData;
        public string SelectedOrganization;
        public string SelectedProduct;

        [Header("Player Tracking")]
        //player tracking
        public float SnapshotInterval = 0.5f;

        public int PlayerDataType = 0; //0 is 3d content with rendered gaze. 1 is video player with gaze from direction
        public bool TrackPosition = true;
        public bool TrackGazePoint = true;
        public bool TrackGazeDirection = false;
        public bool GazePointFromDirection = false;
        public float GazeDirectionMultiplier = 1.0f;

        [Header("Send Data")]
        public int SnapshotThreshold = 1000;
        public bool DebugWriteToFile = false;

        public bool SendDataOnQuit = true;
        public bool SendDataOnHMDRemove = false;
        public bool SendDataOnLevelLoad = true;
        public bool SendDataOnHotkey = true;
        public bool HotkeyShift = true;
        public bool HotkeyCtrl = false;
        public bool HotkeyAlt = false;
        public KeyCode SendDataHotkey = KeyCode.F9;

        [Header("Scene Export")]
        public string SavedBlenderPath = "";
        public ExportSettings ExportSettings;

        public static ExportSettings LowSettings = new ExportSettings() { MinExportGeoSize = 2,ExplorerMaximumFaceCount = 1000, ExplorerMinimumFaceCount = 20, TextureQuality = 8 };
        public static ExportSettings DefaultSettings = new ExportSettings() { MinExportGeoSize = 1, ExplorerMaximumFaceCount = 8000, ExplorerMinimumFaceCount = 100, TextureQuality = 4 };
        public static ExportSettings HighSettings = new ExportSettings() { MinExportGeoSize = 0, ExplorerMaximumFaceCount = 16000, ExplorerMinimumFaceCount = 400, TextureQuality = 2 };




        public List<SceneKeySetting> SceneKeySettings = new List<SceneKeySetting>();
        //use scene path instead of sceneName, if possible
        public SceneKeySetting FindScene(string sceneName)
        {
            return SceneKeySettings.Find(x => x.SceneName == sceneName);
        }

        public SceneKeySetting FindSceneByPath(string scenePath)
        {
            return SceneKeySettings.Find(x => x.ScenePath == scenePath);
        }


        /// <summary>
        /// get organization by name. returns null if no organization matches or no organizations are found
        /// </summary>
        /// <param name="organizationName"></param>
        /// <returns></returns>
        public Json.Organization GetOrganization(string organizationName)
        {
            for (int i = 0; i < UserData.organizations.Length; i++)
            {
                if (UserData.organizations[i].name == organizationName) { return UserData.organizations[i]; }
            }
            return null;
        }

        public Json.Product GetProduct(string productName)
        {
            for (int i = 0; i < UserData.products.Length; i++)
            {
                if (UserData.products[i].name == productName) { return UserData.products[i]; }
            }
            return null;
        }
    }
}