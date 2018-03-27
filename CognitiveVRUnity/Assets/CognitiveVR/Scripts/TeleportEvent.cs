using UnityEngine;
using System.Collections;

/// <summary>
/// sends a transaction when a player's HMD root transform changes positions. likely a teleport
/// </summary>

namespace CognitiveVR.Components
{
    public class TeleportEvent : CognitiveVRAnalyticsComponent
    {
        Transform _root;
        Transform root
        {
            get
            {
                if (_root == null)
                    if (CognitiveVR_Manager.HMD == null) _root = transform;
                    else { _root = CognitiveVR_Manager.HMD.root; }
                return _root;
            }
        }

        Vector3 lastRootPosition;

        public override void CognitiveVR_Init(Error initError)
        {
            if (initError != Error.Success) { return; }
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.UpdateEvent += CognitiveVR_Manager_OnUpdate;
            lastRootPosition = root.position;
        }

        void CognitiveVR_Manager_OnUpdate()
        {
            if (Vector3.SqrMagnitude(lastRootPosition - root.position) > 0.1f)
            {
                Vector3 newPosition = root.position;

                new CustomEvent("cvr.teleport").SetProperty("distance", Vector3.Distance(newPosition, lastRootPosition)).Send(newPosition);
                Util.logDebug("teleport");

                lastRootPosition = root.position;
            }
        }

        public static string GetDescription()
        {
            return "Sends a transaction when a player's HMD root transform changes positions. If the player moves without an immediate teleport, do not use this component!";
        }

        void OnDestroy()
        {
            CognitiveVR_Manager.UpdateEvent -= CognitiveVR_Manager_OnUpdate;
        }
    }
}