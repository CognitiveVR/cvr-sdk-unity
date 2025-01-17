using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Cognitive3D
{
    internal static class ExitPollPointer
    {
        internal static GameObject pointerInstance = null;
        internal static ExitPollPanel currentExitPollPanel;
        internal static ExitPollParameters currentExitPollParameters;

        private static bool trackingWasLost;
        private const string CONTROLLER_NOT_FOUND = "Controller not found!";

        /// <summary>
        /// Creates an HMDPointer and attaches it to the HMD
        /// </summary>
        internal static void SetUpHMDAsPointer()
        {
            GameObject prefab = Resources.Load<GameObject>("HMDPointer");
            if (prefab != null)
                pointerInstance = GameObject.Instantiate(prefab);
            else
                Debug.LogError("Spawning Exitpoll HMD Pointer, but cannot find prefab \"HMDPointer\" in Resources!");

            if (pointerInstance != null)
            {
                //parent to hmd and zero position
                GameplayReferences.HMDPointer = pointerInstance;
                pointerInstance.transform.SetParent(GameplayReferences.HMD);
                pointerInstance.transform.localPosition = Vector3.zero;
                pointerInstance.transform.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// Creates a controller pointer and attaches it to the correct controller anchor <br/>
        /// If no controller is found, it creates an HMDPointer
        /// </summary>
        internal static void SetupControllerAsPointer(ExitPollManager.PointerInputButton inputButton, float pointerWidth, Gradient pointerGradient)
        {
            GameObject prefab = Resources.Load<GameObject>("PointerController");
            if (prefab != null)
                pointerInstance = GameObject.Instantiate(prefab);
            else
                Debug.LogError("Spawning Exitpoll Pointer Controller, but cannot find prefab \"PointerController\" in Resources!");

            if (pointerInstance != null)
            {
                GameplayReferences.PointerController = pointerInstance;
                pointerInstance.transform.localPosition = Vector3.zero;
                pointerInstance.transform.localRotation = Quaternion.identity;
                if (pointerInstance.GetComponent<PointerInputHandler>())
                {
                    pointerInstance.GetComponent<PointerInputHandler>().SetPointerType(inputButton);
                }
                if (pointerInstance.GetComponent<PointerVisualizer>())
                {
                    pointerInstance.GetComponent<PointerVisualizer>().ConstructDefaultLineRenderer(pointerWidth, pointerGradient);
                }
            }
        }

        /// <summary>
        /// Function to execute when tracking is lost
        /// </summary>
        /// <param name="xrNodeState">Information on the node that was lost</param>
        internal static void OnTrackingLost(XRNodeState xrNodeState)
        {
            if (!xrNodeState.tracked && currentExitPollParameters.PointerType == ExitPollManager.PointerType.ControllersAndHands)
            {
                if (xrNodeState.nodeType == XRNode.RightHand || xrNodeState.nodeType == XRNode.LeftHand)
                {
                    DisplayControllerError(true, CONTROLLER_NOT_FOUND);
                    trackingWasLost = true;
                }
            }
        }

        /// <summary>
        /// Function to execute when tracking is regained
        /// </summary>
        /// <param name="xrNodeState">Information on the node that was regained</param>
        internal static void OnTrackingRegained(XRNodeState xrNodeState)
        {
            if (xrNodeState.tracked && trackingWasLost)
            {
                DisplayControllerError(false);
                trackingWasLost = false;
            }
        }

        /// <summary>
        /// Toggle the display of the error message on the exit poll panel
        /// </summary>
        /// <param name="display">True if you want to display an error message; false to hide</param>
        internal static void DisplayControllerError(bool display)
        {
            currentExitPollPanel.DisplayError(display);
        }

        /// <summary>
        /// Set the display of the error message on the exit poll panel
        /// </summary>
        /// <param name="display">True if you want to display an error message; false to hide</param>
        /// <param name="errorText">The error message to display</param>
        internal static void DisplayControllerError(bool display, string errorText)
        {
            currentExitPollPanel.DisplayError(display, errorText);
        }
    }
}