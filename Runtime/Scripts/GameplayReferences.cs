﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

//static access point to get references to main cameras, controllers, room data

namespace Cognitive3D
{
    public static class GameplayReferences
    {
        internal static void Initialize()
        {
            Cognitive3D_Manager.OnUpdate += Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPostSessionEnd += Cognitive3D_Manager_OnPostSessionEnd;
        }

        private static void Cognitive3D_Manager_OnPostSessionEnd()
        {
            Cognitive3D_Manager.OnUpdate -= Cognitive3D_Manager_OnUpdate;
            Cognitive3D_Manager.OnPostSessionEnd -= Cognitive3D_Manager_OnPostSessionEnd;
        }

#region Oculus Eye and Face Tracking
#if C3D_OCULUS
        //face expressions is cached so it doesn't search every frame, instead just a null check. and only if eyetracking is already marked as supported
        static OVRFaceExpressions cachedOVRFaceExpressions;

        /// <summary>
        /// finds or creates an OVRFaceExpressions component. Used for detecting blinking
        /// </summary>
        public static OVRFaceExpressions OVRFaceExpressions
        {
            get
            {
                if (cachedOVRFaceExpressions == null)
                {
                    cachedOVRFaceExpressions = UnityEngine.Object.FindObjectOfType<OVRFaceExpressions>();
                    if (cachedOVRFaceExpressions == null)
                    {
                        Cognitive3D_Manager.Instance.gameObject.AddComponent<OVRFaceExpressions>();
                    }
                }
                return cachedOVRFaceExpressions;
            }
        }

        /// <summary>
        /// returns if eye tracking and face tracking permissions have been allowed, and eye tracking has been started
        /// </summary>
        public static bool EyeTrackingEnabled
        {
            get;
            internal set;
        }

#endif
#endregion

#region Hand Tracking
        /// <summary>
        /// Represents participant is using hands, controller, or neither
        /// </summary>
        public enum TrackingType
        {
            None = 0,
            Controller = 1,
            Hand = 2
        }

        /// <summary>
        /// Oculus SeGets the current tracked device i.e. hand or controller
        /// </summary>
        /// <returns> Enum representing whether user is using hand or controller or neither </returns>
        public static TrackingType GetCurrentTrackedDevice()
        {
#if C3D_OCULUS
            var currentTrackedDevice = OVRInput.GetConnectedControllers();
            if (currentTrackedDevice == OVRInput.Controller.None)
            {
                return TrackingType.None;
            }
            else if (currentTrackedDevice == OVRInput.Controller.Hands
                || currentTrackedDevice == OVRInput.Controller.LHand
                || currentTrackedDevice == OVRInput.Controller.RHand)
            {
                return TrackingType.Hand;
            }
            else
            {
                return TrackingType.Controller;
            }
#else
            return TrackingType.Controller;
#endif
        }

        #endregion

#region Eye Tracking
        public static bool SDKSupportsEyeTracking
        {
            get
            {
#if C3D_SRANIPAL || C3D_VARJOVR || C3D_VARJOXR || C3D_PICOVR || C3D_OMNICEPT
                return true;
#elif C3D_PICOXR
                return Unity.XR.PXR.PXR_Manager.Instance.eyeTracking;
#elif C3D_MRTK
                return Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.EyeGazeProvider.IsEyeTrackingEnabled;
#elif C3D_VIVEWAVE
                return Wave.Essence.Eye.EyeManager.Instance.IsEyeTrackingAvailable();
#elif C3D_OCULUS

                //just check if eye tracking is supported
                //Cognitive3D_Manager tries to enable Eye/Face tracking to create the fixation recorder component

                bool eyeTrackingSupported = OVRPlugin.eyeTrackingSupported;
                if (!eyeTrackingSupported)
                {
                    return false;
                }

                bool faceTrackingSupported = OVRPlugin.faceTrackingSupported;
                if (!faceTrackingSupported)
                {
                    return false;
                }

                return true;
#elif C3D_DEFAULT
                var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                Eyes eyedata;
                return head.TryGetFeatureValue(CommonUsages.eyesData, out eyedata);
#else
                return false;
#endif
            }
        }
        #endregion

#region Room
        public static bool SDKSupportsRoomSize
        {
            //should be everything except AR SDKS
            get
            {
#if C3D_MRTK
                return false;
#endif
                return true;
            }
        }


