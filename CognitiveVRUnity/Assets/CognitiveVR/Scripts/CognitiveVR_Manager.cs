using UnityEngine;
using CognitiveVR;
using System.Collections;
using System.Collections.Generic;
using System;
#if CVR_STEAMVR || CVR_STEAMVR2
using Valve.VR;
#endif

#if CVR_META
using System.Runtime.InteropServices;
#endif

/// <summary>
/// initializes CognitiveVR analytics. Add components to track additional events
/// </summary>

//init components
//update ticks + events
//level change events
//get hmd + controllers
//quit and destroy events
//otherwise use core

namespace CognitiveVR
{
    public partial class CognitiveVR_Manager : MonoBehaviour
    {

#if CVR_META
        [DllImport("MetaVisionDLL", EntryPoint = "getSerialNumberAndCalibration")]
        internal static extern bool GetSerialNumberAndCalibration([MarshalAs(UnmanagedType.BStr), Out] out string serial, [MarshalAs(UnmanagedType.BStr), Out] out string xml);
#endif

        #region Events
        public delegate void CoreInitHandler(Error initError);
        /// <summary>
        /// CognitiveVR Core.Init callback
        /// </summary>
        public static event CoreInitHandler InitEvent;
        public void OnInit(Error initError)
        {
            Util.logDebug("CognitiveVR OnInit recieved response " + initError.ToString());
            if (initError == Error.AlreadyInitialized)
            {
                return;
            }
            initResponse = initError;

            OutstandingInitRequest = false;

            if (initError == Error.Success)
            {
                new CustomEvent("Session Begin").Send();
                if (CognitiveVR_Preferences.Instance.TrackGPSLocation)
                {
                    Input.location.Start(CognitiveVR_Preferences.Instance.GPSAccuracy, CognitiveVR_Preferences.Instance.GPSAccuracy);
                    Input.compass.enabled = true;
                    if (CognitiveVR_Preferences.Instance.SyncGPSWithGaze)
                    {
                        //just get gaze snapshot to grab this
                    }
                    else
                    {
                        StartCoroutine(GPSTick());
                    }
                }
            }
            else //some failure
            {
                StopAllCoroutines();
            }

            InitializeControllers();

            var components = GetComponentsInChildren<CognitiveVR.Components.CognitiveVRAnalyticsComponent>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].CognitiveVR_Init(initError);
            }

            //PlayerRecorderInit(initError);

            switch (CognitiveVR_Preferences.Instance.GazeType)
            {
                case GazeType.Physics: gameObject.AddComponent<PhysicsGaze>().Initialize(); break;
                case GazeType.Command: gameObject.AddComponent<CommandGaze>().Initialize(); break;
                case GazeType.Depth: gameObject.AddComponent<DepthGaze>().Initialize(); break;
                //case GazeType.Sphere: gameObject.AddComponent<SphereGaze>().Initialize(); break;
            }

            if (InitEvent != null) { InitEvent(initError); }

            //required for when restarting cognitiveVR manager
            /*foreach (var d in InitEvent.GetInvocationList())
            {
                InitEvent -= (CoreInitHandler)d;
            }*/

#if CVR_META
            string serialnumber;
            string xml;
            if (GetSerialNumberAndCalibration(out serialnumber, out xml))
            {              
                var metaProperties = new Dictionary<string,object>();
                metaProperties.Add("cvr.vr.serialnumber",serialnumber);
                Core.UpdateSessionState(metaProperties);
                //Instrumentation.updateDeviceState(metaProperties);
            }
#elif CVR_STEAMVR

            string serialnumber = null;

            var error = ETrackedPropertyError.TrackedProp_Success;
            var result = new System.Text.StringBuilder();

            var capacity = OpenVR.System.GetStringTrackedDeviceProperty(0, ETrackedDeviceProperty.Prop_SerialNumber_String, result, 64, ref error);
            if (capacity > 0)
                serialnumber = result.ToString();

