﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitive3D;
using UnityEngine.XR;

//the idea is that you could set up this component with your settings, then just call 'Activate'
//easier to visualize all the valid options, easier to override stuff
namespace Cognitive3D
{
    [AddComponentMenu("Cognitive3D/Common/ExitPoll Holder")]
    [HelpURL("https://docs.cognitive3d.com/unity/exitpoll/")]
    public class ExitPollHolder : MonoBehaviour
    {
        public ExitPollParameters Parameters = new ExitPollParameters();
        public bool ActivateOnEnable;
        ExitPollParameters poll;
        ExitPollSet exitPollSet;

        private void OnEnable()
        {
            InputTracking.trackingLost += OnTrackingLost;
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
            poll = ExitPoll.NewExitPoll(Parameters.Hook, Parameters);

            if (poll.ExitpollSpawnType == ExitPoll.SpawnType.World)
            {
                poll.UseOverridePosition = true;
                poll.OverridePosition = transform.position;
                poll.UseOverrideRotation = true;
                poll.OverrideRotation = transform.rotation;
                poll.RotateToStayOnScreen = false;
                poll.LockYPosition = false;
            }
            exitPollSet = poll.Begin();
        }

        public void OnTrackingLost(XRNodeState xrNodeState)
        {
            if (!xrNodeState.tracked)
            {
                if (xrNodeState.nodeType == XRNode.RightHand && poll.PointerType == ExitPoll.PointerType.RightControllerPointer
                    || xrNodeState.nodeType == XRNode.LeftHand && poll.PointerType == ExitPoll.PointerType.LeftControllerPointer)
                {
                    exitPollSet.EndQuestionSet(5); // Wait for 5 seconds. Default is 1 seconnd.
                    new CustomEvent("Exit Poll ended due to controller exception").Send();
                }
            }
        }
        
        private void Cleanup()
        {
            InputTracking.trackingLost -= OnTrackingLost;
            Cognitive3D_Manager.OnPostSessionEnd -= Cleanup;
        }

    }
}