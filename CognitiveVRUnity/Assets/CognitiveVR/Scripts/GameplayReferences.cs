using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//static access point to get references to main cameras, controllers, room data

namespace CognitiveVR
{
    public static class GameplayReferences
    {
        public static bool SDKSupportsEyeTracking
        {
            get
            {
#if CVR_TOBIIVR || CVR_AH || CVR_FOVE || CVR_PUPIL || CVR_VIVEPROEYE || CVR_VARJO || CVR_PICOVR || CVR_XR || CVR_OMNICEPT
                return true;
#elif CVR_PICOXR
                return Unity.XR.PXR.PXR_Manager.Instance.eyeTracking;
#elif CVR_MRTK
                return Microsoft.MixedReality.Toolkit.CoreServices.InputSystem.EyeGazeProvider.IsEyeTrackingEnabled;
#else
                return false;
#endif
            }
        }
        public static bool SDKSupportsControllers
        {
            get
            {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS || CVR_VIVEWAVE || CVR_PICOVR || CVR_PICOXR || CVR_XR || CVR_WINDOWSMR || CVR_VARJO || CVR_SNAPDRAGON || CVR_OMNICEPT
                return true;
#else
                return false;
#endif
            }
        }
        public static bool SDKSupportsRoomSize
        {
            get
            {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS || CVR_XR || CVR_PICOVR || CVR_PICOXR
                return true;
#else
                return false;
#endif
            }
        }

        #region HMD and Controllers

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

        ///x,y,z is width, height, depth
        ///return value is in meters
        public static bool GetRoomSize(ref Vector3 roomSize)
        {
#if CVR_STEAMVR || CVR_STEAMVR2
            float roomX = 0;
            float roomY = 0;
            if (Valve.VR.OpenVR.Chaperone == null || !Valve.VR.OpenVR.Chaperone.GetPlayAreaSize(ref roomX, ref roomY))
            {
                roomSize = new Vector3(roomX,0,roomY);
                return true;
            }
            else
            {
                return false;
            }
#elif CVR_OCULUS
            if (OVRManager.boundary == null) { return false; }
            if (OVRManager.boundary.GetConfigured())
            {
                roomSize = OVRManager.boundary.GetDimensions(OVRBoundary.BoundaryType.PlayArea);
                return true;
            }
            return false;
#elif CVR_XR || CVR_WINDOWSMR || CVR_VARJO
            List<UnityEngine.XR.InputDevice> inputDevices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, inputDevices);
            if (inputDevices.Count > 0)
            {
                UnityEngine.XR.InputDevice hMDDevice = inputDevices[0];
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
#elif CVR_PICOXR
            if (Unity.XR.PXR.PXR_Boundary.GetEnabled())
            {
                roomSize = Unity.XR.PXR.PXR_Boundary.GetDimensions(Unity.XR.PXR.BoundaryType.PlayArea);
                return true;
            }
            else
            {
                return false;
            }
#elif CVR_PICOVR
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
            return false;
#endif
        }

#if CVR_OCULUS
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

#if CVR_FOVE
        static Fove.Unity.FoveInterface _foveInstance;
        public static Fove.Unity.FoveInterface FoveInstance
        {
            get
            {
                if (_foveInstance == null)
                {
                    _foveInstance = GameObject.FindObjectOfType<Fove.Unity.FoveInterface>();
                }
                return _foveInstance;
            }
        }
#endif
#if CVR_PICOVR
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
#if CVR_OMNICEPT
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
#if CVR_PUPIL
        static PupilLabs.GazeController gazeController;
        public static PupilLabs.GazeController GazeController
        {
            get
            {
                if (gazeController == null)
                {
                    gazeController = GameObject.FindObjectOfType<PupilLabs.GazeController>();
                }
                return gazeController;
            }
        }
        static PupilLabs.CalibrationController calibrationController;
        public static PupilLabs.CalibrationController CalibrationController
        {
            get
            {
                if (calibrationController == null)
                {
                    calibrationController = GameObject.FindObjectOfType<PupilLabs.CalibrationController>();
                }
                return calibrationController;
            }
        }
#endif
#if CVR_STEAMVR
        static SteamVR_Camera steamVR_Camera;
        public static SteamVR_Camera SteamVR_Camera
        {
            get
            {
                if (steamVR_Camera == null)
                {
                    steamVR_Camera = GameObject.FindObjectOfType<SteamVR_Camera>();
                }
                return steamVR_Camera;
            }
        }
        static Valve.VR.InteractionSystem.Player player;
        public static Valve.VR.InteractionSystem.Player Player
        {
            get
            {
                if (player == null)
                {
                    player = GameObject.FindObjectOfType<Valve.VR.InteractionSystem.Player>();
                }
                return player;
            }
        }
                static SteamVR_ControllerManager controllerManager;
        public static SteamVR_ControllerManager ControllerManager
        {
            get
            {
                if (controllerManager == null)
                {
                    controllerManager = GameObject.FindObjectOfType<SteamVR_ControllerManager>();
                }
                return controllerManager;
            }
        }
#endif

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
                    maxZ = v.z;
                if (v.z > maxZ)
                    minZ = v.z;
            }
            return new Vector3(maxX - minX, 0, maxZ - minZ);
        }

        private static Transform _hmd;
        /// <summary>Returns HMD based on included SDK, or Camera.Main if no SDK is used. MAY RETURN NULL!</summary>
        public static Transform HMD
        {
            get
            {
                if (_hmd == null)
                {
#if CVR_STEAMVR
                    SteamVR_Camera cam = GameplayReferences.SteamVR_Camera;
                    if (cam != null){ _hmd = cam.transform; }
#elif CVR_OCULUS
                    OVRCameraRig rig = CameraRig;
                    if (rig != null)
                    {
                        Camera cam = rig.centerEyeAnchor.GetComponent<Camera>();
                        _hmd = cam.transform;
                    }
#elif CVR_FOVE
                    var fi = FoveInstance;
                    if (fi != null)
                    {
                        _hmd = fi.transform;
                    }
#elif CVR_VIVEWAVE
                    var cameras = GameObject.FindObjectsOfType<WaveVR_Camera>();
                    for (int i = 0; i < cameras.Length; i++)
                    {
                        if (cameras[i].eye == wvr.WVR_Eye.WVR_Eye_Both)
                        {
                            _hmd = cameras[i].transform;
                            break;
                        }
                    }
#endif
#if CVR_PICOVR
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

                    if (CognitiveVR_Preferences.Instance.EnableLogging)
                        Util.logDebug("HMD set to " + _hmd);
                    if (_hmd == null)
                        Util.logError("No HMD camera found. Is it tagged as 'MainCamera'?");
                }
                return _hmd;
            }
        }