            if (!string.IsNullOrEmpty(serialnumber))
            {
                var properties = new Dictionary<string, object>();
                properties.Add("cvr.vr.serialnumber", serialnumber);

                Core.UpdateSessionState(properties);
                //Instrumentation.updateDeviceState(properties);
            }
#endif
        }

        public delegate void UpdateHandler();
        /// <summary>
        /// Update. Called through Manager's update function for easy enabling/disabling
        /// </summary>
        public static event UpdateHandler UpdateEvent;
        public void OnUpdate() { if (UpdateEvent != null) { UpdateEvent(); } }

        public delegate void TickHandler();
        /// <summary>
        /// repeatedly called. interval is CognitiveVR_Preferences.Instance.PlayerSnapshotInterval. Only if the sceneid is valid
        /// </summary>
        public static event TickHandler TickEvent;
        public void OnTick() { if (TickEvent != null) { TickEvent(); } }

        public delegate void QuitHandler(); //quit
        /// <summary>
        /// called from Unity's built in OnApplicationQuit. Cancelling quit gets weird - do all application quit stuff in Manager
        /// </summary>
        public static event QuitHandler QuitEvent;
        public void OnQuit() { if (QuitEvent != null) { QuitEvent(); } }

        public delegate void LevelLoadedHandler(); //level
        /// <summary>
        /// called from Unity's SceneManager.SceneLoaded(scene scene)
        /// </summary>
        public static event LevelLoadedHandler LevelLoadedEvent;
        public void OnLevelLoaded() { if (LevelLoadedEvent != null) { LevelLoadedEvent(); } }

#if CVR_STEAMVR
        //1.1
        /*
        public delegate void PoseUpdateHandler(params object[] args);
        /// <summary>
        /// params are SteamVR pose args. does not check index. Currently only used for TrackedDevice valid/disconnected
        /// </summary>
        public static event PoseUpdateHandler PoseUpdateEvent;
        public void OnPoseUpdate(params object[] args) { if (PoseUpdateEvent != null) { PoseUpdateEvent(args); } }
        */

        //1.2
        public delegate void PoseUpdateHandler(params TrackedDevicePose_t[] args);
        /// <summary>
        /// params are SteamVR pose args. does not check index. Currently only used for TrackedDevice valid/disconnected
        /// </summary>
        public static event PoseUpdateHandler PoseUpdateEvent;
        public void OnPoseUpdate(params TrackedDevicePose_t[] args) { if (PoseUpdateEvent != null) { PoseUpdateEvent(args); } }

        //1.1 and 1.2
        public delegate void PoseEventHandler(Valve.VR.EVREventType eventType);
        /// <summary>
        /// polled in Update. sends all events from Valve.VR.OpenVR.System.PollNextEvent(ref vrEvent, size)
        /// </summary>
        public static event PoseEventHandler PoseEvent;
        public void OnPoseEvent(Valve.VR.EVREventType eventType) { if (PoseEvent != null) { PoseEvent(eventType); } }
#endif
#if CVR_STEAMVR2
        public delegate void PoseUpdateHandler(params Valve.VR.TrackedDevicePose_t[] args);
        /// <summary>
        /// params are SteamVR pose args. does not check index. Currently only used for TrackedDevice valid/disconnected
        /// </summary>
        public static event PoseUpdateHandler PoseUpdateEvent;
        public void OnPoseUpdate(params Valve.VR.TrackedDevicePose_t[] args) { if (PoseUpdateEvent != null) { PoseUpdateEvent(args); } }

        //1.1 and 1.2
        public delegate void PoseEventHandler(Valve.VR.EVREventType eventType);
        /// <summary>
        /// polled in Update. sends all events from Valve.VR.OpenVR.System.PollNextEvent(ref vrEvent, size)
        /// </summary>
        public static event PoseEventHandler PoseEvent;
        public void OnPoseEvent(Valve.VR.EVREventType eventType) { if (PoseEvent != null) { PoseEvent(eventType); } }
#endif
        #endregion

        #region HMD and Controllers