        //really simple function to a rect from a collection of points
        //IMPROVEMENT support non-rectangular boundaries
        //IMPROVEMENT support rotated rectangular boundaries
        static Vector3 GetArea(Vector3[] points)
        {
            float minX = 0;
            float maxX = 0;
            float minZ = 0;
            float maxZ = 0;
            foreach (var v in points)
            {
                if (v.x < minX)
                    minX = v.x;
                if (v.x > maxX)
                    maxX = v.x;
                if (v.z < minZ)
                    minZ = v.z;
                if (v.z > maxZ)
                    maxZ = v.z;
            }
            return new Vector3(maxX - minX, 0, maxZ - minZ);
        }

        ///x,y,z is width, height, depth
        ///return value is in meters
        [System.Obsolete]
        public static bool GetRoomSize(ref Vector3 roomSize)
        {
#if C3D_STEAMVR2
            float roomX = 0;
            float roomY = 0;
            if (Valve.VR.OpenVR.Chaperone == null || !Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
            {
                roomSize = new Vector3(roomX, 0, roomY);
                return true;
            }
            else
            {
                return false;
            }
#elif C3D_OCULUS
            if (OVRManager.boundary == null) { return false; }
            if (OVRManager.boundary.GetConfigured())
            {
                roomSize = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
                return true;
            }
            return false;
#elif C3D_PICOXR
            if (Unity.XR.PXR.PXR_Boundary.GetEnabled())
            {
                roomSize = Unity.XR.PXR.PXR_Boundary.GetDimensions(Unity.XR.PXR.BoundaryType.PlayArea);
                return true;
            }
            else
            {
                return false;
            }
#elif C3D_PICOVR
            if (Pvr_UnitySDKAPI.BoundarySystem.UPvr_BoundaryGetEnabled())
            {
                //api returns mm
                roomSize = Pvr_UnitySDKAPI.BoundarySystem.UPvr_BoundaryGetDimensions(Pvr_UnitySDKAPI.BoundarySystem.BoundaryType.PlayArea);
                roomSize /= 1000;
                return true;
            }
            else
            {
                return false;
            }
#else
            UnityEngine.XR.InputDevice inputDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (inputDevice.isValid)
            {
                UnityEngine.XR.InputDevice hMDDevice = inputDevice;
                UnityEngine.XR.XRInputSubsystem XRIS = hMDDevice.subsystem;
                if (XRIS == null)
                {
                    return false;
                }
                List<Vector3> boundaryPoints = new List<Vector3>();
                if (XRIS.TryGetBoundaryPoints(boundaryPoints))
                {
                    roomSize = GetArea(boundaryPoints.ToArray());
                    return true;
                }
            }
            return false;
#endif
        }

#endregion

#region HMD

        private static InputDevice HMDDevice;
        private static Camera cam;
        
        /// <summary>
        /// A refrence to the hmd pointer (if) instantiated for exitpoll
        /// </summary>
        private static GameObject hmdPointer;

        /// <summary>
        /// 
        /// </summary>
        public static GameObject HMDPointer
        {
            get { return hmdPointer; }
            set { hmdPointer = value; }
        }

        public static Camera HMDCameraComponent
        {
            get
            {
                if (cam == null)
                {
                    if (HMD != null)
                    {
                        cam = HMD.GetComponent<Camera>();
                    }
                }
                return cam;
            }
        }

#if C3D_OCULUS
        static OVRCameraRig _cameraRig;
        static OVRCameraRig CameraRig
        {
            get
            {
                if (_cameraRig == null)
                {
                    _cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
                }
                return _cameraRig;
            }
        }
#endif
#if C3D_PICOVR
        static Pvr_UnitySDKManager pvr_UnitySDKManager;
        public static Pvr_UnitySDKManager Pvr_UnitySDKManager
        {
            get
            {
                if (pvr_UnitySDKManager == null)
                {
                    pvr_UnitySDKManager = GameObject.FindObjectOfType<Pvr_UnitySDKManager>();
                }
                return pvr_UnitySDKManager;
            }
        }
#endif
#if C3D_OMNICEPT
        static HP.Omnicept.Unity.GliaBehaviour gliaBehaviour;
        public static HP.Omnicept.Unity.GliaBehaviour GliaBehaviour
        {
            get
            {
                if (gliaBehaviour == null)
                {
                    gliaBehaviour = GameObject.FindObjectOfType<HP.Omnicept.Unity.GliaBehaviour>();
                }
                return gliaBehaviour;
            }
        }
#endif



        private static Transform _hmd;
        /// <summary>Returns HMD based on included SDK, or Camera.Main if no SDK is used. MAY RETURN NULL!</summary>
        public static Transform HMD
        {
            get
            {
                if (_hmd == null)
                {
#if C3D_OCULUS
                    OVRCameraRig rig = CameraRig;
                    if (rig != null)
                    {
                        Camera cam = rig.centerEyeAnchor.GetComponent<Camera>();
                        _hmd = cam.transform;
                    }
#endif
#if C3D_PICOVR
//camera component is disabled, so it isn't returned with Camera.main
                    foreach (var cam in UnityEngine.Object.FindObjectsOfType<Camera>())
                    {
                        if (cam.gameObject.CompareTag("MainCamera"))
                        {
                            _hmd = cam.transform;
                            break;
                        }
                    }
#endif
                    if (_hmd == null)
                    {
                        if (Camera.main != null)
                        {
                            _hmd = Camera.main.transform;
                        }
                    }

                    if (Cognitive3D_Preferences.Instance.EnableLogging)
                        Util.logDebug("HMD set to " + _hmd);
                    if (_hmd == null)
                        Util.logError("No HMD camera found. Is it tagged as 'MainCamera'?");
                }
                return _hmd;
            }
        }

        #endregion

#region Controllers

        /// <summary>
        ///  A refrence to the controller pointer (if) instantiated for exitpoll
        /// </summary>
        private static GameObject pointerController;

        /// <summary>
        /// 
        /// </summary>
        public static GameObject PointerController
        {
            get { return pointerController; }
            set { pointerController = value; }
        }

        public static bool SDKSupportsControllers
        {
            get
            {
                //hand tracking not currently part of the setup process. may work, but not implemented as fully as other SDKs
#if C3D_MRTK
                return false;
#endif
                return true;
            }
        }


        //dynamic objects set as controllers call 'set controller' on enable passing a reference to that transform. input device isn't guaranteed to be valid at this point

        static Transform[] controllerTransforms = new Transform[2];
        static InputDevice[] controllerDevices = new InputDevice[2];

        /// <summary>
        /// Represents how far down the right trigger is pressed <br/>
        /// Usually 0 - 1 (0 = not at all, 1 = fully pressed)
        /// </summary>
        internal static float rightTriggerValue;

        /// <summary>
        /// Represents how far down the left trigger is pressed <br/>
        /// Usually 0 - 1 (0 = not at all, 1 = fully pressed)
        /// </summary>
        internal static float leftTriggerValue;


        /// <summary>
        /// Updates hmd and controller device info
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last frame</param>
        private static void Cognitive3D_Manager_OnUpdate(float deltaTime)
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (head.isValid != HMDDevice.isValid)
            {
                InvokeHMDValidityChangeEvent(head.isValid);
                HMDDevice = head;
            }
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (left.isValid != controllerDevices[0].isValid)
            {
                controllerDevices[0] = left;
                InvokeControllerValidityChangeEvent(left, XRNode.LeftHand, left.isValid);
            }
            left.TryGetFeatureValue(CommonUsages.trigger, out leftTriggerValue);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (right.isValid != controllerDevices[1].isValid)
            {
                controllerDevices[1] = right;
                InvokeControllerValidityChangeEvent(right, XRNode.RightHand, right.isValid);
            }
            right.TryGetFeatureValue(CommonUsages.trigger, out rightTriggerValue);
        }