#if CVR_OCULUS
        //records controller transforms from either interaction player or behaviour poses
        static void InitializeControllers()
        {
            if (CameraRig == null) { return; } //TODO IMPROVE could be calling FindObjectOfType every frame

            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
            }

            if (controllers[0] == null)
            {
                controllers[0] = new ControllerInfo() { transform = CameraRig.leftHandAnchor, isRight = false, id = 1 };
                controllers[0].connected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
                controllers[0].visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);

            }

            if (controllers[1] == null)
            {
                controllers[1] = new ControllerInfo() { transform = CameraRig.rightHandAnchor, isRight = true, id = 2 };
                controllers[1].connected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
                controllers[1].visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
            }
        }
#elif CVR_STEAMVR2

        static Valve.VR.SteamVR_Behaviour_Pose[] poses;

        //records controller transforms from either interaction player or behaviour poses
        static void InitializeControllers()
        {
            if (controllers == null || controllers[0].transform == null || controllers[1].transform == null)
            {
                if (controllers == null)
                {
                    controllers = new ControllerInfo[2];
                    controllers[0] = new ControllerInfo() { transform = null, isRight = false, id = -1 };
                    controllers[1] = new ControllerInfo() { transform = null, isRight = false, id = -1 };
                }

                if (poses != null)
                {
                    for (int i = 0; i < poses.Length; i++)
                    {
                        if (poses[i] == null)
                        {
                            poses = null;
                            break;
                        }
                    }
                }

                if (poses == null)
                {
                    poses = GameObject.FindObjectsOfType<Valve.VR.SteamVR_Behaviour_Pose>();
                }
                if (poses != null && poses.Length > 1)
                {
                    controllers[0].transform = poses[0].transform;
                    controllers[1].transform = poses[1].transform;
                    controllers[0].isRight = poses[0].inputSource == Valve.VR.SteamVR_Input_Sources.RightHand;
                    controllers[1].isRight = poses[1].inputSource == Valve.VR.SteamVR_Input_Sources.RightHand;
                    controllers[0].id = poses[0].GetDeviceIndex();
                    controllers[1].id = poses[1].GetDeviceIndex();
                }
            }
        }