#if CVR_OCULUS
        static OVRCameraRig _cameraRig;
        static OVRCameraRig CameraRig
        {
            get
            {
                if (_cameraRig == null)
                {
                    _cameraRig = FindObjectOfType<OVRCameraRig>();
                }
                return _cameraRig;
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
#if CVR_STEAMVR
                    SteamVR_Camera cam = FindObjectOfType<SteamVR_Camera>();
                    if (cam != null){ _hmd = cam.transform; }
                    if (_hmd == null)
                    {
                        if (Camera.main == null)
                            _hmd = FindObjectOfType<Camera>().transform;
                        else
                            _hmd = Camera.main.transform;
                    }
#elif CVR_OCULUS
                    OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
                    if (rig != null)
                    {
                        Camera cam = rig.GetComponentInChildren<Camera>();
                        _hmd = cam.transform;
                    }
                    if (_hmd == null)
                    {
                        if (Camera.main == null)
                            _hmd = FindObjectOfType<Camera>().transform;
                        else
                            _hmd = Camera.main.transform;
                    }
#elif CVR_FOVE
                    /*FoveEyeCamera eyecam = FindObjectOfType<FoveEyeCamera>();
                    if (eyecam != null)
                    {
                        Camera cam = eyecam.GetComponentInChildren<Camera>();
                        _hmd = cam.transform;
                    }*/
                    if (_hmd == null)
                    {
                        if (Camera.main == null)
                            _hmd = FindObjectOfType<Camera>().transform;
                        else
                            _hmd = Camera.main.transform;
                    }
#elif CVR_SNAPDRAGON
                    _hmd = FindObjectOfType<Camera>().transform;
#else
                    if (Camera.main == null)
                        _hmd = FindObjectOfType<Camera>().transform;
                    else
                        _hmd = Camera.main.transform;

#endif
                }
                return _hmd;
            }
        }

