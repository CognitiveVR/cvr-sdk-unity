using UnityEngine;
using Cognitive3D;
using System.Collections;
using System.Collections.Generic;
using System;
#if C3D_STEAMVR || C3D_STEAMVR2
using Valve.VR;
#endif

#if C3D_META
using System.Runtime.InteropServices;
#endif

/// <summary>
/// initializes Cognitive3D analytics. Add components to track additional events
/// </summary>

//init components
//update ticks + events
//level change events
//quit and destroy events

namespace Cognitive3D
{
    [HelpURL("https://docs.cognitive3d.com/unity/get-started/")]
    [AddComponentMenu("Cognitive3D/Common/Cognitive VR Manager",1)]
    [DefaultExecutionOrder(-1)]
    public class Cognitive3D_Manager : MonoBehaviour
    {

#if C3D_META
        [DllImport("MetaVisionDLL", EntryPoint = "getSerialNumberAndCalibration")]
        internal static extern bool GetSerialNumberAndCalibration([MarshalAs(UnmanagedType.BStr), Out] out string serial, [MarshalAs(UnmanagedType.BStr), Out] out string xml);
#endif

        #region Events


#if C3D_STEAMVR
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
#if C3D_STEAMVR2
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

        private static Cognitive3D_Manager instance;
        public static Cognitive3D_Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<Cognitive3D_Manager>();
                    if (instance == null)
                    {
                        Util.logWarning("Cognitive Manager Instance not present in scene. Creating new gameobject");
                        instance = new GameObject("Cognitive3D_Manager").AddComponent<Cognitive3D_Manager>();
                    }
                }
                return instance;
            }
        }
        YieldInstruction playerSnapshotInverval;
        YieldInstruction automaticSendInterval;
        YieldInstruction GPSUpdateInverval;

        public static bool IsQuitting = false;

        [Tooltip("Delay before starting a session. This delay can ensure other SDKs have properly initialized")]
        public float StartupDelayTime = 2;

        [Tooltip("Start recording analytics when this gameobject becomes active (and after the StartupDelayTime has elapsed)")]
        public bool InitializeOnStart = true;

#if C3D_OCULUS
        [Tooltip("Used to automatically associate a profile to a participant. Allows tracking between different sessions")]
        public bool AssignOculusProfileToParticipant = true;
