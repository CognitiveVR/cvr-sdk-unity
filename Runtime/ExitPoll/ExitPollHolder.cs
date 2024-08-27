using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    /// <summary>
    /// Initializes and activates ExitPoll
    /// </summary>
    [AddComponentMenu("Cognitive3D/Common/ExitPoll Holder")]
    [HelpURL("https://docs.cognitive3d.com/unity/exitpoll/")]
    public class ExitPollHolder : MonoBehaviour
    {
        public ExitPollParameters Parameters = new ExitPollParameters();
        public bool ActivateOnEnable;

        private void OnEnable()
        {
            Cognitive3D_Manager.OnPostSessionEnd += Cleanup;
            if (ActivateOnEnable)
            {
                //will wait for cognitive vr manager to have call initialize before activating
                if (Cognitive3D_Manager.IsInitialized)
                {
                    Activate();
                }
                else
                {
                    Cognitive3D_Manager.OnSessionBegin += Core_DelayInitEvent;
                }
            }
        }

        /// <summary>
        /// This function subscribes to OnSessionBegin event if C3D_Manager isn't initialized on enable
        /// </summary>
        private void Core_DelayInitEvent()
        {
            Cognitive3D_Manager.OnSessionBegin -= Core_DelayInitEvent;
            OnEnable();
        }

        /// <summary>
        /// Display an ExitPoll using the QuestionSetHook and parameters configured on the component
        /// </summary>
        public void Activate()
        {
            ExitPollParameters poll = ExitPoll.NewExitPoll(Parameters.Hook, Parameters);
            if (poll.ExitpollSpawnType == ExitPoll.SpawnType.World)
            {
                poll.UseOverridePosition = true;
                poll.OverridePosition = transform.position;
                poll.UseOverrideRotation = true;
                poll.OverrideRotation = transform.rotation;
                poll.RotateToStayOnScreen = false;
                poll.LockYPosition = false;
            }
            ExitPollSet exitPollSet = new ExitPollSet();
            exitPollSet.BeginExitPoll(Parameters);
        }

        private void Cleanup()
        {
            Cognitive3D_Manager.OnPostSessionEnd -= Cleanup;
        }
    }
}