#if CVR_OCULUS
        //records controller transforms from either interaction player or behaviour poses
        static void InitializeControllers()
        {
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

                if (poses == null)
                {
                    poses = FindObjectsOfType<Valve.VR.SteamVR_Behaviour_Pose>();
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
                cm = FindObjectOfType<SteamVR_ControllerManager>();
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
                    player = FindObjectOfType<Valve.VR.InteractionSystem.Player>();
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
#else
        static void InitializeControllers()
        {
            if (controllers == null)
            {
                controllers = new ControllerInfo[2];
            }
        }
#endif




        public class ControllerInfo
        {
            public Transform transform;
            public bool isRight;
            public int id = -1;

            public bool connected;
            public bool visible;
        }

        static ControllerInfo[] controllers;

        public static ControllerInfo GetControllerInfo(int deviceID)
        {
            InitializeControllers();
            if (controllers[0].id == deviceID) { return controllers[0]; }
            if (controllers[1].id == deviceID) { return controllers[1]; }
            return null;
        }

        public static ControllerInfo GetControllerInfo(bool right)
        {
            InitializeControllers();
            if (controllers[0].isRight == right && controllers[0].id > 0) { return controllers[0]; }
            if (controllers[1].isRight == right && controllers[1].id > 0) { return controllers[1]; }
            return null;
        }


        /// <summary>
        /// steamvr ID is tracked device id
        /// oculus ID 0 is right, 1 is left controller
        /// </summary>
        public static Transform GetController(int deviceid)
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS
            InitializeControllers();
            if (controllers[0].id == deviceid) { return controllers[0].transform; }
            if (controllers[1].id == deviceid) { return controllers[1].transform; }
            return null;
#else
            return null;
#endif
        }

        public static Transform GetController(bool right)
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS
            InitializeControllers();
            if (right == controllers[0].isRight && controllers[0].id > 0) { return controllers[0].transform; }
            if (right == controllers[1].isRight && controllers[1].id > 0) { return controllers[1].transform; }
            return null;
#else
            return null;
#endif
        }

        /// <summary>Returns Tracked Controller position by index. Based on SDK</summary>
        public static Vector3 GetControllerPosition(bool right)
        {
#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS

            InitializeControllers();
            if (right == controllers[0].isRight && controllers[0].transform != null && controllers[0].id > 0) { return controllers[0].transform.position; }
            if (right == controllers[1].isRight && controllers[1].transform != null && controllers[1].id > 0) { return controllers[1].transform.position; }
            return Vector3.zero;
#else
            return Vector3.zero;
#endif
        }

        #endregion

        private static CognitiveVR_Manager instance;
        public static CognitiveVR_Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CognitiveVR_Manager>();
                }
                return instance;
            }
        }
        YieldInstruction playerSnapshotInverval;
        YieldInstruction GPSUpdateInverval;

        [Tooltip("Enable automatic initialization. If false, you must manually call Initialize()")]
        public bool InitializeOnStart = true;

        [HideInInspector] //complete this option later
        [Tooltip("Save ExitPoll questions and answers to disk if internet connection is unavailable")]
        public bool SaveExitPollOnDevice = false;

        static Error initResponse = Error.NotInitialized;
        public static Error InitResponse { get { return initResponse; } }
        bool OutstandingInitRequest = false;

        public float StartupDelayTime = 2;

        private void OnEnable()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
        }

        IEnumerator Start()
        {
            GameObject.DontDestroyOnLoad(gameObject);
            if (StartupDelayTime > 0)
            {
                yield return new WaitForSeconds(StartupDelayTime);
            }
            if (InitializeOnStart)
                Initialize("");
        }

        private void OnValidate()
        {
            if (StartupDelayTime < 0) { StartupDelayTime = 0;}
        }

        public static bool IsQuitting = false;

        public void Initialize(string userName="", Dictionary<string,object> userProperties = null)
        {
            if (instance != null && instance != this)
            {
                Util.logDebug("CognitiveVR_Manager Initialize instance is not null and not this! Destroy");
                Destroy(gameObject);
                return;
            } //destroy if there's already another manager
            if (instance == this && Core.Initialized)
            {
                Util.logDebug("CognitiveVR_Manager Initialize instance is this! <color=yellow>Skip Initialize</color>");
                return;
            } //skip if this manage has already been initialized

            if (!CognitiveVR_Preferences.Instance.IsAPIKeyValid)
            {
                Util.logDebug("CognitiveVR_Manager Initialize does not have valid apikey");
                return;
            }
            if (OutstandingInitRequest)
            {
                Util.logDebug("CognitiveVR_Manager Initialize already called. Waiting for response");
                return;
            }
            Util.logDebug("CognitiveVR_Manager Initialize");

            OutstandingInitRequest = true;

            playerSnapshotInverval = new WaitForSeconds(CognitiveVR.CognitiveVR_Preferences.S_SnapshotInterval);
            GPSUpdateInverval = new WaitForSeconds(CognitiveVR_Preferences.Instance.GPSInterval);
            StartCoroutine(Tick());

#if CVR_STEAMVR
            SteamVR_Events.NewPoses.AddListener(OnPoseUpdate); //steamvr 1.2
            PoseUpdateEvent += PoseUpdateEvent_ControllerStateUpdate;
            //SteamVR_Utils.Event.Listen("new_poses", OnPoseUpdate); //steamvr 1.1
#endif

#if CVR_STEAMVR2
            Valve.VR.SteamVR_Events.NewPoses.AddListener(OnPoseUpdate);
            PoseUpdateEvent += PoseUpdateEvent_ControllerStateUpdate;
#endif

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            //SceneManager_SceneLoaded(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), UnityEngine.SceneManagement.LoadSceneMode.Single);
            Core.SetTrackingScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            OnLevelLoaded();



            Core.UserId = userName;

            CognitiveVR.Core.init(OnInit); //TODO return errors from init method, not callback since there isn't a delay on startup
            Core.UpdateSessionState(Util.GetDeviceProperties() as Dictionary<string,object>);

#if UNITY_2017_2_OR_NEWER
            Core.UpdateSessionState("cvr.vr.enabled", UnityEngine.XR.XRSettings.enabled);
            Core.UpdateSessionState("cvr.vr.display.model", UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRDevice.isPresent ? UnityEngine.XR.XRDevice.model : "Not Found"); //vive mvt, vive. mv, oculus rift cv1, acer ah100
            Core.UpdateSessionState("cvr.vr.display.family", UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRDevice.isPresent ? UnityEngine.XR.XRSettings.loadedDeviceName : "Not Found"); //openvr, oculus, windowsmr
#else
            Core.UpdateSessionState("cvr.vr.enabled", UnityEngine.VR.VRSettings.enabled);
            Core.UpdateSessionState("cvr.vr.display.model", UnityEngine.VR.VRSettings.enabled && UnityEngine.VR.VRDevice.isPresent ? UnityEngine.VR.VRDevice.model : "Not Found");
            Core.UpdateSessionState("cvr.vr.display.family", UnityEngine.VR.VRSettings.enabled && UnityEngine.VR.VRDevice.isPresent ? UnityEngine.VR.VRSettings.loadedDeviceName : "Not Found");
#endif


            Core.UpdateSessionState("cvr.deviceId", Core.DeviceId);
            Core.UpdateSessionState(userProperties);
            Core.UpdateSessionState("cvr.name", userName);

            CognitiveVR.NetworkManager.InitLocalStorage(System.Environment.NewLine);
        }

