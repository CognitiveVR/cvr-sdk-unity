using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CognitiveVR
{
    public class CognitiveVR_Preferences : ScriptableObject
    {
        [Serializable]
        public class SceneKeySetting
        {
            public string SceneName = "";
            public string SceneKey = "";
            public string ScenePath = "";
            public bool Track = true;

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

        public string CustomerID = "companyname1234-productname-test";

        [Header("Events")]
        public int LowFramerateThreshold = 60;
        public LayerMask CollisionLayerMask = 1;

        [Header("Player Tracking")]
        //player tracking
        public int SnapshotThreshold = 1000;
        public bool DebugWriteToFile = false;
        public float SnapshotInterval = 0.5f;

        public bool TrackPosition = true;
        public bool TrackGazePoint = true;
        public bool TrackGazeDirection = false;
        public bool GazePointFromDirection = false;
        public float GazeDirectionMultiplier = 1.0f;

        public bool SendDataOnQuit = true;
        public bool SendDataOnHMDRemove = true;
        public bool SendDataOnLevelLoad = true;

        public int TrackHMDHeightSamples = 50;
        public int TrackArmLengthSamples = 50;

        public bool ExportStaticOnly = true;
        public float MinExportGeoSize = 4;
        public int ExplorerMinimumFaceCount = 200;
        public int ExplorerMaximumFaceCount = 5000;
        public string SavedBlenderPath = "";

        [Header("Gaze Objects")]
        //gaze object
        public float GazeObjectSendInterval = 10;

        public List<SceneKeySetting> SceneKeySettings = new List<SceneKeySetting>();
        public SceneKeySetting FindScene(string sceneName)
        {
            return SceneKeySettings.Find(x => x.SceneName == sceneName);
        }
    }
}