#endif

        /// <summary>
        /// sets instance of Cognitive3D_Manager
        /// </summary>
        private void OnEnable()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            if (instance == this) { return; }
            instance = this;
        }

        [NonSerialized]
        public long StartupTimestampMilliseconds;

        IEnumerator Start()
        {
            StartupTimestampMilliseconds = (long)(Util.Timestamp() * 1000);
            GameObject.DontDestroyOnLoad(gameObject);
            if (StartupDelayTime > 0)
            {
                yield return new WaitForSeconds(StartupDelayTime);
            }
            if (InitializeOnStart)
                Initialize("");

#if C3D_OCULUS
            if (AssignOculusProfileToParticipant && (Core.ParticipantName == string.Empty && Core.ParticipantId == string.Empty))
            {
                if (!Oculus.Platform.Core.IsInitialized())
                    Oculus.Platform.Core.Initialize();

                Oculus.Platform.Users.GetLoggedInUser().OnComplete(delegate (Oculus.Platform.Message<Oculus.Platform.Models.User> message)
                {
                    if (message.IsError)
                    {
                        Debug.LogError(message.GetError().Message);
                    }
                    else
                    {
                        Core.SetParticipantId(message.Data.OculusID.ToString());
                    }
                });
            }
#endif
        }

        [System.NonSerialized]
        public GazeBase gazeBase;
        [System.NonSerialized]
        public FixationRecorder fixationRecorder;

        /// <summary>
        /// Start recording a session. Sets SceneId, records basic hardware information, starts coroutines to record other data points on intervals
        /// </summary>
        /// <param name="participantName">friendly name for identifying participant</param>
        /// <param name="participantId">unique id for identifying participant</param>
        public void Initialize(string participantName="", string participantId = "", List<KeyValuePair<string,object>> participantProperties = null)
        {
            if (instance != null && instance != this)
            {
                Util.logDebug("Cognitive3D_Manager Initialize instance is not null and not this! Destroy");
                Destroy(gameObject);
                return;
            } //destroy if there's already another manager
            if (IsInitialized)
            {
                Util.logWarning("Cognitive3D_Manager Initialize - Already Initialized!");
                return;
            } //skip if a session has already been initialized

            if (!Cognitive3D_Preferences.Instance.IsApplicationKeyValid)
            {
                Util.logDebug("Cognitive3D_Manager Initialize does not have valid apikey");
                return;
            }

#if C3D_STEAMVR
            SteamVR_Events.NewPoses.AddListener(OnPoseUpdate); //steamvr 1.2
            PoseUpdateEvent += PoseUpdateEvent_ControllerStateUpdate;
            //SteamVR_Utils.Event.Listen("new_poses", OnPoseUpdate); //steamvr 1.1
#endif

#if C3D_STEAMVR2
            Valve.VR.SteamVR_Events.NewPoses.AddListener(OnPoseUpdate);
            PoseUpdateEvent += PoseUpdateEvent_ControllerStateUpdate;
#endif

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;

            if (!string.IsNullOrEmpty(participantName))
                SetParticipantFullName(participantName);
            if (!string.IsNullOrEmpty(participantId))
                SetParticipantId(participantId);

            //sets session properties for system hardware
            //also constructs network and local cache files/readers

            CognitiveStatics.Initialize();

            DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;

            ExitpollHandler = new ExitPollLocalDataHandler(Application.persistentDataPath + "/c3dlocal/exitpoll/");

            if (Cognitive3D_Preferences.Instance.LocalStorage)
                DataCache = new DualFileCache(Application.persistentDataPath + "/c3dlocal/");
            GameObject networkGo = new GameObject("Cognitive Network");
            networkGo.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            NetworkManager = networkGo.AddComponent<NetworkManager>();
            NetworkManager.Initialize(DataCache, ExitpollHandler);

            DynamicManager.Initialize();
            //DynamicObjectCore.Initialize();
            CustomEvent.Initialize();
            SensorRecorder.Initialize();

            _timestamp = Util.Timestamp();
            //set session timestamp
            if (string.IsNullOrEmpty(_sessionId))
            {
                _sessionId = (int)SessionTimeStamp + "_" + DeviceId;
            }
            CoreInterface.Initialize(SessionID, SessionTimeStamp, DeviceId);
            IsInitialized = true;
            //if (Cognitive3D_Preferences.Instance.EnableGaze == false)
            //GazeCore.SendSessionProperties(false);
            //TODO support skipping spatial gaze data but still recording session properties

            //get all loaded scenes. if one has a sceneid, use that
            var count = UnityEngine.SceneManagement.SceneManager.sceneCount;
            UnityEngine.SceneManagement.Scene scene = new UnityEngine.SceneManagement.Scene();
            for(int i = 0; i<count;i++)
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                var cogscene = Cognitive3D_Preferences.FindSceneByPath(scene.path);
                if (cogscene != null && !string.IsNullOrEmpty(cogscene.SceneId))
                {
                    SetTrackingScene(cogscene, false);
                    break;
                }
            }
            if (TrackingScene == null)
            {
                Util.logWarning("CogntitiveVRManager No Tracking Scene Set!");
            }

            InvokeLevelLoadedEvent(scene, UnityEngine.SceneManagement.LoadSceneMode.Single, true);

            new CustomEvent("c3d.sessionStart").Send();
            if (Cognitive3D_Preferences.Instance.TrackGPSLocation)
            {
                Input.location.Start(Cognitive3D_Preferences.Instance.GPSAccuracy, Cognitive3D_Preferences.Instance.GPSAccuracy);
                Input.compass.enabled = true;
                if (Cognitive3D_Preferences.Instance.SyncGPSWithGaze)
                {
                    //just get gaze snapshot to grab this
                }
                else
                {
                    StartCoroutine(GPSTick());
                }
            }
            playerSnapshotInverval = new WaitForSeconds(Cognitive3D.Cognitive3D_Preferences.S_SnapshotInterval);
            GPSUpdateInverval = new WaitForSeconds(Cognitive3D_Preferences.Instance.GPSInterval);
            automaticSendInterval = new WaitForSeconds(Cognitive3D_Preferences.Instance.AutomaticSendTimer);
            StartCoroutine(Tick());
            Util.logDebug("Cognitive3D Initialized");

            var components = GetComponentsInChildren<Cognitive3D.Components.AnalyticsComponentBase>();
            for (int i = 0; i < components.Length; i++)
            {
                components[i].Cognitive3D_Init();
            }

            //TODO support for 360 skysphere media recording
            gazeBase = gameObject.GetComponent<PhysicsGaze>();
            if (gazeBase == null)
            {
                gazeBase = gameObject.AddComponent<PhysicsGaze>();
            }
            gazeBase.Initialize();

            if (GameplayReferences.SDKSupportsEyeTracking)
            {
                fixationRecorder = gameObject.GetComponent<FixationRecorder>();
                if (fixationRecorder == null)
                {
                    fixationRecorder = gameObject.AddComponent<FixationRecorder>();
                }
                fixationRecorder.Initialize();
            }

            //if (InitEvent != null) { InitEvent(initError); }
            InvokeSessionBeginEvent();

            SetSessionProperties();

            if (participantProperties != null)
                SetSessionProperties(participantProperties);

            OnPreSessionEnd += Core_EndSessionEvent;
            InvokeSendDataEvent(false);
