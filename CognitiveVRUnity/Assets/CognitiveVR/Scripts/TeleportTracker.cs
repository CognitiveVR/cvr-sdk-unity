using UnityEngine;
using System.Collections;

/// <summary>
/// sends a transaction when a player's HMD root transform changes positions. likely a teleport
/// </summary>

namespace CognitiveVR
{
    public class TeleportTracker : CognitiveVRAnalyticsComponent
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
            base.CognitiveVR_Init(initError);
            CognitiveVR_Manager.OnUpdate += CognitiveVR_Manager_OnUpdate;
        }

        void CognitiveVR_Manager_OnUpdate()
        {
            if (Vector3.SqrMagnitude(lastRootPosition - root.position) > 0.1f)
            {
                string transactionID = System.Guid.NewGuid().ToString();
                Vector3 newPosition = root.position;

                Instrumentation.Transaction("teleport", transactionID).setProperty("distance", Vector3.Distance(newPosition, lastRootPosition)).beginAndEnd();
                Util.logDebug("teleport");

                lastRootPosition = root.position;
            }
        }

        public static string GetDescription()
        {
            return "Sends a transaction when a player's HMD root transform changes positions. If the player moves without an immediate teleport, do not use this component!";
        }
    }
}