using UnityEngine;
using System.Collections;
using System;

namespace CognitiveVR
{
    public class CognitiveVR_Preferences : ScriptableObject
    {
        static CognitiveVR_Preferences instance;
        public static CognitiveVR_Preferences GetPreferences()
        {
            if (instance == null)
            {
                instance = Resources.Load<CognitiveVR_Preferences>("CognitiveVR_Preferences");
                if (instance == null)
                    instance = new CognitiveVR_Preferences();
            }
            return instance;
        }

        //teleport
        public bool trackTeleportDistance = true;

        //gaze object
        public float objectSendInterval = 10;

    }
}