#if C3D_OMNICEPT
            var gliaBehaviour = GameplayReferences.GliaBehaviour;

            if (gliaBehaviour != null)
            {
                gliaBehaviour.OnEyeTracking.AddListener(RecordEyePupillometry);
                gliaBehaviour.OnHeartRate.AddListener(RecordHeartRate);
                gliaBehaviour.OnCognitiveLoad.AddListener(RecordCognitiveLoad);
                gliaBehaviour.OnHeartRateVariability.AddListener(RecordHeartRateVariability);
            }
#endif
        }

#if C3D_OMNICEPT
        double pupillometryTimestamp;

        //update every 100MS
        void RecordEyePupillometry(HP.Omnicept.Messaging.Messages.EyeTracking data)
        {
            double timestampMS = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            if (pupillometryTimestamp < timestampMS)
            {
                pupillometryTimestamp = timestampMS + 0.1;
                if (data.LeftEye.PupilDilationConfidence > 0.5f && data.LeftEye.PupilDilation > 1.5f)
                {
                    SensorRecorder.RecordDataPoint("HP.Left Pupil Diameter", data.LeftEye.PupilDilation, timestampMS);
                }
                if (data.RightEye.PupilDilationConfidence > 0.5f && data.RightEye.PupilDilation > 1.5f)
                {
                    SensorRecorder.RecordDataPoint("HP.Right Pupil Diameter", data.RightEye.PupilDilation, timestampMS);
                }
            }
        }

        //every 5000 ms
        void RecordHeartRate(HP.Omnicept.Messaging.Messages.HeartRate data)
        {
            double timestampMS = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            SensorRecorder.RecordDataPoint("HP.HeartRate", data.Rate, timestampMS);
        }

        //every 60 000ms
        void RecordHeartRateVariability(HP.Omnicept.Messaging.Messages.HeartRateVariability data)
        {
            double timestampMS = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            SensorRecorder.RecordDataPoint("HP.HeartRate.Variability", data.Sdnn, timestampMS);
        }

        //every 1000MS
        void RecordCognitiveLoad(HP.Omnicept.Messaging.Messages.CognitiveLoad data)
        {
            double timestampMS = (double)data.Timestamp.OmniceptTimeMicroSeconds / 1000000.0;
            SensorRecorder.RecordDataPoint("HP.CognitiveLoad", data.CognitiveLoadValue, timestampMS);
            SensorRecorder.RecordDataPoint("HP.CognitiveLoad.Confidence", data.StandardDeviation, timestampMS);
        }