#elif CVR_STEAMVR

        static SteamVR_ControllerManager cm;
        static Valve.VR.InteractionSystem.Player player;

        static void InitializeControllers()
        {
            if (controllers != null && controllers[0].transform != null && controllers[1].transform != null && controllers[0].id >0 && controllers[1].id > 0) {return;}

            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
            }
            //try to initialize with controllermanager
            //otherwise try to initialize with player.hands
            if (cm == null)
            {
                cm = GameplayReferences.SteamVR_ControllerManager;
            }
            if (cm != null)
            {
                var left = cm.left.GetComponent<SteamVR_TrackedObject>();
                controllers[0].transform = left.transform;
                controllers[0].id = (int)left.index;
                controllers[0].isRight = false;
                if (left.index != SteamVR_TrackedObject.EIndex.None)
                {
                    controllers[0].connected = SteamVR_Controller.Input((int)left.index).connected;
                    controllers[0].visible = SteamVR_Controller.Input((int)left.index).valid;
                }
                else
                {
                    controllers[0].connected = false;
                    controllers[0].visible = false;
                }

                var right = cm.right.GetComponent<SteamVR_TrackedObject>();
                controllers[1].transform = right.transform;
                controllers[1].id = (int)right.index;
                controllers[1].isRight = true;
                if (right.index != SteamVR_TrackedObject.EIndex.None)
                {
                    controllers[1].connected = SteamVR_Controller.Input((int)right.index).connected;
                    controllers[1].visible = SteamVR_Controller.Input((int)right.index).valid;
                }
                else
                {
                    controllers[1].connected = false;
                    controllers[1].visible = false;
                }
            }
            else
            {
                if (player == null)
                {
                    player = GameplayReferences.Player;
                }
                if (player != null)
                {
                    var left = player.leftHand;
                    if (left != null && left.controller != null)
                    {
                        controllers[0].transform = player.leftHand.transform;
                        controllers[0].id = (int)player.leftHand.controller.index;
                        controllers[0].isRight = false;
                        controllers[0].connected = left.controller.connected;
                        controllers[0].visible = left.controller.valid;
                    }

                    var right = player.rightHand;
                    if (right != null && right.controller != null)
                    {
                        controllers[1].transform = player.rightHand.transform;
                        controllers[1].id = (int)player.rightHand.controller.index;
                        controllers[1].isRight = true;
                        controllers[1].connected = right.controller.connected;
                        controllers[1].visible = right.controller.valid;
                    }
                }
            }

        }
#elif CVR_VIVEWAVE

        //no clear way to get vive wave controller reliably. wave controller dynamics call this when enabled
        public static void SetController(GameObject go, bool isRight)
        {
            InitializeControllers();
            if (isRight)
            {
                controllers[1].transform = go.transform;
                controllers[1].isRight = true;
                controllers[1].id = WaveVR.Instance.controllerRight.index;
                controllers[1].connected = WaveVR.Instance.controllerRight.connected;
                controllers[1].visible = WaveVR.Instance.controllerRight.pose.pose.IsValidPose;
            }
            else
            {
                controllers[0].transform = go.transform;
                controllers[0].isRight = false;
                controllers[0].id = WaveVR.Instance.controllerLeft.index;
                controllers[0].connected = WaveVR.Instance.controllerLeft.connected;
                controllers[0].visible = WaveVR.Instance.controllerLeft.pose.pose.IsValidPose;
            }
        }

        static void InitializeControllers()
        {
            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
            }
        }
#elif CVR_WINDOWSMR

        //no clear way to get controller reliably. dynamic object calls this when enabled
        public static void SetController(GameObject go, bool isRight)
        {
            InitializeControllers();
            if (isRight)
            {
                controllers[1].transform = go.transform;
                controllers[1].isRight = true;
                controllers[1].connected = true;
                controllers[1].visible = true;
                controllers[1].id = 1;
            }
            else
            {
                controllers[0].transform = go.transform;
                controllers[0].isRight = false;
                controllers[0].connected = true;
                controllers[0].visible = true;
                controllers[0].id = 0;
            }
        }

        static void InitializeControllers()
        {
            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
            }
        }
#elif CVR_PICOVR
        static void InitializeControllers()
        {
            if (controllers == null)
            {
                var manager = Pvr_ControllerManager.Instance;
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
                if (manager != null)
                {
                    var pico_controller = manager.GetComponent<Pvr_Controller>();
                    controllers[0].transform = pico_controller.controller0.transform;
                    controllers[0].isRight = false;
                    controllers[0].connected = true;
                    controllers[0].visible = true;
                    controllers[0].id = 0;
                    controllers[1].transform = pico_controller.controller1.transform;
                    controllers[1].isRight = true;
                    controllers[1].connected = true;
                    controllers[1].visible = true;
                    controllers[1].id = 1;
                }
            }
        }
