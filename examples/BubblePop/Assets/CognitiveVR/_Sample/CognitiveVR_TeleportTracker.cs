using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using CognitiveVR;

/// ===================================================
/// put this script on a SteamVR's [CameraRig] prefab. when the position of this gameobject changes, it will send a message to the analytics server
/// this is more efficient that constantly updating the player's position
/// ===================================================

public class CognitiveVR_TeleportTracker : MonoBehaviour
{
    string transactionID;

    Transform _transform;
    Transform myTransform
    {
        get { if (_transform == null) _transform = transform; return _transform ; }
    }

    bool trackDistance; //set in CognitiveVR_EditorPrefs
    bool teleporting;
    Vector3 startPosition;
    Vector3 lastPosition;

    void Start()
    {
        lastPosition = myTransform.position;
        CognitiveVR_Preferences prefs = CognitiveVR_Preferences.GetPreferences();
        trackDistance = prefs.trackTeleportDistance;
    }

    void Update()
    {
        if (Vector3.SqrMagnitude(lastPosition-myTransform.position) > 0.1f)
        {
            //start transaction
            if (!teleporting)
            {
                transactionID = System.Guid.NewGuid().ToString();
                CognitiveVR.Instrumentation.Transaction("teleport", transactionID).begin();
                if (trackDistance)
                {
                    startPosition = lastPosition;
                }
            }

            teleporting = true;
            lastPosition = myTransform.position;
        }
        if (teleporting)
        {
            //wait until the position is hasn't changed. ie, assume the player's position is moving very quickly instead of instantly
            if (Vector3.SqrMagnitude(lastPosition - myTransform.position) < 0.1f)
            {
                //end transaction
                lastPosition = myTransform.position;
                if (trackDistance)
                {
                    CognitiveVR.Instrumentation.Transaction("teleport", transactionID).setProperty("distance", Vector3.Distance(startPosition, lastPosition));
                }
                CognitiveVR.Instrumentation.Transaction("teleport", transactionID).end();
                teleporting = false;
            }
        }
    }
}