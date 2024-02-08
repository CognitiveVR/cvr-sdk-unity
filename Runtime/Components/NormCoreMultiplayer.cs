using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Normal.Realtime;

namespace Cognitive3D
{
    public class NormCoreMultiplayer : MonoBehaviour
    {
        Realtime normcoreRealtimeComponent;
        // Start is called before the first frame update
        void Start()
        {
            normcoreRealtimeComponent = FindObjectOfType<Realtime>();
            if (normcoreRealtimeComponent != null)
            {
                normcoreRealtimeComponent.didConnectToRoom += OnNormcoreConnected;
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void OnNormcoreConnected(Realtime realtime)
        {
            new CustomEvent("c3d.multiplayer.This player joined a room")
                .SetProperty("Room name", realtime.room.name)
                .SetProperty("Player ID", realtime.clientID)
                .Send();
        }
    }

}