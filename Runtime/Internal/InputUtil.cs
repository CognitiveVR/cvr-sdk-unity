using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Cognitive3D
{
    public static class InputUtil
    {
        // Defines input types for handling different control methods:
        // Controller (0), Hand (1), and a placeholder for future simultaneous support of both (ControllerAndHand).
        public enum InputType
        {
            None = 0,
            Controller = 1,
            Hand = 2,
            // For simultaneous controller and hand support
            // ControllerAndHand = 3,
        }
        
        //developer facing high level controller type selection
        public enum ControllerType
        {
            Quest2 = 1,
            QuestPro = 2,
            Quest3 = 9,
            ViveWand = 3,
            WindowsMRController = 4,
            SteamIndex = 5,
            PicoNeo3 = 6,
            PicoNeo4 = 7,
            ViveFocus = 8,
            Hand = 10, //might suggest that this includes skeletal hand tracking, which needs some more design
            //Generic = 0, //basically a non-branded oculus touch controller
        }
        
        //used internally to have a consistent mesh name string
        public enum CommonDynamicMesh
        {
            ViveController = 0,
            OculusRiftTouchLeft = 1,
            OculusRiftTouchRight = 2,
            ViveTracker = 3,
            ExitPoll = 4, //used internally
            LeapMotionHandLeft = 5,
            LeapMotionHandRight = 6,
            WindowsMixedRealityLeft = 7,
            WindowsMixedRealityRight = 8,
            VideoSphereLatitude = 9,
            VideoSphereCubemap = 10,
            SnapdragonVRController = 11,
            ViveFocusControllerRight = 12,
            OculusQuestTouchLeft = 13,
            OculusQuestTouchRight = 14,
            PicoNeoControllerLeft = 15,
            PicoNeoControllerRight = 16,
            PicoNeo3ControllerLeft = 17,
            PicoNeo3ControllerRight = 18,
            Unknown = 19,
            ViveFocusControllerLeft = 20,
            SteamIndexLeft = 21,
            SteamIndexRight = 22,
            PicoNeo4ControllerLeft = 23,
            PicoNeo4ControllerRight = 24,
            QuestProTouchLeft = 25,
            QuestProTouchRight = 26,
            QuestPlusTouchLeft = 27,
            QuestPlusTouchRight = 28,
            handLeft = 29,
            handRight = 30
        }

        //used internally to have a consistent button input image
        public enum ControllerDisplayType
        {
            vive_controller = 1, //wand
            vive_focus_controller_right = 2,
            vive_focus_controller_left = 14,
            oculus_rift_controller_left = 3,
            oculus_rift_controller_right = 4,
            oculus_quest_touch_left = 5,
            oculus_quest_touch_right = 6,
            windows_mixed_reality_controller_left = 7,
            windows_mixed_reality_controller_right = 8,
            pico_neo_2_eye_controller_left = 9,
            pico_neo_2_eye_controller_right = 10,
            pico_neo_3_eye_controller_left = 11,
            pico_neo_3_eye_controller_right = 12,
            unknown = 13,
            pico_neo_4_eye_controller_left = 15,
            pico_neo_4_eye_controller_right = 16,
            steam_index_left = 17,
            steam_index_right = 18,
            quest_pro_touch_left = 19,
            quest_pro_touch_right = 20,
            quest_plus_touch_left = 21,
            quest_plus_touch_right = 22,
            hand_left = 23,
            hand_right = 24,
        }

        internal static bool TryGetInputDevice(XRNode node, out InputDevice input)
        {
            input = InputDevices.GetDeviceAtXRNode(node);
            if (input.isValid)
            {
                return true;
            }

            return false;
        }

        internal static CommonDynamicMesh GetControllerMeshName(string xrDeviceName, bool isRight)
        {
            if (xrDeviceName.Contains("Vive Wand")
                || xrDeviceName.Contains("Vive. Controller MV")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR"))
            {
                return CommonDynamicMesh.ViveController;
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Left")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                string oculusHeadsetType = "";
    #if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
    #endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return CommonDynamicMesh.QuestProTouchLeft;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return CommonDynamicMesh.QuestPlusTouchLeft;
                }
                else
                {
                    return CommonDynamicMesh.OculusQuestTouchLeft;
                }
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Right")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                string oculusHeadsetType = "";
    #if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
    #endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return CommonDynamicMesh.QuestProTouchRight;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return CommonDynamicMesh.QuestPlusTouchRight;
                }
                else
                {
                    return CommonDynamicMesh.OculusQuestTouchRight;
                }
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Left")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Left"))
                && !isRight)
            {
                return CommonDynamicMesh.ViveFocusControllerLeft;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Right"))
                && isRight)
            {
                return CommonDynamicMesh.ViveFocusControllerRight;
            }
            if ((xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR"))
                && !isRight)
            {
                return CommonDynamicMesh.WindowsMixedRealityLeft;
            }
            if ((xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR"))
                && isRight)
            {
                return CommonDynamicMesh.WindowsMixedRealityRight;
            }
            if (xrDeviceName.Equals("PicoXR Controller-Left"))
            {
                return CommonDynamicMesh.PicoNeo3ControllerLeft;
            }
            if (xrDeviceName.Equals("PicoXR Controller-Right"))
            {
                return CommonDynamicMesh.PicoNeo3ControllerRight;
            }
            if (xrDeviceName.Equals("PICO Controller-Left"))
            {
                return CommonDynamicMesh.PicoNeo4ControllerLeft;
            }
            if (xrDeviceName.Equals("PICO Controller-Right"))
            {
                return CommonDynamicMesh.PicoNeo4ControllerRight;
            }
            return CommonDynamicMesh.Unknown;
        }

        //the svg popup that displays the button presses
        //used by controller input tracker to determine how to record input names
        internal static ControllerDisplayType GetControllerPopupName(string xrDeviceName, bool isRight)
        {
            if (xrDeviceName.Contains("Vive Wand")
                || xrDeviceName.Contains("Vive. Controller MV")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR"))
            {
                return ControllerDisplayType.vive_controller;
            }

#if !C3D_VIVEWAVE
            if (xrDeviceName.Contains("WVR_CR"))
            {
                return ControllerDisplayType.vive_controller;
            }
#endif
            if (xrDeviceName.Equals("Oculus Touch Controller - Left")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == false))
            {
                string oculusHeadsetType = "";
#if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
#endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return ControllerDisplayType.quest_pro_touch_left;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return ControllerDisplayType.quest_plus_touch_left;
                }
                else
                {
                    return ControllerDisplayType.oculus_quest_touch_left;
                }
            }
            if (xrDeviceName.Equals("Oculus Touch Controller - Right")
                || (xrDeviceName.Equals("Oculus Touch Controller OpenXR") && isRight == true))
            {
                string oculusHeadsetType = "";
#if C3D_OCULUS
                oculusHeadsetType = OVRPlugin.GetSystemHeadsetType().ToString();
#endif
                if (oculusHeadsetType.Contains("Pro"))
                {
                    return ControllerDisplayType.quest_pro_touch_right;
                }
                else if (oculusHeadsetType.Contains("Quest_3"))
                {
                    return ControllerDisplayType.quest_plus_touch_right;
                }
                else
                {
                    return ControllerDisplayType.oculus_quest_touch_right;
                }
            }
            if (xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR")
                && isRight == false)
            {
                return ControllerDisplayType.windows_mixed_reality_controller_left;
            }
            if (xrDeviceName.Contains("OpenVR Controller(WindowsMR")
                || xrDeviceName.Equals("Windows MR Controller OpenXR")
                && isRight == true)
            {
                return ControllerDisplayType.windows_mixed_reality_controller_right;
            }
            if (xrDeviceName.Equals("PicoXR Controller-Left"))
            {
                return ControllerDisplayType.pico_neo_3_eye_controller_left;
            }
            if(xrDeviceName.Equals("PicoXR Controller-Right"))
            {
                return ControllerDisplayType.pico_neo_3_eye_controller_right;
            }
            if (xrDeviceName.Equals("PICO Controller-Left"))
            {
                return ControllerDisplayType.pico_neo_4_eye_controller_left;
            }
            if (xrDeviceName.Equals("PICO Controller-Right"))
            {
                return ControllerDisplayType.pico_neo_4_eye_controller_right;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Left")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Left")))
            {
                return ControllerDisplayType.vive_focus_controller_left;
            }
            if ((xrDeviceName.Equals("OpenVR Controller(vive_cosmos_controller) - Right")
                || xrDeviceName.Equals("HTC Vive Controller OpenXR")
                || xrDeviceName.Contains("WVR_CR_Right")))
            {
                return ControllerDisplayType.vive_focus_controller_right;
            }
            return ControllerDisplayType.unknown;
        }

        /// <summary>
        /// Oculus SeGets the current tracked device i.e. hand or controller
        /// </summary>
        /// <returns> Enum representing whether user is using hand or controller or neither </returns>
        public static InputType GetCurrentTrackedDevice()
        {
#if C3D_OCULUS
            var currentTrackedDevice = OVRInput.GetConnectedControllers();
            if (currentTrackedDevice == OVRInput.Controller.None)
            {
                return InputType.None;
            }
            else if (currentTrackedDevice == OVRInput.Controller.Hands
                || currentTrackedDevice == OVRInput.Controller.LHand
                || currentTrackedDevice == OVRInput.Controller.RHand)
            {
                return InputType.Hand;
            }
            else
            {
                return InputType.Controller;
            }
#elif C3D_VIVEWAVE
            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);

            foreach (var device in devices)
            {
                bool isHandTracked = Wave.OpenXR.InputDeviceHand.IsTracked(true) || Wave.OpenXR.InputDeviceHand.IsTracked(false);
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.HandTracking) && isHandTracked)
                {
                    // Hand tracking is in use
                    return InputType.Hand;
                }

                if (device.characteristics.HasFlag(InputDeviceCharacteristics.Controller) && !isHandTracked)
                {
                    // Controller is in use
                    return InputType.Controller;
                }
            }

            return InputType.None;
