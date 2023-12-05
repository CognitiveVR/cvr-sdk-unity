using UnityEngine;

/// <summary>
/// Sends a Custom Event when a player's HMD root transform changes positions
/// </summary>

namespace Cognitive3D.Components
{
    [DisallowMultipleComponent]
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
            Cognitive3D_Manager.OnPreSessionEnd += Cognitive3D_Manager_OnPreSessionEnd;
            if (teleportPlayer != null)
            {
                lastRootPosition = teleportPlayer.position;
            }
        }

        void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            // We don't want these lines to execute if component disabled
            // Without this condition, these lines will execute regardless
            //      of component being disabled since this function is bound to C3D_Manager.Update on SessionBegin()
            if (isActiveAndEnabled)
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
            else
            {
                Debug.LogWarning("Teleport Event component is disabled. Please enable in inspector.");
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

        private void Cognitive3D_Manager_OnPreSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPreSessionEnd -= Cognitive3D_Manager_OnPreSessionEnd;
        }
    }
}