#endif

        /// <summary>
        /// sets automatic session properties from scripting define symbols, device ids, etc
        /// </summary>
        private void SetSessionProperties()
        {
            SetSessionProperty("c3d.app.name", Application.productName);
            SetSessionProperty("c3d.app.version", Application.version);
            SetSessionProperty("c3d.app.engine.version", Application.unityVersion);
            SetSessionProperty("c3d.device.type", SystemInfo.deviceType.ToString());
            SetSessionProperty("c3d.device.cpu", SystemInfo.processorType);
            SetSessionProperty("c3d.device.model", SystemInfo.deviceModel);
            SetSessionProperty("c3d.device.gpu", SystemInfo.graphicsDeviceName);
            SetSessionProperty("c3d.device.os", SystemInfo.operatingSystem);
            SetSessionProperty("c3d.device.memory", Mathf.RoundToInt((float)SystemInfo.systemMemorySize / 1024));

            SetSessionProperty("c3d.deviceid", DeviceId);

#if C3D_META
            string serialnumber;
            string xml;
            if (GetSerialNumberAndCalibration(out serialnumber, out xml))
            {
                Core.SetSessionProperty("c3d.device.serialnumber",serialnumber);
            }
#elif C3D_STEAMVR

            string serialnumber = null;

            var error = ETrackedPropertyError.TrackedProp_Success;
            var result = new System.Text.StringBuilder();

            if (OpenVR.System != null)
            {
                var capacity = OpenVR.System.GetStringTrackedDeviceProperty(0, ETrackedDeviceProperty.Prop_SerialNumber_String, result, 64, ref error);
                if (capacity > 0)
                    serialnumber = result.ToString();

                if (!string.IsNullOrEmpty(serialnumber))
                {
                    Core.SetSessionProperty("c3d.device.serialnumber", serialnumber);
                }
            }
#endif

#if UNITY_EDITOR
            SetSessionProperty("c3d.app.inEditor", true);
#else
            Core.SetSessionProperty("c3d.app.inEditor", false);
#endif
            SetSessionProperty("c3d.version", SDK_VERSION);
            SetSessionProperty("c3d.device.hmd.type", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);

#if C3D_STEAMVR2 || C3D_STEAMVR
            //other SDKs may use steamvr as a base or for controllers (ex, hp omnicept). this may be replaced below
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type","None");
            SetSessionProperty("c3d.app.sdktype", "Vive");
#endif
#if C3D_SNAPDRAGON
            SetSessionProperty("c3d.device.eyetracking.enabled", true);
            SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            SetSessionProperty("c3d.app.sdktype", "Snapdragon");
#elif C3D_OCULUS
            SetSessionProperty("c3d.device.hmd.type", OVRPlugin.GetSystemHeadsetType().ToString().Replace('_', ' '));
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type", "None");
            SetSessionProperty("c3d.app.sdktype", "Oculus");
#elif C3D_ARKIT
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type","None");
            SetSessionProperty("c3d.app.sdktype", "ARKit");
#elif C3D_ARCORE
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type","None");
            SetSessionProperty("c3d.app.sdktype", "ARCore");
#elif C3D_GOOGLEVR
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type","None");
            SetSessionProperty("c3d.app.sdktype", "Google VR");
#elif C3D_HOLOLENS
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type","None");
            SetSessionProperty("c3d.app.sdktype", "Hololens");
#elif C3D_META
            SetSessionProperty("c3d.device.eyetracking.enabled", false);
            SetSessionProperty("c3d.device.eyetracking.type","None");
            SetSessionProperty("c3d.app.sdktype", "Meta");
#elif C3D_VARJO
            SetSessionProperty("c3d.device.eyetracking.enabled", true);
            SetSessionProperty("c3d.device.eyetracking.type","Varjo");
            SetSessionProperty("c3d.app.sdktype", "Varjo");
#elif C3D_OMNICEPT
            SetSessionProperty("c3d.device.eyetracking.enabled", true);
            SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            SetSessionProperty("c3d.app.sdktype", "HP Omnicept");
#elif C3D_PICOVR
            SetSessionProperty("c3d.device.eyetracking.enabled", true);
            SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            SetSessionProperty("c3d.app.sdktype", "PicoVR");
            SetSessionProperty("c3d.device.model", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);
#elif C3D_PICOXR
            SetSessionProperty("c3d.device.eyetracking.enabled", true);
            SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            SetSessionProperty("c3d.app.sdktype", "PicoXR");
            SetSessionProperty("c3d.device.model", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);
#endif
            //TODO add XR inputdevice name

            //eye tracker addons
#if C3D_SRANIPAL
            SetSessionProperty("c3d.device.eyetracking.enabled", true);
            SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            SetSessionProperty("c3d.app.sdktype", "Vive Pro Eye");
#elif C3D_WINDOWSMR
            SetSessionProperty("c3d.app.sdktype", "Windows Mixed Reality");
#elif C3D_OPENXR
            //Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            //Core.SetSessionProperty("c3d.device.eyetracking.type","OpenXR");
            SetSessionProperty("c3d.app.sdktype", "OpenXR");
#endif
            SetSessionPropertyIfEmpty("c3d.device.eyetracking.enabled", false);
            SetSessionPropertyIfEmpty("c3d.device.eyetracking.type", "None");
            SetSessionPropertyIfEmpty("c3d.app.sdktype", "Default");

            SetSessionProperty("c3d.app.engine", "Unity");
        }

        /// <summary>
        /// sets the user's name property
        /// </summary>
        /// <param name="name"></param>
        [Obsolete("Use Core.SetParticipantFullName and Core.SetParticipantId instead")]
        public static void SetUserName(string name)
        {
            if (!string.IsNullOrEmpty(name))
                SetParticipantFullName(name);
        }

        /// <summary>
        /// registered to unity's OnSceneLoaded callback. sends outstanding data, then sets correct tracking scene id and refreshes dynamic object session manifest
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void SceneManager_SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            var loadingScene = Cognitive3D_Preferences.FindScene(scene.name);
            bool replacingSceneId = false;

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
                //DynamicObject.ClearObjectIds();
                replacingSceneId = true;
            }
            
            if (replacingSceneId && Cognitive3D_Preferences.Instance.SendDataOnLevelLoad)
            {
                InvokeSendDataEvent(false);
            }

            if (replacingSceneId)
            {
                if (loadingScene != null)
                {
                    if (!string.IsNullOrEmpty(loadingScene.SceneId))
                    {
                        SetTrackingScene(scene.name,true);
                    }
                    else
                    {
                        SetTrackingScene("", true);
                    }
                }
                else
                {
                    SetTrackingScene("", true);
                }
            }

            InvokeLevelLoadedEvent(scene, mode, replacingSceneId);
        }

        private void SceneManager_SceneUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            //TODO for unload scene async, may need to change tracking scene id
            //a situation where a scene without an ID is loaded additively, then a scene with an id is unloaded, the sceneid will persist
        }

        #region Updates and Loops

