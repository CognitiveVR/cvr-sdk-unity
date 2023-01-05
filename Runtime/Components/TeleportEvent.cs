using UnityEngine;
using System.Collections;

/// <summary>
/// Sends a Custom Event when a player's HMD root transform changes positions
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Teleport Event")]
    public class TeleportEvent : AnalyticsComponentBase
    {
        Transform _root;
        Transform root
        {
            get
            {
                if (_root == null)
                    if (GameplayReferences.HMD == null) _root = transform;
                    else { _root = GameplayReferences.HMD.root; }
                return _root;
            }
        }

        Vector3 lastRootPosition;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
            lastRootPosition = root.position;
        }

        void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            if (Vector3.SqrMagnitude(lastRootPosition - root.position) > 0.1f)
            {
                Vector3 newPosition = root.position;

                new CustomEvent("cvr.teleport").SetProperty("distance", Vector3.Distance(newPosition, lastRootPosition)).Send(newPosition);
                Util.logDebug("teleport");

                lastRootPosition = root.position;
            }
        }

        public override string GetDescription()
        {
            return "Sends a Custom Event when a player's HMD root transform changes positions. If the player moves without an immediate teleport, do not use this component!";
        }

        private void Cognitive3D_Manager_OnPostSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPostSessionEnd -= Cognitive3D_Manager_OnPostSessionEnd;
        }

        void OnDestroy()
        {
            Cognitive3D_Manager_OnPostSessionEnd();
        }
    }
}