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

namespace CognitiveVR
{
    [HelpURL("https://docs.cognitive3d.com/unity/get-started/")]
    [AddComponentMenu("Cognitive3D/Common/Cognitive VR Manager",1)]
    public class CognitiveVR_Manager : MonoBehaviour
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

        private static CognitiveVR_Manager instance;
        public static CognitiveVR_Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<CognitiveVR_Manager>();
                    if (instance == null)
                    {
                        Util.logWarning("Cognitive Manager Instance not present in scene. Creating new gameobject");
                        instance = new GameObject("CognitiveVR_Manager").AddComponent<CognitiveVR_Manager>();
                    }
                }
                return instance;
            }
        }
        YieldInstruction playerSnapshotInverval;
        YieldInstruction GPSUpdateInverval;

        //cached Time.frameCount to quickly get Util.Timestamp
        public static int FrameCount { get; private set; }

        public static bool IsQuitting = false;

        static Error initResponse = Error.NotInitialized;
        public static Error InitResponse { get { return initResponse; } }

        [Tooltip("Enable automatic initialization. If false, you must manually call Initialize()")]
        public bool InitializeOnStart = true;

        [Tooltip("Delay before starting a session. This delay can ensure other SDKs have properly initialized")]
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

        public void Initialize(string userName="", Dictionary<string,object> userProperties = null)
        {
            if (instance != null && instance != this)
            {
                Util.logDebug("CognitiveVR_Manager Initialize instance is not null and not this! Destroy");
                Destroy(gameObject);
                return;
            } //destroy if there's already another manager
            if (Core.IsInitialized)
            {
                Util.logWarning("CognitiveVR_Manager Initialize - Already Initialized!");
                return;
            } //skip if a session has already been initialized

            if (!CognitiveVR_Preferences.Instance.IsApplicationKeyValid)
            {
                Util.logDebug("CognitiveVR_Manager Initialize does not have valid apikey");
                return;
            }

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

            //get all loaded scenes. if one has a sceneid, use that
            var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for(int i = 0; i<count;i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                var cogscene = CognitiveVR_Preferences.FindSceneByPath(scene.path);
                if (cogscene != null)
                {
                    Core.SetTrackingScene(cogscene);
                    break;
                }
            }

            Core.UserId = userName;
            Core.SetSessionProperty("c3d.username", userName);

            //sets session properties for system hardware
            Error initError = CognitiveVR.Core.Init(GameplayReferences.HMD);

            OnLevelLoaded();

            //on init stuff here
            initResponse = initError;

            if (initError == Error.None)
            {
                new CustomEvent("c3d.sessionStart").Send();
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
                playerSnapshotInverval = new WaitForSeconds(CognitiveVR.CognitiveVR_Preferences.S_SnapshotInterval);
                GPSUpdateInverval = new WaitForSeconds(CognitiveVR_Preferences.Instance.GPSInterval);
                StartCoroutine(Tick());
                Util.logDebug("CognitiveVR Initialized");
            }
            else //some failure
            {
                StopAllCoroutines();
                Util.logDebug("CognitiveVR Error" + initError.ToString());
            }

            var components = GetComponentsInChildren<CognitiveVR.Components.CognitiveVRAnalyticsComponent>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].CognitiveVR_Init(initError);
            }

            switch (CognitiveVR_Preferences.Instance.GazeType)
            {
                case GazeType.Physics: gameObject.AddComponent<PhysicsGaze>().Initialize(); break;
                case GazeType.Command: gameObject.AddComponent<CommandGaze>().Initialize(); break;
                    //case GazeType.Sphere: gameObject.AddComponent<SphereGaze>().Initialize(); break;
            }
#if CVR_TOBIIVR || CVR_AH || CVR_FOVE || CVR_PUPIL
            //fixation requires some kind of eye tracking hardware
            FixationRecorder fixationRecorder = gameObject.GetComponent<FixationRecorder>();
            if (fixationRecorder == null)
                fixationRecorder = gameObject.AddComponent<FixationRecorder>();
            fixationRecorder.Initialize();
#endif

            if (InitEvent != null) { InitEvent(initError); }

            CognitiveVR.NetworkManager.InitLocalStorage(System.Environment.NewLine);

            SetSessionProperties();
        }

        /// <summary>
        /// sets automatic session properties from scripting define symbols, device ids, etc
        /// </summary>
        private void SetSessionProperties()
        {
#if CVR_META
            string serialnumber;
            string xml;
            if (GetSerialNumberAndCalibration(out serialnumber, out xml))
            {
                Core.SetSessionProperty("c3d.device.serialnumber",serialnumber);
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
                Core.SetSessionProperty("c3d.device.serialnumber", serialnumber);
            }
#endif

#if UNITY_EDITOR
            Core.SetSessionProperty("c3d.app.inEditor", true);
#else
            Core.SetSessionProperty("c3d.app.inEditor", false);
#endif
            Core.SetSessionProperty("c3d.version", Core.SDK_VERSION);
            //TODO support multiple hmds (tobii + pupil + vive)
            //addon sdks - tobii, ah, pupil

#if CVR_STEAMVR2 || CVR_STEAMVR
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Vive");
#elif CVR_FOVE
            Core.SetSessionProperty("c3d.device.hmd.type", "Fove");
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Fove");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Fove");
            Core.SetSessionProperty("c3d.app.sdktype", "Fove");
