using UnityEngine;

/// <summary>
/// Sends a Custom Event when a player's HMD root transform changes positions
/// </summary>

namespace Cognitive3D.Components
{
    [AddComponentMenu("Cognitive3D/Components/Teleport Event")]
    public class TeleportEvent : AnalyticsComponentBase
    {
        [SerializeField]
        private Transform teleportPlayer;
        public Transform TeleportPlayer { get { return teleportPlayer; } set { teleportPlayer = value; } }

        Vector3 lastRootPosition;

        protected override void OnSessionBegin()
        {
            base.OnSessionBegin();
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
            if (teleportPlayer != null)
            {
                lastRootPosition = teleportPlayer.position;
            }
        }

        void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            if (teleportPlayer != null)
            {
                if (Vector3.SqrMagnitude(lastRootPosition - teleportPlayer.position) > 0.1f)
                {
                    Vector3 newPosition = teleportPlayer.position;
                    new CustomEvent("cvr.teleport").SetProperty("distance", Vector3.Distance(newPosition, lastRootPosition)).Send(newPosition);
                    Util.logDebug("teleport");
                    lastRootPosition = teleportPlayer.position;
                }
            }
        }

        public override string GetDescription()
        {
            return "Sends a Custom Event when a player's HMD root transform changes positions. If the player moves without an immediate teleport, do not use this component!";
        }

        public override bool GetError()
        {
            return (teleportPlayer == null);
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