#if C3D_STEAMVR || C3D_STEAMVR2 || C3D_OCULUS
        GameplayReferences.ControllerInfo tempControllerInfo = null;
#endif

#if C3D_STEAMVR || C3D_STEAMVR2
        private void PoseUpdateEvent_ControllerStateUpdate(params Valve.VR.TrackedDevicePose_t[] args)
        {
            for (int i = 0; i<args.Length;i++)
            {
                for (int j = 0; j<2;j++)
                {
                    if (GameplayReferences.GetControllerInfo(j,out tempControllerInfo))
                    {
                        if (tempControllerInfo.id == i)
                        {
                            tempControllerInfo.connected = args[i].bDeviceIsConnected;
                            tempControllerInfo.visible = args[i].bPoseIsValid;
                        }

                    }
                }
            }
        }
#endif

        /// <summary>
        /// start after successful session initialization
        /// </summary>
        IEnumerator Tick()
        {
            while (IsInitialized)
            {
                yield return playerSnapshotInverval;
                InvokeTickEvent();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IEnumerator AutomaticSendData()
        {
            while (IsInitialized)
            {
                yield return automaticSendInterval;
                CoreInterface.Flush(false);
            }
        }

        void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            InvokeUpdateEvent(Time.deltaTime);
            UpdateSendHotkeyCheck();

            //this should only update if components that use these values are found (controller visibility, arm length?)

#if C3D_STEAMVR || C3D_STEAMVR2
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

#if C3D_OCULUS
            if (GameplayReferences.GetControllerInfo(false, out tempControllerInfo))
            {
                tempControllerInfo.connected = OVRInput.IsControllerConnected(OVRInput.Controller.LTouch);
                tempControllerInfo.visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.LTouch);
            }

            if (GameplayReferences.GetControllerInfo(true, out tempControllerInfo))
            {
                tempControllerInfo.connected = OVRInput.IsControllerConnected(OVRInput.Controller.RTouch);
                tempControllerInfo.visible = OVRInput.GetControllerPositionTracked(OVRInput.Controller.RTouch);
            }
#endif
        }

        void UpdateSendHotkeyCheck()
        {
            Cognitive3D_Preferences prefs = Cognitive3D_Preferences.Instance;

            if (!prefs.SendDataOnHotkey) { return; }
            if (Input.GetKeyDown(prefs.SendDataHotkey))
            {
                if (prefs.HotkeyShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) { return; }
                if (prefs.HotkeyAlt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) { return; }
                if (prefs.HotkeyCtrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) { return; }

                InvokeSendDataEvent(false);
            }
        }

#endregion