#if CVR_STEAMVR || CVR_STEAMVR2
        private void PoseUpdateEvent_ControllerStateUpdate(params Valve.VR.TrackedDevicePose_t[] args)
        {
            InitializeControllers();

            for (int i = 0; i<args.Length;i++)
            {
                for (int j = 0; j<controllers.Length;j++)
                {
                    if (controllers[j].id == i)
                    {
                        controllers[j].connected = args[i].bDeviceIsConnected;
                        controllers[j].visible = args[i].bPoseIsValid;
                    }
                }
            }
        }
#endif

        /// <summary>
        /// sets a user friendly label for the session on the dashboard. automatically generated if not supplied
        /// </summary>
        /// <param name="name"></param>
        public static void SetSessionName(string name)
        {
            Core.UpdateSessionState("cvr.sessionname", name);
        }

        public static void SetLobbyId(string lobbyId)
        {
            CognitiveVR_Preferences.SetLobbyId(lobbyId);
        }

        private void SceneManager_SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            var loadingScene = CognitiveVR_Preferences.FindScene(scene.name);
            bool replacingSceneId = false;

            if (CognitiveVR_Preferences.Instance.SendDataOnLevelLoad)
            {
                Core.SendDataEvent();
            }


            if (mode == UnityEngine.SceneManagement.LoadSceneMode.Additive)
            {
                //if scene loaded has new scene id
                if (loadingScene != null && !string.IsNullOrEmpty(loadingScene.SceneId))
                {
                    replacingSceneId = true;
                }
            }
            if (mode == UnityEngine.SceneManagement.LoadSceneMode.Single || replacingSceneId)
            {
                DynamicObject.ClearObjectIds();
                Core.SetTrackingScene("");
                if (loadingScene != null)
                {
                    if (!string.IsNullOrEmpty(loadingScene.SceneId))
                    {
                        Core.SetTrackingScene(scene.name);
                    }
                }
            }
            OnLevelLoaded();
        }

        public static int frameCount;

        //start after successful init callback
        IEnumerator Tick()
        {
            while (Application.isPlaying) //cognitive manager is destroyed on end session, which will stop this
            {
                yield return playerSnapshotInverval;
                frameCount = Time.frameCount;
                OnTick();
            }
        }

        public void GetGPSLocation(ref Vector3 loc, ref float bearing)
        {
            if (CognitiveVR_Preferences.Instance.SyncGPSWithGaze)
            {
                loc.x = Input.location.lastData.latitude;
                loc.y = Input.location.lastData.longitude;
                loc.z = Input.location.lastData.altitude;
                bearing = 360-Input.compass.magneticHeading;
            }
            else
            {
                loc = GPSLocation;
                bearing = CompassOrientation;
            }
        }

        Vector3 GPSLocation;
        float CompassOrientation;
        IEnumerator GPSTick()
        {
            while (Application.isPlaying)
            {
                yield return GPSUpdateInverval;
                GPSLocation.x = Input.location.lastData.latitude;
                GPSLocation.y = Input.location.lastData.longitude;
                GPSLocation.z = Input.location.lastData.altitude;
                CompassOrientation = 360 - Input.compass.magneticHeading;                
            }
        }

        void Update()
        {
            if (initResponse != Error.Success)
            {
                return;
            }

            //doPostRender = false;

            OnUpdate();
            UpdateSendHotkeyCheck();

#if CVR_STEAMVR || CVR_STEAMVR2
            var system = Valve.VR.OpenVR.System;
            if (system != null)
            {
                var vrEvent = new Valve.VR.VREvent_t();
                var size = (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(Valve.VR.VREvent_t));
                for (int i = 0; i < 64; i++)
                {
                    if (!system.PollNextEvent(ref vrEvent, size))
                        break;
                    OnPoseEvent((Valve.VR.EVREventType)vrEvent.eventType);
                }
            }
#endif

#if CVR_OCULUS
            controllers[0].connected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
            controllers[0].visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);

            controllers[1].connected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
            controllers[1].visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