#elif CVR_PICOXR
        static void InitializeControllers()
        {
            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
            }
        }

        public static void SetController(GameObject go, bool isRight)
        {
            InitializeControllers();
            if (isRight)
            {
                controllers[1].transform = go.transform;
                controllers[1].isRight = true;
                controllers[1].connected = true;
                controllers[1].visible = true;
                controllers[1].id = 1;
            }
            else
            {
                controllers[0].transform = go.transform;
                controllers[0].isRight = false;
                controllers[0].connected = true;
                controllers[0].visible = true;
                controllers[0].id = 0;
            }
        }

        public static bool GetEyeTrackingDevice(out UnityEngine.XR.InputDevice device)
        {
            //TODO cache input device and check if 'IsValid'
            device = default(UnityEngine.XR.InputDevice);

            if (!Unity.XR.PXR.PXR_Manager.Instance.eyeTracking)
                return false;

            List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
            UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(UnityEngine.XR.InputDeviceCharacteristics.EyeTracking | UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, devices);
            if (devices.Count == 0)
            {
                Debug.Log("Failed at GetEyeTrackingDevice 0");
                return false;
            }
            device = devices[0];
            if (!device.isValid)
                Debug.Log("Failed at GetEyeTrackingDevice 1");
            return device.isValid;
        }
#elif CVR_XR

        //no clear way to get controller reliably. dynamic object calls this when enabled
        public static void SetController(GameObject go, bool isRight)
        {
            InitializeControllers();
            if (isRight)
            {
                controllers[1].transform = go.transform;
                controllers[1].isRight = true;
                controllers[1].connected = true;
                controllers[1].visible = true;
                controllers[1].id = 1;
            }
            else
            {
                controllers[0].transform = go.transform;
                controllers[0].isRight = false;
                controllers[0].connected = true;
                controllers[0].visible = true;
                controllers[0].id = 0;
            }
        }

        static void InitializeControllers()
        {
            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
            }
        }
#else
        static void InitializeControllers()
        {
            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
                controllers[0] = new ControllerInfo();
                controllers[1] = new ControllerInfo();
            }
        }
#endif

        public static IControllerPointer ControllerPointerLeft;
        public static IControllerPointer ControllerPointerRight;

        public static bool DoesPointerExistInScene()
        {
            InitializeControllers();
            if (ControllerPointerLeft == null && controllers[0].transform != null)
                ControllerPointerLeft = controllers[0].transform.GetComponent<IControllerPointer>();
            if (ControllerPointerRight == null && controllers[1].transform != null)
                ControllerPointerRight = controllers[1].transform.GetComponent<IControllerPointer>();
            if (ControllerPointerRight == null && ControllerPointerLeft == null)
            {
                return false;
            }
            return true;
        }


        public class ControllerInfo
        {
            public Transform transform;
            public bool isRight;
            public int id = -1;

            public bool connected;
            public bool visible;
        }

        static ControllerInfo[] controllers;

        public static bool GetControllerInfo(int deviceID, out ControllerInfo info)
        {
            InitializeControllers();
            if (controllers[0].id == deviceID && controllers[0].transform != null) { info = controllers[0]; return true; }
            if (controllers[1].id == deviceID && controllers[1].transform != null) { info = controllers[1]; return true; }
            info = null;
            return false;
        }

        public static bool GetControllerInfo(bool right, out ControllerInfo info)
        {
            InitializeControllers();
            if (controllers[0].isRight == right && controllers[0].id >= 0 && controllers[0].transform != null) { info = controllers[0]; return true; }
            if (controllers[1].isRight == right && controllers[1].id >= 0 && controllers[1].transform != null) { info = controllers[1]; return true; }
            info = null;
            return false;
        }


        /// <summary>
        /// steamvr ID is tracked device id
        /// oculus ID 0 is right, 1 is left controller
        /// </summary>
        public static bool GetController(int deviceid, out Transform transform)
        {
            if (SDKSupportsControllers)
            {
                InitializeControllers();
                if (controllers[0].id == deviceid) { transform = controllers[0].transform; return true; }
                if (controllers[1].id == deviceid) { transform = controllers[1].transform; return true; }
                transform = null;
                return false;
            }
            else
            {
                transform = null;
                return false;
            }
        }

        public static bool GetController(bool right, out Transform transform)
        {
            if (SDKSupportsControllers)
            {
                InitializeControllers();
                if (right == controllers[0].isRight && controllers[0].id >= 0) { transform = controllers[0].transform; return true; }
                if (right == controllers[1].isRight && controllers[1].id >= 0) { transform = controllers[1].transform; return true; }
                transform = null;
                return false;
            }
            else
            {
                transform = null;
                return false;
            }
        }

        /// <summary>Returns Tracked Controller position by index. Based on SDK</summary>
        public static bool GetControllerPosition(bool right, out Vector3 position)
        {
            if (SDKSupportsControllers)
            {

                InitializeControllers();
                if (right == controllers[0].isRight && controllers[0].transform != null && controllers[0].id >= 0) { position = controllers[0].transform.position; return true; }
                if (right == controllers[1].isRight && controllers[1].transform != null && controllers[1].id >= 0) { position = controllers[1].transform.position; return true; }
                position = Vector3.zero;
                return false;
            }
            else
            {
                position = Vector3.zero;
                return false;
            }
        }

#endregion
    }
}