#region GPS
        public void GetGPSLocation(ref Vector3 loc, ref float bearing)
        {
            if (Cognitive3D_Preferences.Instance.SyncGPSWithGaze)
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
            while (IsInitialized)
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
        /// End the Cognitive3D session. sends any outstanding data to dashboard and sceneexplorer
        /// requires calling Initialize to create a new session id and begin recording analytics again
        /// </summary>
        public void EndSession()
        {
            if (IsInitialized)
            {
                double playtime = Util.Timestamp(Time.frameCount) - SessionTimeStamp;
                new CustomEvent("c3d.sessionEnd").SetProperty("sessionlength", playtime).Send();
                Cognitive3D.Util.logDebug("Session End. Duration: " + string.Format("{0:0.00}", playtime));

                InvokeSendDataEvent(false);
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
                Reset();
            }
        }

        private void Core_EndSessionEvent()
        {
            OnPreSessionEnd -= Core_EndSessionEvent;
#if C3D_OMNICEPT
            var gliaBehaviour = GameplayReferences.GliaBehaviour;

            if (gliaBehaviour != null)
            {
                //gliaBehaviour.OnEyePupillometry.RemoveListener(RecordEyePupillometry);
                gliaBehaviour.OnHeartRate.RemoveListener(RecordHeartRate);
                gliaBehaviour.OnCognitiveLoad.RemoveListener(RecordCognitiveLoad);
            }
#endif
        }

        void OnDestroy()
        {
            if (instance != this) { return; }
            if (!Application.isPlaying) { return; }

            InvokeQuitEvent();

            if (IsInitialized)
            {
                Reset();
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneUnloaded -= SceneManager_SceneUnloaded;
        }

        void OnApplicationPause(bool paused)
        {
            if (!IsInitialized) { return; }
            if (Cognitive3D_Preferences.Instance.SendDataOnPause)
            {
                new CustomEvent("c3d.pause").SetProperty("is paused", paused).Send();
                InvokeSendDataEvent(true);
            }
        }

        bool hasCanceled = false;
        void OnApplicationQuit()
        {
            IsQuitting = true;
            if (hasCanceled) { return; }

            double playtime = Util.Timestamp(Time.frameCount) - SessionTimeStamp;
            Cognitive3D.Util.logDebug("Session End. Duration: " + string.Format("{0:0.00}", playtime));            

            if (IsQuitEventBound())
            {
                new CustomEvent("Session End").SetProperty("sessionlength",playtime).Send();
                return;
            }
            new CustomEvent("Session End").SetProperty("sessionlength", playtime).Send();
            Application.CancelQuit();
            //TODO update and test with Application.wantsToQuit and Application.qutting

            InvokeQuitEvent();
            QuitEventClear();
            

            InvokeSendDataEvent(true);
            Reset();
            StartCoroutine(SlowQuit());
        }

        IEnumerator SlowQuit()
        {
            yield return new WaitForSeconds(0.5f);
            hasCanceled = true;            
            Application.Quit();
        }

        #endregion


        public const string SDK_VERSION = "0.26.19";

        public delegate void onSendData(bool copyDataToCache); //send data
        /// <summary>
        /// invoked when Cognitive3D_Manager.SendData is called or when the session ends
        /// </summary>
        public static event onSendData OnSendData;

        /// <summary>
        /// call this to send all outstanding data to the dashboard
        /// </summary>
        public static void InvokeSendDataEvent(bool copyDataToCache) { if (OnSendData != null) { OnSendData(copyDataToCache); } }

        public delegate void onSessionBegin();
        /// <summary>
        /// Cognitive3D Core.Init callback
        /// </summary>
        public static event onSessionBegin OnSessionBegin;
        public static void InvokeSessionBeginEvent() { if (OnSessionBegin != null) { OnSessionBegin.Invoke(); } }

        public delegate void onSessionEnd();
        /// <summary>
        /// Cognitive3D Core.Init callback
        /// </summary>
        public static event onSessionEnd OnPreSessionEnd;
        public static void InvokeEndSessionEvent() { if (OnPreSessionEnd != null) { OnPreSessionEnd.Invoke(); } }

        public static event onSessionEnd OnPostSessionEnd;
        public static void InvokePostEndSessionEvent() { if (OnPostSessionEnd != null) { OnPostSessionEnd.Invoke(); } }

        public delegate void onUpdate(float deltaTime);
        /// <summary>
        /// Update. Called through Manager's update function
        /// </summary>
        public static event onUpdate OnUpdate;
        public static void InvokeUpdateEvent(float deltaTime) { if (OnUpdate != null) { OnUpdate(deltaTime); } }

        public delegate void onTick();
        /// <summary>
        /// repeatedly called if the sceneid is valid. interval is Cognitive3D_Preferences.Instance.PlayerSnapshotInterval
        /// </summary>
        public static event onTick OnTick;
        public static void InvokeTickEvent() { if (OnTick != null) { OnTick(); } }

        public delegate void onQuit();
        /// <summary>
        /// called from Unity's built in OnApplicationQuit. Cancelling quit gets weird - do all application quit stuff in Manager
        /// </summary>
        public static event onQuit OnQuit;
        public static void InvokeQuitEvent() { if (OnQuit != null) { OnQuit(); } }
        public static bool IsQuitEventBound() { return OnQuit != null; }
        public static void QuitEventClear() { OnQuit = null; }

        public delegate void onLevelLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId);
        /// <summary>
        /// from Unity's SceneManager.SceneLoaded event. happens after manager sends outstanding data and updates new SceneId
        /// </summary>
        public static event onLevelLoaded OnLevelLoaded;
        public static void InvokeLevelLoadedEvent(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId) { if (OnLevelLoaded != null) { OnLevelLoaded(scene, mode, newSceneId); } }

        //public delegate void onDataSend();

        internal static ILocalExitpoll ExitpollHandler;
        internal static ICache DataCache;
        internal static NetworkManager NetworkManager;

        private static bool HasCustomSessionName;
        public static string ParticipantId { get; private set; }
        public static string ParticipantName { get; private set; }
        private static string _deviceId;
        public static string DeviceId
        {
            get
            {
                if (string.IsNullOrEmpty(_deviceId))
                {
                    _deviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
                }
                return _deviceId;
            }
            private set { _deviceId = value; }
        }

        private static double _timestamp;
        public static double SessionTimeStamp
        {
            get
            {
                return _timestamp;
            }
        }

        private static string _sessionId;
        public static string SessionID
        {
            get
            {
                return _sessionId;
            }
        }

        public static void SetSessionId(string sessionId)
        {
            if (!IsInitialized)
                _sessionId = sessionId;
            else
                Util.logWarning("Core::SetSessionId cannot be called during a session!");
        }

        public static string TrackingSceneId
        {
            get
            {
                if (TrackingScene == null) { return ""; }
                return TrackingScene.SceneId;
            }
        }
        public static int TrackingSceneVersionNumber
        {
            get
            {
                if (TrackingScene == null) { return 0; }
                return TrackingScene.VersionNumber;
            }
        }
        public static int TrackingSceneVersionId
        {
            get
            {
                if (TrackingScene == null) { return 0; }
                return TrackingScene.VersionId;
            }
        }
        public static string TrackingSceneName
        {
            get
            {
                if (TrackingScene == null) { return ""; }
                return TrackingScene.SceneName;
            }
        }

        public static Cognitive3D_Preferences.SceneSettings TrackingScene { get; private set; }

        /// <summary>
        /// Set the SceneId for recorded data by string
        /// </summary>
        public static void SetTrackingScene(string sceneName, bool writeSceneChangeEvent)
        {
            var scene = Cognitive3D_Preferences.FindScene(sceneName);
            SetTrackingScene(scene, writeSceneChangeEvent);
        }

        private static float SceneStartTime;
        internal static bool ForceWriteSessionMetadata = false;

        /// <summary>
        /// Set the SceneId for recorded data by reference
        /// </summary>
        /// <param name="scene"></param>
        public static void SetTrackingScene(Cognitive3D_Preferences.SceneSettings scene, bool WriteSceneChangeEvent)
        {
            if (IsInitialized)
            {
                if (WriteSceneChangeEvent)
                {
                    if (scene == null || string.IsNullOrEmpty(scene.SceneId))
                    {
                        //what scene is being loaded
                        float duration = Time.time - SceneStartTime;
                        SceneStartTime = Time.time;
                        new CustomEvent("c3d.SceneChange").SetProperty("Duration", duration).Send();
                    }
                    else
                    {
                        //what scene is being loaded
                        float duration = Time.time - SceneStartTime;
                        SceneStartTime = Time.time;
                        new CustomEvent("c3d.SceneChange").SetProperty("Duration", duration).SetProperty("Scene Name", scene.SceneName).SetProperty("Scene Id", scene.SceneId).Send();
                    }
                }

                //just to send this scene change event
                if (WriteSceneChangeEvent && TrackingScene != null)
                {
                    InvokeSendDataEvent(false);
                }
                ForceWriteSessionMetadata = true;
                TrackingScene = scene;
            }
            else
            {
                Util.logWarning("Trying to set scene without a session!");
            }
        }

        public static string LobbyId { get; private set; }
        public static void SetLobbyId(string lobbyId)
        {
            LobbyId = lobbyId;
        }

        public static void SetParticipantFullName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Util.logWarning("SetParticipantFullName is empty!");
                return;
            }
            ParticipantName = name;
            SetParticipantProperty("name", name);
            if (!HasCustomSessionName)
                SetSessionProperty("c3d.sessionname", name);
        }

        public static void SetParticipantId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                Util.logWarning("SetParticipantId is empty!");
                return;
            }
            if (id.Length > 64)
            {
                Debug.LogError("Cognitive3D SetParticipantId exceeds maximum character limit. Clipping to 64");
                id = id.Substring(0, 64);
            }
            ParticipantId = id;
            SetParticipantProperty("id", id);
        }

        /// <summary>
        /// has the Cognitive3D session started?
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Reset all of the static vars to their default values. Used when a session ends
        /// </summary>
        public static void Reset()
        {
            InvokeEndSessionEvent();
            if (NetworkManager != null)
                NetworkManager.EndSession();
            ParticipantId = null;
            ParticipantName = null;
            _sessionId = null;
            _timestamp = 0;
            DeviceId = null;
            IsInitialized = false;
            TrackingScene = null;
            if (NetworkManager != null)
            {
                GameObject.Destroy(NetworkManager.gameObject);
                //NetworkManager.OnDestroy();
            }
            HasCustomSessionName = false;
            InvokePostEndSessionEvent();

            CognitiveStatics.Reset();
            DynamicManager.Reset();
        }

        //internal static Error InitError;

        /// <summary>
        /// Starts a Cognitive3D session. Records hardware info, creates network manager
        /// returns false if it cannot start the session or session has already started. returns true if it starts a session
        /// </summary>
        /*public static bool Init()
        {
            //_hmd = HMDCamera;


            return true;
        }*/



        public static void SetSessionProperties(List<KeyValuePair<string, object>> kvpList)
        {

            if (kvpList == null) { return; }

            for (int i = 0; i < kvpList.Count; i++)
            {
                SetSessionProperty(kvpList[i].Key, kvpList[i].Value);
            }
        }

        public static void SetSessionProperties(Dictionary<string, object> properties)
        {
            if (properties == null) { return; }

            foreach (var prop in properties)
            {
                SetSessionProperty(prop.Key, prop.Value);
            }
        }

        public static void SetSessionProperty(string key, object value)
        {
            if (value == null) { return; }
            CoreInterface.SetSessionProperty(key, value);
        }

        /// <summary>
        /// writes a value into the session properties if the key has not already been added
        /// for easy use of 'addon' sdks
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSessionPropertyIfEmpty(string key, object value)
        {
            if (value == null) { return; }

            CoreInterface.SetSessionPropertyIfEmpty(key, value);

        }

        /// <summary>
        /// sets a property about the participant in the current session
        /// should first call Core.SetParticipantName() and Core.SetParticipantId()
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetParticipantProperty(string key, object value)
        {
            SetSessionProperty("c3d.participant." + key, value);
        }

        /// <summary>
        /// sets a tag to a session for filtering on the dashboard
        /// MUST contain 12 or fewer characters
        /// </summary>
        /// <param name="tag"></param>
        public static void SetSessionTag(string tag, bool setValue = true)
        {
            if (string.IsNullOrEmpty(tag))
            {
                Debug.LogWarning("Session Tag cannot be empty!");
                return;
            }
            if (tag.Length > 12)
            {
                Debug.LogWarning("Session Tag must be less that 12 characters!");
                return;
            }
            SetSessionProperty("c3d.session_tag." + tag, setValue);
        }

        class AttributeParameters
        {
            public string attributionKey;
            public string sessionId;
            public int sceneVersionId;
        }

        /// <summary>
        /// returns a formatted string to append to a web request
        /// this can be used to identify an event outside of unity
        /// requires javascript to parse this key. see the documentation for details
        /// </summary>
        public static string GetAttributionParameters()
        {
            var ap = new AttributeParameters();
            ap.attributionKey = Cognitive3D_Preferences.Instance.AttributionKey;
            ap.sessionId = SessionID;
            if (TrackingScene != null)
                ap.sceneVersionId = TrackingScene.VersionId;

            return "?c3dAtkd=AK-" + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(ap)));
        }

        public static void SetSessionName(string sessionName)
        {
            HasCustomSessionName = true;
            SetSessionProperty("c3d.sessionname", sessionName);
        }

        public static int GetLocalStorageBatchCount()
        {
            if (DataCache == null)
                return 0;
            return DataCache.NumberOfBatches();
        }
    }
}