using UnityEngine;
using System.Collections;
using System;

namespace CognitiveVR
{
    public class CognitiveVR_Preferences : ScriptableObject
    {
        static CognitiveVR_Preferences instance;
        public static CognitiveVR_Preferences Instance
        {
            get
            {
                if (instance == null)
                {
                    Debug.Log("preference instance is null");
                    instance = Resources.Load<CognitiveVR_Preferences>("CognitiveVR_Preferences");
                    if (instance == null)
                        instance = CreateInstance<CognitiveVR_Preferences>();
                }
                return instance;
            }
        }

        public string CustomerID;

        [Header("Device Auto Scrap")]
        //autoscrap properties
        public bool ScreenResolution;
        public bool ChaperoneRoomSize;
        public bool SerialNumber;
        public bool HMDModel; //only in 5.4

        [Header("Events")]
        //events
        public bool TrackLowFramerateThreshold;
        public int LowFramerateThreshold = 60;
        public bool TrackBatteryLevel;
        public bool TrackChaperoneVisible;

        public bool HMDCollision = false;
        public bool ControllerCollision = false;
        public bool HMDProximity = false; //there is a 10 second delay on SteamVR proxy
        public LayerMask CollisionLayerMask;


        [Header("Transactions")]
        //transactions
        public bool sendDataOnQuit = true;
        public bool sendDataOnLevelLoad = true;
        public bool SessionEndOnApplicationQuit = true;
        public bool TotalTimePlayedCollectionUpdate = true; //collections are only for reporting changes. not storing values

        //comfort
        public bool TrackComfort = false; //fps + rotationrate

        [Header("Player Tracking")]
        //player tracking
        //public int SnapshotThreshold = 1000;
        //public bool DebugWriteToFile = true;
        public float SnapshotInterval = 1.0f;

        //public bool TrackPosition = true;
        //public bool TrackGazeDirection = false;
        //public bool TrackGazePoint = true;
        public bool TrackHMDHeight = false; //floor to hmd
        public bool TrackArmLength = false; //hmd to controller. needs to save distances. invalid poses (occluded) could result in incorrect arm lengths
        public bool TrackTeleport = true; //SteamVR root position moved
        public bool TrackTeleportDistance = true;

        [Header("Gaze Objects")]
        //gaze object
        public float GazeObjectSendInterval = 10;

        [Header("Scene Explorer Export")]
        //scene explorer
        public bool ExportStaticOnly = true;
        public float MinExportGeoSize = 4;
        public string AppendFileName = "";
        public int MinFaceCount = 200;
        public int MaxFaceCount = 10000;

    }
}