#elif CVR_SNAPDRAGON
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Qualcomm");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Snapdragon");
            Core.SetSessionProperty("c3d.app.sdktype", "Snapdragon");
#elif CVR_OCULUS
            Core.SetSessionProperty("c3d.device.hmd.type", OVRPlugin.GetSystemHeadsetType().ToString().Replace('_', ' '));
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Oculus");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type", "None");
            Core.SetSessionProperty("c3d.app.sdktype", "Oculus");
#elif CVR_NEURABLE
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Neurable");
            Core.SetSessionProperty("c3d.app.sdktype", "Neurable");
#elif CVR_ARKIT
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Apple");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "ARKit");
#elif CVR_ARCORE
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Android");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "ARCore");
#elif CVR_GOOGLEVR
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Android");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Google VR");
#elif CVR_HOLOLENS
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Microsoft");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Hololens");
#elif CVR_META
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Meta");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Meta");
#endif

            //eye tracker addons
#if CVR_TOBIIVR
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer","HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            Core.SetSessionProperty("c3d.app.sdktype", "Tobii");
#elif CVR_PUPIL
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Pupil");
            Core.SetSessionProperty("c3d.app.sdktype", "Pupil");
#elif CVR_AH
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer", "Google");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Adhawk");
            Core.SetSessionProperty("c3d.app.sdktype", "Adhawk");
#endif
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer", "Unknown");
            Core.SetSessionPropertyIfEmpty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionPropertyIfEmpty("c3d.device.eyetracking.type", "None");
            Core.SetSessionPropertyIfEmpty("c3d.app.sdktype", "Default");

            Core.SetSessionProperty("c3d.app.engine", "Unity");
        }


        /// <summary>
        /// sets a user friendly label for the session on the dashboard. automatically generated if not supplied
        /// </summary>
        /// <param name="name"></param>
        public static void SetSessionName(string name)
        {
            Core.SetSessionProperty("c3d.sessionname", name);
        }

        /// <summary>
        /// sets a constant lobby id shared between multiple sessions. this is for associating sessions together for multiplayer
        /// </summary>
        public static void SetLobbyId(string lobbyId)
        {
            Core.SetLobbyId(lobbyId);
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

        #region Updates and Loops

#if CVR_STEAMVR || CVR_STEAMVR2
        private void PoseUpdateEvent_ControllerStateUpdate(params Valve.VR.TrackedDevicePose_t[] args)
        {
            for (int i = 0; i<args.Length;i++)
            {
                for (int j = 0; j<2;j++)
                {
                    if (GameplayReferences.GetControllerInfo(j).id == i)
                    {
                        GameplayReferences.GetControllerInfo(j).connected = args[i].bDeviceIsConnected;
                        GameplayReferences.GetControllerInfo(j).visible = args[i].bPoseIsValid;
                    }
                }
            }
        }
#endif

        //start after successful init callback
        IEnumerator Tick()
        {
            while (Application.isPlaying) //cognitive manager is destroyed on end session, which will stop this
            {
                yield return playerSnapshotInverval;
                FrameCount = Time.frameCount;
                OnTick();
            }
        }

        void Update()
        {
            if (initResponse != Error.None)
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
            GameplayReferences.GetControllerInfo(false).connected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
            GameplayReferences.GetControllerInfo(false).visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);

            GameplayReferences.GetControllerInfo(true).connected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
            GameplayReferences.GetControllerInfo(true).visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
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

        #endregion

        #region GPS
        public void GetGPSLocation(ref Vector3 loc, ref float bearing)
        {
            if (CognitiveVR_Preferences.Instance.SyncGPSWithGaze)
            {
                loc.x = Input.location.lastData.latitude;
                loc.y = Input.location.lastData.longitude;
                loc.z = Input.location.lastData.altitude;
                bearing = 360 - Input.compass.magneticHeading;
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
#endregion

#region Application Quit, Session End and OnDestroy
        /// <summary>
        /// End the cognitivevr session. sends any outstanding data to dashboard and sceneexplorer
        /// requires calling Initialize to create a new session id and begin recording analytics again
        /// </summary>
        public void EndSession()
        {
            double playtime = Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp;
            new CustomEvent("c3d.sessionEnd").SetProperty("sessionlength", playtime).Send();

            Core.SendDataEvent();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            initResponse = Error.NotInitialized;
            Core.Reset();
            initResponse = Error.NotInitialized;
            DynamicObject.ClearObjectIds();
        }

        void OnDestroy()
        {
            if (instance != this) { return; }
            if (!Application.isPlaying) { return; }

            OnQuit();
            QuitEvent = null;

            if (Core.IsInitialized)
            {
                Core.Reset();
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            initResponse = Error.NotInitialized;
        }

        bool hasCanceled = false;
        void OnApplicationQuit()
        {
            IsQuitting = true;
            if (hasCanceled) { return; }

            if (InitResponse != Error.None) { return; }

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

            OnQuit();
            QuitEvent = null;

            Core.SendDataEvent();
            Core.Reset();
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