#endif
        }

        void UpdateSendHotkeyCheck()
        {
            CognitiveVR_Preferences prefs = CognitiveVR_Preferences.Instance;

            if (!prefs.SendDataOnHotkey) { return; }
            if (Input.GetKeyDown(prefs.SendDataHotkey))
            {
                if (prefs.HotkeyShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) { return; }
                if (prefs.HotkeyAlt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) { return; }
                if (prefs.HotkeyCtrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) { return; }

                Core.SendDataEvent();
            }
        }

        /// <summary>
        /// End the cognitivevr session. sends any outstanding data to dashboard and sceneexplorer
        /// requires calling Initialize to create a new session id and begin recording analytics again
        /// </summary>
        public void EndSession()
        {
            double playtime = Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp;
            new CustomEvent("Session End").SetProperty("sessionlength", playtime).Send();

            Core.SendDataEvent();

            //clear properties from last session
            //newSessionProperties.Clear();
            //knownSessionProperties.Clear();

            CleanupEvents();
            Core.reset();
            initResponse = Error.NotInitialized;
            DynamicObject.ClearObjectIds();
        }

        void OnDestroy()
        {
            if (instance != this) { return; }
            if (!Application.isPlaying) { return; }

            OnQuit();
            //OnSendData();

            if (Core.Initialized)
            {
                Core.reset();
            }

            CleanupEvents();
        }

        void CleanupEvents()
        {
            //CleanupPlayerRecorderEvents();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            initResponse = Error.NotInitialized;
        }

#region Application Quit
        bool hasCanceled = false;
        void OnApplicationQuit()
        {
            IsQuitting = true;
            if (hasCanceled) { return; }

            if (InitResponse != Error.Success) { return; }

            double playtime = Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp;
            if (QuitEvent == null)
            {
				CognitiveVR.Util.logDebug("session length " + playtime);
                new CustomEvent("Session End").SetProperty("sessionlength",playtime).Send();
                return;
            }

			CognitiveVR.Util.logDebug("session length " + playtime);
            new CustomEvent("Session End").SetProperty("sessionlength", playtime).Send();
            Application.CancelQuit();


            Core.SendDataEvent();
            Core.reset();


            //Camera.onPostRender -= MyPostRender;
            //OnQuit();
            StartCoroutine(SlowQuit());
        }

        IEnumerator SlowQuit()
        {
            yield return new WaitForSeconds(0.5f);
            hasCanceled = true;            
            Application.Quit();
        }

        #endregion
    }
}