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
        public bool DebugWriteToFile = true;
        public float SnapshotInterval = 1.0f;

        public int TrackHMDHeightSamples = 50;
        public int TrackArmLengthSamples = 50;

        [Header("Gaze Objects")]
        //gaze object
        public float GazeObjectSendInterval = 10;
    }
}