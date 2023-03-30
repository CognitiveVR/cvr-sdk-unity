using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

//static access point to get references to main cameras, controllers, room data

namespace Cognitive3D
{
    public static class GameplayReferences
    {
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
#elif C3D_DEFAULT
                var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                Eyes eyedata;
                return head.TryGetFeatureValue(CommonUsages.eyesData, out eyedata);
#else
                return false;
#endif
            }
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

        //updates controller and hmd inputdevices to call events when states change
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
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (right.isValid != controllerDevices[1].isValid)
            {
                controllerDevices[1] = right;
                InvokeControllerValidityChangeEvent(right, XRNode.RightHand, right.isValid);
            }
        }

#region Room

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

        static InputDevice HMDDevice;

        private static Camera cam;
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
                    foreach (var cam in Object.FindObjectsOfType<Camera>())
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

        //dynamic objects set as controllers call 'set controller' on enable passing a reference to that transform. input device isn't guaranteed to be valid at this point

        static Transform[] controllerTransforms = new Transform[2];
        static InputDevice[] controllerDevices = new InputDevice[2];


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

        public static IControllerPointer ControllerPointerLeft;
        public static IControllerPointer ControllerPointerRight;

        public static bool DoesPointerExistInScene()
        {
            if (ControllerPointerLeft == null && controllerTransforms[0] != null)
            {
                ControllerPointerLeft = controllerTransforms[0].GetComponent<IControllerPointer>();
            }
            if (ControllerPointerRight == null && controllerTransforms[1] != null)
            {
                ControllerPointerRight = controllerTransforms[1].GetComponent<IControllerPointer>();
            }
            if (ControllerPointerRight == null && ControllerPointerLeft == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// this function returns true if the cached data for this node is valid. for head, lefthand and righthand only
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
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