        public delegate void onDeviceValidityChange(InputDevice device, XRNode node, bool isValid);
        /// <summary>
        /// called after the controller has changed from valid/invalid
        /// </summary>
        public static event onDeviceValidityChange OnControllerValidityChange;
        public static void InvokeControllerValidityChangeEvent(InputDevice device, XRNode node, bool isValid) { if (OnControllerValidityChange != null) { OnControllerValidityChange(device, node, isValid); } }

        public static event onDeviceValidityChange OnHMDValidityChange;
        public static void InvokeHMDValidityChangeEvent(bool isValid) { if (OnHMDValidityChange != null) { OnHMDValidityChange(HMDDevice, XRNode.Head, isValid); } }

        //dynamic object sets itself as a controller. simple way of getting the correct dynamic object
        public static bool SetController(DynamicObject dyn, bool isRight)
        {
            if (isRight)
            {
                controllerTransforms[1] = dyn.transform;
                controllerDevices[1] = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                return controllerDevices[1].isValid;
            }
            else
            {
                controllerTransforms[0] = dyn.transform;
                controllerDevices[0] = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
                return controllerDevices[0].isValid;
            }
        }

        /// <summary>
        /// Returns whether the left controller is currently tracked.
        /// Uses InputDevice.TryGetFeatureUsage API
        /// </summary>
        /// <param name="hand"> The XRNode; either left hand or right hand</param>
        /// <returns>True if left controller is tracking; false otherwise</returns>
        public static bool IsControllerTracking(XRNode hand)
        {
            InputDevice controller = InputDevices.GetDeviceAtXRNode(hand);
            bool isControllerTrackingRef;
            controller.TryGetFeatureValue(CommonUsages.isTracked, out isControllerTrackingRef);
            return isControllerTrackingRef;
        }