#elif C3D_PICOXR
            Unity.XR.PXR.ActiveInputDevice currentTrackedInputDevice = Unity.XR.PXR.PXR_HandTracking.GetActiveInputDevice();
            if (currentTrackedInputDevice == Unity.XR.PXR.ActiveInputDevice.HandTrackingActive)
            {
                return InputType.Hand;
            }
            else if (currentTrackedInputDevice == Unity.XR.PXR.ActiveInputDevice.ControllerActive)
            {
                return InputType.Controller;
            }
            else // if neither hand nor controller, it's head (Technically none)
            {
                return InputType.None;
            }
#elif C3D_DEFAULT
    #if COGNITIVE3D_INCLUDE_XR_HANDS
            UnityEngine.XR.Hands.XRHandSubsystem activeHandSubsystem = null;

            // Fetch all available XRHandSubsystems
            var subsystems = new List<UnityEngine.XR.Hands.XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            foreach (var subsystem in subsystems)
            {
                if (subsystem.running)
                {
                    activeHandSubsystem = subsystem;
                    break;
                }
            }

            if (activeHandSubsystem != null)
            {
                if (activeHandSubsystem.leftHand.isTracked || activeHandSubsystem.rightHand.isTracked)
                {
                    return InputType.Hand;
                }
            }
    #endif

            List<InputDevice> devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (var device in devices)
            {
                if (device.characteristics.HasFlag(InputDeviceCharacteristics.Controller))
                {
                    return InputType.Controller;
                }
            }

            return InputType.None;
