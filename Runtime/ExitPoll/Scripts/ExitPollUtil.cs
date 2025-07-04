using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Cognitive3D
{
	internal static class ExitPollUtil
    {
        /// <summary>
        /// Retrieves the appropriate prefab based on the question type.
        /// </summary>
        internal static GameObject GetPrefab(ExitPollParameters parameters, Dictionary<string, string> properties)
        {
            if (!properties.ContainsKey("type")) { return null; }

            GameObject prefab = null;
            switch (properties["type"])
            {
                case "HAPPYSAD":
                    if (parameters.HappyPanelOverride != null)
                    {
                        prefab = parameters.HappyPanelOverride;
                    }
                    break;
                case "SCALE":
                    if (parameters.ScalePanelOverride != null)
                    {
                        prefab = parameters.ScalePanelOverride;
                    }
                    break;
                case "MULTIPLE":
                    if (parameters.MultiplePanelOverride != null)
                    {
                        prefab = parameters.MultiplePanelOverride;
                    }
                    break;
                case "VOICE":
                    if (parameters.VoicePanelOverride != null)
                    {
                        prefab = parameters.VoicePanelOverride;
                    }
                    break;
                case "THUMBS":
                    if (parameters.ThumbsPanelOverride != null)
                    {
                        prefab = parameters.ThumbsPanelOverride;
                    }
                    break;
                case "BOOLEAN":
                    if (parameters.BoolPanelOverride != null)
                    {
                        prefab = parameters.BoolPanelOverride;
                    }
                    break;
                default: Util.logDebug("Unknown Exitpoll panel type: " + properties["type"]);break;
            }
            return prefab;
        }

        /// <summary>
        /// Determines if a valid spawn position exists.
        /// </summary>
        internal static bool GetSpawnPosition(out Vector3 pos, ExitPollParameters parameters)
        {
            pos = Vector3.zero;
            if (GameplayReferences.HMD == null) //no hmd? fail
            {
                return false;
            }

            //set position and rotation
            Vector3 spawnPosition = GameplayReferences.HMD.position + GameplayReferences.HMD.forward * parameters.DisplayDistance;

            if (parameters.LockYPosition)
            {
                Vector3 modifiedForward = GameplayReferences.HMD.forward;
                modifiedForward.y = 0;
                modifiedForward.Normalize();

                spawnPosition = GameplayReferences.HMD.position + modifiedForward * parameters.DisplayDistance;
            }

            RaycastHit hit = new RaycastHit();

            if (parameters.PanelLayerMask.value != 0)
            {
                //test slightly in front of the player's hmd
                Collider[] colliderHits = Physics.OverlapSphere(GameplayReferences.HMD.position + Vector3.forward * 0.5f, 0.5f, parameters.PanelLayerMask);
                if (colliderHits.Length > 0)
                {
                    Util.logDebug("ExitPoll.Initialize hit collider " + colliderHits[0].gameObject.name + " too close to player. Skip exit poll");
                    //too close! just fail the popup and keep playing the game
                    return false;
                }

                //ray from player's hmd position
                if (Physics.SphereCast(GameplayReferences.HMD.position, 0.5f, spawnPosition - GameplayReferences.HMD.position, out hit, parameters.DisplayDistance, parameters.PanelLayerMask))
                {
                    if (hit.distance < parameters.MinimumDisplayDistance)
                    {
                        Util.logDebug("ExitPoll.Initialize hit collider " + hit.collider.gameObject.name + " too close to player. Skip exit poll");
                        //too close! just fail the popup and keep playing the game
                        return false;
                    }
                    else
                    {
                        spawnPosition = GameplayReferences.HMD.position + (spawnPosition - GameplayReferences.HMD.position).normalized * (hit.distance);
                    }
                }
            }

            pos = spawnPosition;
            return true;
        }

        /// <summary>
        /// Maps the specified input button to its corresponding XR input feature usage.
        /// </summary>
        internal static InputFeatureUsage<bool> GetButtonFeature(ExitPollManager.PointerInputButton button)
        {
            switch (button)
            {
                case ExitPollManager.PointerInputButton.Trigger:
                    return CommonUsages.triggerButton;
                case ExitPollManager.PointerInputButton.Grip:
                    return CommonUsages.gripButton;
                case ExitPollManager.PointerInputButton.PrimaryButton:
                    return CommonUsages.primaryButton;
                case ExitPollManager.PointerInputButton.SecondaryButton:
                    return CommonUsages.secondaryButton;
                case ExitPollManager.PointerInputButton.Primary2DAxisClick:
                    return CommonUsages.primary2DAxisClick;
                default:
                    return CommonUsages.triggerButton; // Default to trigger button if no match
            }
        }

        /// <summary>
        /// Checks the state of a specific input button for a given XRNode (controller).
        /// </summary>
        internal static bool GetButtonState(XRNode hand, InputFeatureUsage<bool> button)
        {
            InputDevice device = InputDevices.GetDeviceAtXRNode(hand);
            bool isPressed = false;

            if (device.isValid)
            {
                device.TryGetFeatureValue(button, out isPressed);
            }

            return isPressed;
        }
    }
}