        /// <summary>
        /// Returns true if the cached data for this node is valid. for head, lefthand and righthand only
        /// </summary>
        /// <param name="node">The XRNode (we only support head, left hand, right hand)</param>
        /// <returns>True if the device is valid</returns>
        public static bool IsInputDeviceValid(XRNode node)
        {
            switch (node)
            {
                case XRNode.Head:
                    return HMDDevice.isValid;
                case XRNode.LeftHand:
                    return controllerDevices[0].isValid;
                case XRNode.RightHand:
                    return controllerDevices[1].isValid;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="right"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool GetControllerInfo(bool right, out InputDevice info)
        {
            if (right)
            {
                info = controllerDevices[1];
                return controllerDevices[1].isValid;
            }
            else
            {
                info = controllerDevices[0];
                return controllerDevices[0].isValid;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="right"></param>
        /// <param name="transform"></param>
        /// <returns></returns>
        public static bool GetControllerTransform(bool right, out Transform transform)
        {
            if (right)
            {
                transform = controllerTransforms[1];
                return transform != null;
            }
            else
            {
                transform = controllerTransforms[0];
                return transform != null;
            }
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
            position = HMD.transform.parent.TransformPoint(position);
            return position != Vector3.zero;
        }


        /// <summary>
        /// Retrieves the position of a specified XR node (e.g., hand or controller) based on the current tracking type.
        /// Returns Vector3.zero if the node position cannot be determined or if the tracking type is unsupported.
        /// </summary>
        /// <param name="node">The XRNode (e.g., XRNode.LeftHand, XRNode.RightHand) to get the position for.</param>
        private static Vector3 GetNodePosition(XRNode node)
        {
            TrackingType currentTracking = GetCurrentTrackedDevice();

#if C3D_OCULUS
            var targetNode = currentTracking switch
            {
                TrackingType.Controller => node == XRNode.RightHand ? OVRPlugin.Node.ControllerRight : OVRPlugin.Node.ControllerLeft,
                TrackingType.Hand => node == XRNode.RightHand ? OVRPlugin.Node.HandRight : OVRPlugin.Node.HandLeft,
                _ => OVRPlugin.Node.None,
            };

            if (targetNode != OVRPlugin.Node.None)
            {
                return OVRPlugin.GetNodePose(targetNode, OVRPlugin.Step.Physics).ToOVRPose().position;
            }
#elif C3D_PICOXR
            switch (currentTracking)
            {
                case TrackingType.Controller:
                    var controllerNode = node == XRNode.RightHand ? Unity.XR.PXR.PXR_Input.Controller.RightController : Unity.XR.PXR.PXR_Input.Controller.LeftController;
                    return Unity.XR.PXR.PXR_Input.GetControllerPredictPosition(controllerNode, 0);
                case TrackingType.Hand:
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
                case TrackingType.Controller:
                    var controller = node == XRNode.RightHand ? Wave.OpenXR.InputDeviceControl.ControlDevice.Right : Wave.OpenXR.InputDeviceControl.ControlDevice.Left;
                    Wave.OpenXR.InputDeviceControl.GetPosition(controller, out position);
                    return position;

                case TrackingType.Hand:
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
                case TrackingType.Controller:
                    return GetDefaultNodePosition(node);

    #if COGNITIVE3D_INCLUDE_XR_HANDS
                case TrackingType.Hand:
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
            
            rotation = HMD.transform.parent.rotation * rotation;
            return true;
        }

        /// <summary>
        /// Retrieves the rotation of a specified XR node (e.g., hand or controller) based on the current tracking type.
        /// Returns Quaternion.identity if the node rotation cannot be determined or if the tracking type is unsupported.
        /// </summary>
        /// <param name="node">The XRNode (e.g., XRNode.LeftHand, XRNode.RightHand) to get the rotation for.</param>
        private static Quaternion GetNodeRotation(XRNode node)
        {
            TrackingType currentTracking = GetCurrentTrackedDevice();

#if C3D_OCULUS
            var targetNode = currentTracking switch
            {
                TrackingType.Controller => node == XRNode.RightHand ? OVRPlugin.Node.ControllerRight : OVRPlugin.Node.ControllerLeft,
                TrackingType.Hand => node == XRNode.RightHand ? OVRPlugin.Node.HandRight : OVRPlugin.Node.HandLeft,
                _ => OVRPlugin.Node.None,
            };

            if (targetNode != OVRPlugin.Node.None)
            {
                return OVRPlugin.GetNodePose(targetNode, OVRPlugin.Step.Physics).ToOVRPose().orientation;
            }
#elif C3D_PICOXR
            switch (currentTracking)
            {
                case TrackingType.Controller:
                    var controllerNode = node == XRNode.RightHand ? Unity.XR.PXR.PXR_Input.Controller.RightController : Unity.XR.PXR.PXR_Input.Controller.LeftController;
                    return Unity.XR.PXR.PXR_Input.GetControllerPredictRotation(controllerNode, 0);
                case TrackingType.Hand:
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
                case TrackingType.Controller:
                    var controller = node == XRNode.RightHand ? Wave.OpenXR.InputDeviceControl.ControlDevice.Right : Wave.OpenXR.InputDeviceControl.ControlDevice.Left;
                    Wave.OpenXR.InputDeviceControl.GetRotation(controller, out rotation);
                    return rotation;

                case TrackingType.Hand:
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
                case TrackingType.Controller:
                    return GetDefaultNodeRotation(node);

    #if COGNITIVE3D_INCLUDE_XR_HANDS
                case TrackingType.Hand:
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


#endregion

#region Location

#if C3D_LOCATION
        /// <summary>
        /// starts location services if not already active.
        /// writes location latitude, longitude, altitude and compass heading as x,y,z,w respectively
        /// </summary>
        /// <param name="loc"></param>
        /// <returns>true if location data is available</returns>
        public static bool TryGetGPSLocation(ref Vector4 loc)
        {
            if (!Input.location.isEnabledByUser)
            {
                return false;
            }
            if (Input.location.status == LocationServiceStatus.Stopped)
            {
                Input.location.Start(Cognitive3D_Preferences.Instance.GPSAccuracy, Cognitive3D_Preferences.Instance.GPSAccuracy);
            }
            if (Input.location.status != LocationServiceStatus.Running)
            {                
                return false;
            }
            loc.x = Input.location.lastData.latitude;
            loc.y = Input.location.lastData.longitude;
            loc.z = Input.location.lastData.altitude;
            loc.w = 360 - Input.compass.magneticHeading;
            return true;
        }

        /// <summary>
        /// returns true if C3D_LOCATION is an included symbol
        /// </summary>
        /// <returns></returns>
        public static bool IsGPSLocationEnabled()
        {
            return true;
        }
#else
        /// <summary>
        /// starts location services if not already active.
        /// writes location latitude, longitude, altitude and compass heading as x,y,z,w respectively
        /// </summary>
        /// <param name="loc"></param>
        /// <returns>true if location data is available</returns>
        public static bool TryGetGPSLocation(ref Vector4 loc)
        {
            return false;
        }

        /// <summary>
        /// returns true if C3D_LOCATION is an included symbol
        /// </summary>
        /// <returns></returns>
        public static bool IsGPSLocationEnabled()
        {
            return false;
        }
#endif

#endregion
    }
}
