using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;

//the idea is that you could set up this component with your settings, then just call 'Activate'
//easier to visualize all the valid options, easier to override stuff
namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Common/ExitPoll Holder")]
    public class ExitPollHolder : MonoBehaviour
    {
        public ExitPollParameters Parameters = new ExitPollParameters();
        public bool ActivateOnEnable;

        private void OnEnable()
        {
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
            var poll = ExitPoll.NewExitPoll(Parameters.Hook, Parameters);

            if (poll.ExitpollSpawnType == ExitPoll.SpawnType.World)
            {
                poll.UseOverridePosition = true;
                poll.OverridePosition = transform.position;
                poll.UseOverrideRotation = true;
                poll.OverrideRotation = transform.rotation;
                poll.RotateToStayOnScreen = false;
                poll.LockYPosition = false;
            }

            poll.Begin();
        }
    }
}