#else
            return InputType.Controller;
#endif
        }

        /// <summary>
        /// Attempts to retrieve the world-space position of a controller/hand based on the specified XRNode.
        /// Transforms the local position to world-space using the HMD's parent transform.
        /// </summary>
        /// <param name="node">The XRNode representing the controller (e.g., LeftHand or RightHand).</param>
        /// <param name="position">The resulting world-space position of the controller.</param>
        /// <returns>True if a valid position is obtained, false if the position is (0,0,0).</returns>
        public static bool TryGetControllerPosition(XRNode node, out Vector3 position)
        {
            position = GetNodePosition(node);
            position = GameplayReferences.HMD.transform.parent.TransformPoint(position);
            return position != Vector3.zero;
        }


        /// <summary>
        /// Retrieves the position of a specified XR node (e.g., hand or controller) based on the current tracking type.
        /// Returns Vector3.zero if the node position cannot be determined or if the tracking type is unsupported.
        /// </summary>
        /// <param name="node">The XRNode (e.g., XRNode.LeftHand, XRNode.RightHand) to get the position for.</param>
        private static Vector3 GetNodePosition(XRNode node)
        {
            InputType currentTracking = GetCurrentTrackedDevice();

#if C3D_OCULUS
            var targetNode = currentTracking switch
            {
                InputType.Controller => node == XRNode.RightHand ? OVRPlugin.Node.ControllerRight : OVRPlugin.Node.ControllerLeft,
                InputType.Hand => node == XRNode.RightHand ? OVRPlugin.Node.HandRight : OVRPlugin.Node.HandLeft,
                _ => OVRPlugin.Node.None,
            };

            if (targetNode != OVRPlugin.Node.None)
            {
                return OVRPlugin.GetNodePose(targetNode, OVRPlugin.Step.Render).ToOVRPose().position;
            }
#elif C3D_PICOXR
            switch (currentTracking)
            {
                case InputType.Controller:
                    var controllerNode = node == XRNode.RightHand ? Unity.XR.PXR.PXR_Input.Controller.RightController : Unity.XR.PXR.PXR_Input.Controller.LeftController;
                    return Unity.XR.PXR.PXR_Input.GetControllerPredictPosition(controllerNode, 0);
                case InputType.Hand:
                    var handNode = node == XRNode.RightHand ? Unity.XR.PXR.HandType.HandRight : Unity.XR.PXR.HandType.HandLeft;

                    Unity.XR.PXR.HandJointLocations handJointLocations = new Unity.XR.PXR.HandJointLocations();

                    if (Unity.XR.PXR.PXR_HandTracking.GetJointLocations(handNode, ref handJointLocations) && handJointLocations.isActive != 0U)
                    {
                        int wristIndex = (int)Unity.XR.PXR.HandJoint.JointWrist;
                        if (wristIndex < handJointLocations.jointLocations.Length)
                        {
                            return handJointLocations.jointLocations[wristIndex].pose.Position.ToVector3();
                        }
                    }
                    break;
            }
#elif C3D_VIVEWAVE
            var position = Vector3.zero;
            switch (currentTracking)
            {
                case InputType.Controller:
                    var controller = node == XRNode.RightHand ? Wave.OpenXR.InputDeviceControl.ControlDevice.Right : Wave.OpenXR.InputDeviceControl.ControlDevice.Left;
                    Wave.OpenXR.InputDeviceControl.GetPosition(controller, out position);
                    return position;

                case InputType.Hand:
                    if (Wave.Essence.Hand.HandManager.Instance != null)
                    {
                        var isRightHand = node == XRNode.RightHand;
                        Wave.Essence.Hand.HandManager.Instance.GetJointPosition(Wave.Essence.Hand.HandManager.HandJoint.Wrist, ref position, isRightHand);
                        return position;
                    }
                    break;
            }
#elif C3D_DEFAULT
            switch (currentTracking)
            {
                case InputType.Controller:
                    return GetDefaultNodePosition(node);

    #if COGNITIVE3D_INCLUDE_XR_HANDS
                case InputType.Hand:
                    var subsystems = new List<UnityEngine.XR.Hands.XRHandSubsystem>();
                    SubsystemManager.GetSubsystems(subsystems);

                    foreach (var subsystem in subsystems)
                    {
                        if (!subsystem.running) continue;

                        var hand = node == XRNode.RightHand ? subsystem.rightHand : subsystem.leftHand;
                        if (hand.isTracked)
                        {
                            var wrist = hand.GetJoint(UnityEngine.XR.Hands.XRHandJointID.Wrist);
                            if (wrist.TryGetPose(out var pose))
                            {
                                return pose.position;
                            }
                        }
                    }
                    break;
    #endif
            }
#endif
            // Default fallback for retrieving controller positions
            // Compatible with all XR SDKs but does not return hand positions
            return GetDefaultNodePosition(node);
        }

        /// <summary>
        /// Default fallback for retrieving the position of an XRNode when no specific SDK is defined.
        /// </summary>
        /// <param name="node">The XRNode to retrieve the position for (e.g., LeftController, RightController, Head).</param>
        /// <returns>The position of the specified XRNode, or Vector3.zero if not found.</returns>
        internal static Vector3 GetDefaultNodePosition(XRNode node)
        {
            List<XRNodeState> nodeStates = new List<XRNodeState>();
            InputTracking.GetNodeStates(nodeStates);

            foreach (XRNodeState nodeState in nodeStates)
            {
                if (nodeState.nodeType == node && nodeState.TryGetPosition(out var position))
                {
                    return position;
                }
            }

            return Vector3.zero;
        }

        /// <summary>
        /// Attempts to retrieve the world-space rotation of a controller/hand based on the specified XRNode.
        /// Adjusts the local rotation to account for the parent's rotation.
        /// </summary>
        /// <param name="node">The XRNode representing the controller (e.g., LeftHand or RightHand).</param>
        /// <param name="rotation">The resulting world-space rotation of the controller.</param>
        /// <returns>True if a valid rotation is obtained.</returns>
        public static bool TryGetControllerRotation(XRNode node, out Quaternion rotation)
        {
            rotation = GetNodeRotation(node);
            // Ensure we have a valid rotation
            if (rotation == Quaternion.identity)
            {
                return false;
            }
            
            rotation = GameplayReferences.HMD.transform.parent.rotation * rotation;
            return true;
        }

        /// <summary>
        /// Retrieves the rotation of a specified XR node (e.g., hand or controller) based on the current tracking type.
        /// Returns Quaternion.identity if the node rotation cannot be determined or if the tracking type is unsupported.
        /// </summary>
        /// <param name="node">The XRNode (e.g., XRNode.LeftHand, XRNode.RightHand) to get the rotation for.</param>
        private static Quaternion GetNodeRotation(XRNode node)
        {
            InputType currentTracking = GetCurrentTrackedDevice();

#if C3D_OCULUS
            var targetNode = currentTracking switch
            {
                InputType.Controller => node == XRNode.RightHand ? OVRPlugin.Node.ControllerRight : OVRPlugin.Node.ControllerLeft,
                InputType.Hand => node == XRNode.RightHand ? OVRPlugin.Node.HandRight : OVRPlugin.Node.HandLeft,
                _ => OVRPlugin.Node.None,
            };

            if (targetNode != OVRPlugin.Node.None)
            {
                return OVRPlugin.GetNodePose(targetNode, OVRPlugin.Step.Render).ToOVRPose().orientation;
            }
#elif C3D_PICOXR
            switch (currentTracking)
            {
                case InputType.Controller:
                    var controllerNode = node == XRNode.RightHand ? Unity.XR.PXR.PXR_Input.Controller.RightController : Unity.XR.PXR.PXR_Input.Controller.LeftController;
                    return Unity.XR.PXR.PXR_Input.GetControllerPredictRotation(controllerNode, 0);
                case InputType.Hand:
                    var handNode = node == XRNode.RightHand ? Unity.XR.PXR.HandType.HandRight : Unity.XR.PXR.HandType.HandLeft;

                    Unity.XR.PXR.HandJointLocations handJointLocations = new Unity.XR.PXR.HandJointLocations();

                    if (Unity.XR.PXR.PXR_HandTracking.GetJointLocations(handNode, ref handJointLocations) && handJointLocations.isActive != 0U)
                    {
                        int wristIndex = (int)Unity.XR.PXR.HandJoint.JointWrist;
                        if (wristIndex < handJointLocations.jointLocations.Length)
                        {
                            return handJointLocations.jointLocations[wristIndex].pose.Orientation.ToQuat();
                        }
                    }
                    break;
            }
#elif C3D_VIVEWAVE
            var rotation = Quaternion.identity;
            switch (currentTracking)
            {
                case InputType.Controller:
                    var controller = node == XRNode.RightHand ? Wave.OpenXR.InputDeviceControl.ControlDevice.Right : Wave.OpenXR.InputDeviceControl.ControlDevice.Left;
                    Wave.OpenXR.InputDeviceControl.GetRotation(controller, out rotation);
                    return rotation;

                case InputType.Hand:
                    if (Wave.Essence.Hand.HandManager.Instance != null)
                    {
                        var isRightHand = node == XRNode.RightHand;
                        Wave.Essence.Hand.HandManager.Instance.GetJointRotation(Wave.Essence.Hand.HandManager.HandJoint.Wrist, ref rotation, isRightHand);
                        return rotation;
                    }
                    break;
            }
#elif C3D_DEFAULT
            switch (currentTracking)
            {
                case InputType.Controller:
                    return GetDefaultNodeRotation(node);

    #if COGNITIVE3D_INCLUDE_XR_HANDS
                case InputType.Hand:
                    var subsystems = new List<UnityEngine.XR.Hands.XRHandSubsystem>();
                    SubsystemManager.GetSubsystems(subsystems);

                    foreach (var subsystem in subsystems)
                    {
                        if (!subsystem.running) continue;

                        var hand = node == XRNode.RightHand ? subsystem.rightHand : subsystem.leftHand;
                        if (hand.isTracked)
                        {
                            var wrist = hand.GetJoint(UnityEngine.XR.Hands.XRHandJointID.Wrist);
                            if (wrist.TryGetPose(out var pose))
                            {
                                return pose.rotation;
                            }
                        }
                    }
                    break;
    #endif
            }
#endif
            // Default fallback for retrieving controller rotations
            // Compatible with all XR SDKs but does not return hand rotations
            return GetDefaultNodeRotation(node);
        }

        /// <summary>
        /// Default fallback for retrieving the rotation of an XRNode when no specific SDK is defined.
        /// </summary>
        /// <param name="node">The XRNode to retrieve the rotation for (e.g., LeftController, RightController, Head).</param>
        /// <returns>The rotation of the specified XRNode, or Quaternion.identity if not found.</returns>
        internal static Quaternion GetDefaultNodeRotation(XRNode node)
        {
            List<XRNodeState> nodeStates = new List<XRNodeState>();
            InputTracking.GetNodeStates(nodeStates);

            foreach (XRNodeState nodeState in nodeStates)
            {
                if (nodeState.nodeType == node && nodeState.TryGetRotation(out var rotation))
                {
                    return rotation;
                }
            }

            return Quaternion.identity;
        }
    }
}
