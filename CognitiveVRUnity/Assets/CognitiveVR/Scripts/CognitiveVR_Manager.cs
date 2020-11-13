﻿using UnityEngine;
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
    [DefaultExecutionOrder(-1)]
    public class CognitiveVR_Manager : MonoBehaviour
    {

#if CVR_META
        [DllImport("MetaVisionDLL", EntryPoint = "getSerialNumberAndCalibration")]
        internal static extern bool GetSerialNumberAndCalibration([MarshalAs(UnmanagedType.BStr), Out] out string serial, [MarshalAs(UnmanagedType.BStr), Out] out string xml);
#endif

#region Events


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

        [Tooltip("Delay before starting a session. This delay can ensure other SDKs have properly initialized")]
        public float StartupDelayTime = 2;

        [Tooltip("Start recording analytics when this gameobject becomes active (and after the StartupDelayTime has elapsed)")]
        public bool InitializeOnStart = true;

#if CVR_AH || CVR_PUPIL
        [Tooltip("Start recording analytics after calibration is successfully completed")]
        public bool InitializeAfterCalibration = true;
#endif

        /// <summary>
        /// sets instance of CognitiveVR_Manager
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

            CognitiveVR.NetworkManager.InitLocalStorage(System.Environment.NewLine);
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

#if CVR_AH
            if (InitializeAfterCalibration)
            {
                if (AdhawkApi.Calibrator.Instance != null)
                {
                    while (!AdhawkApi.Calibrator.Instance.Calibrated)
                    {
                        yield return new WaitForSeconds(1);
                        if (AdhawkApi.Calibrator.Instance == null){break;}
                    }
                }
                Initialize();
            }
#endif
#if CVR_PUPIL
            if (InitializeAfterCalibration)
            {
                if (calibrationController == null)
                    calibrationController = FindObjectOfType<PupilLabs.CalibrationController>();
                if (calibrationController != null)
                    calibrationController.OnCalibrationSucceeded += PupilLabs_OnCalibrationSucceeded;
                else
                    Util.logWarning("Cognitive Manager could not find PupilLabs.CalibrationController in scene. Initialize After Calibration will not work as expected!");
            }
#endif
        }

#if CVR_PUPIL
        PupilLabs.CalibrationController calibrationController;

        private void PupilLabs_OnCalibrationSucceeded()
        {
            calibrationController.OnCalibrationSucceeded -= PupilLabs_OnCalibrationSucceeded;
            Initialize();
        }
#endif


        /// <summary>
        /// Start recording a session. Sets SceneId, records basic hardware information, starts coroutines to record other data points on intervals
        /// </summary>
        /// <param name="participantName">friendly name for identifying participant</param>
        /// <param name="participantId">unique id for identifying participant</param>
        public void Initialize(string participantName="", string participantId = "", List<KeyValuePair<string,object>> participantProperties = null)
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

            CognitiveVR.NetworkManager.InitLocalStorage(System.Environment.NewLine);

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
            UnityEngine.SceneManagement.Scene scene = new UnityEngine.SceneManagement.Scene();
            for(int i = 0; i<count;i++)
            {
                scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                var cogscene = CognitiveVR_Preferences.FindSceneByPath(scene.path);
                if (cogscene != null && !string.IsNullOrEmpty(cogscene.SceneId))
                {
                    Core.SetTrackingScene(cogscene);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(participantName))
                Core.SetParticipantFullName(participantName);
            if (!string.IsNullOrEmpty(participantId))
                Core.SetParticipantId(participantId);

            //sets session properties for system hardware
            Error initError = CognitiveVR.Core.Init(GameplayReferences.HMD);

            Core.InvokeLevelLoadedEvent(scene, UnityEngine.SceneManagement.LoadSceneMode.Single, true);

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
            if (GameplayReferences.SDKSupportsEyeTracking)
            {
                //fixation requires some kind of eye tracking hardware
                FixationRecorder fixationRecorder = FixationRecorder.Instance;
                if (fixationRecorder == null)
                {
                    fixationRecorder = gameObject.AddComponent<FixationRecorder>();
                }
                fixationRecorder.Initialize();
            }

            //if (InitEvent != null) { InitEvent(initError); }
            Core.InvokeInitEvent(initError);

            SetSessionProperties();

            if (participantProperties != null)
                Core.SetSessionProperties(participantProperties);

            Core.EndSessionEvent += Core_EndSessionEvent;
#if CVR_OMNICEPT
            var gliaBehaviour = FindObjectOfType<HP.Omnicept.Unity.GliaBehaviour>();

            if (gliaBehaviour != null)
            {
                gliaBehaviour.OnEyePupillometry.AddListener(RecordEyePupillometry);
                gliaBehaviour.OnHeartRate.AddListener(RecordHeartRate);
                gliaBehaviour.OnCognitiveLoad.AddListener(RecordCognitiveLoad);
            }
#endif
        }

#if CVR_OMNICEPT
        double pupillometryTimestamp;

        //update every 100MS
        void RecordEyePupillometry(HP.Omnicept.Messaging.Messages.EyePupillometry data)
        {
            double timestampMS = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            if (pupillometryTimestamp < timestampMS)
            {
                pupillometryTimestamp = timestampMS + 0.1;
                if (data.LeftPupilDiameter.Confidence > 0.5f && data.LeftPupilDiameter.Size > 1.5f)
                {
                    SensorRecorder.RecordDataPoint("HP.Left Pupil Diameter", data.LeftPupilDiameter.Size, timestampMS);
                }
                if (data.RightPupilDiameter.Confidence > 0.5f && data.RightPupilDiameter.Size > 1.5f)
                {
                    SensorRecorder.RecordDataPoint("HP.Right Pupil Diameter", data.RightPupilDiameter.Size, timestampMS);
                }
            }
        }

        //every 1000MS
        void RecordHeartRate(HP.Omnicept.Messaging.Messages.HeartRate data)
        {
            double timestampMS = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            SensorRecorder.RecordDataPoint("HP.HeartRate", data.Rate, timestampMS);
        }
        
        //every 1000MS
        void RecordCognitiveLoad(HP.Omnicept.Messaging.Messages.CognitiveLoad data)
        {
            double timestampMS = (double)data.Timestamp.SystemTimeMicroSeconds / 1000000.0;
            SensorRecorder.RecordDataPoint("HP.CognitiveLoad", data.CognitiveLoadValue, timestampMS);
            SensorRecorder.RecordDataPoint("HP.CognitiveLoad.Confidence", data.StandardDeviation, timestampMS);
        }
#endif

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
            Core.SetSessionProperty("c3d.app.inEditor", true);
#else
            Core.SetSessionProperty("c3d.app.inEditor", false);
#endif
            Core.SetSessionProperty("c3d.version", Core.SDK_VERSION);

#if UNITY_2019_1_OR_NEWER
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);
#elif UNITY_2017_2_OR_NEWER
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.XR.XRDevice.model);
#else
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
#endif

#if CVR_STEAMVR2 || CVR_STEAMVR
            //other SDKs may use steamvr as a base or for controllers (ex, hp omnicept). this may be replaced below
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Vive");
#endif
#if CVR_FOVE
            Core.SetSessionProperty("c3d.device.hmd.type", "Fove");
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Fove");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Fove");
            Core.SetSessionProperty("c3d.app.sdktype", "Fove");
#elif CVR_SNAPDRAGON
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
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Neurable");
            Core.SetSessionProperty("c3d.app.sdktype", "Neurable");
#elif CVR_ARKIT
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Apple");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "ARKit");
#elif CVR_ARCORE
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Android");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "ARCore");
#elif CVR_GOOGLEVR
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Android");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Google VR");
#elif CVR_HOLOLENS
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Microsoft");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Hololens");
#elif CVR_META
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Meta");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", false);
            Core.SetSessionProperty("c3d.device.eyetracking.type","None");
            Core.SetSessionProperty("c3d.app.sdktype", "Meta");
#elif CVR_VARJO
            Core.SetSessionProperty("c3d.device.hmd.type", "Varjo VR-1");
            Core.SetSessionProperty("c3d.device.hmd.manufacturer", "Varjo");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Varjo");
            Core.SetSessionProperty("c3d.app.sdktype", "Varjo");
#elif CVR_OMNICEPT
            Core.SetSessionProperty("c3d.device.hmd.type", UnityEngine.XR.XRDevice.model);
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","HP Omnicept");
            Core.SetSessionProperty("c3d.app.sdktype", "HP Omnicept");
#endif
            //TODO add XR inputdevice name

            //eye tracker addons
#if CVR_TOBIIVR
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer","HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Tobii");
            Core.SetSessionProperty("c3d.app.sdktype", "Tobii");
#elif CVR_PUPIL
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Pupil");
            Core.SetSessionProperty("c3d.app.sdktype", "Pupil");
#elif CVR_AH
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer", "Google");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Adhawk");
            Core.SetSessionProperty("c3d.app.sdktype", "Adhawk");
#elif CVR_VIVEPROEYE
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.XR.XRDevice.model);
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.manufacturer", "HTC");
            Core.SetSessionProperty("c3d.device.eyetracking.enabled", true);
            Core.SetSessionProperty("c3d.device.eyetracking.type","Vive Pro Eye");
            Core.SetSessionProperty("c3d.app.sdktype", "Vive Pro Eye");
#elif CVR_WINDOWSMR
            Core.SetSessionProperty("c3d.app.sdktype", "Windows Mixed Reality");
#endif
#if UNITY_2019_1_OR_NEWER
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);
#elif UNITY_2017_2_OR_NEWER
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.XR.XRDevice.model);
#else
            Core.SetSessionPropertyIfEmpty("c3d.device.hmd.type", UnityEngine.VR.VRDevice.model);
#endif
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
        [Obsolete("Use Core.SetSessionName instead")]
        public static void SetSessionName(string name)
        {
            Core.SetSessionName(name);
        }

        /// <summary>
        /// sets the user's name property
        /// </summary>
        /// <param name="name"></param>
        [Obsolete("Use Core.SetParticipantFullName and Core.SetParticipantId instead")]
        public static void SetUserName(string name)
        {
            if (!string.IsNullOrEmpty(name))
                Core.SetParticipantFullName(name);
        }

        /// <summary>
        /// sets a constant lobby id shared between multiple sessions. this is for associating sessions together for multiplayer
        /// </summary>
        public static void SetLobbyId(string lobbyId)
        {
            Core.SetLobbyId(lobbyId);
        }

        /// <summary>
        /// registered to unity's OnSceneLoaded callback. sends outstanding data, then sets correct tracking scene id and refreshes dynamic object session manifest
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="mode"></param>
        private void SceneManager_SceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            var loadingScene = CognitiveVR_Preferences.FindScene(scene.name);
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
            
            if (replacingSceneId && CognitiveVR_Preferences.Instance.SendDataOnLevelLoad)
            {
                Core.InvokeSendDataEvent();
            }

            if (replacingSceneId)
            {
                if (loadingScene != null)
                {
                    if (!string.IsNullOrEmpty(loadingScene.SceneId))
                    {
                        Core.SetTrackingScene(scene.name);
                    }
                    else
                    {
                        Core.SetTrackingScene("");
                    }
                }
                else
                {
                    Core.SetTrackingScene("");
                }
            }

            Core.InvokeLevelLoadedEvent(scene, mode, replacingSceneId);
        }

#region Updates and Loops

#if CVR_STEAMVR || CVR_STEAMVR2 || CVR_OCULUS
        GameplayReferences.ControllerInfo tempControllerInfo = null;
#endif

#if CVR_STEAMVR || CVR_STEAMVR2
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
            while (Application.isPlaying)
            {
                yield return playerSnapshotInverval;
                FrameCount = Time.frameCount;
                Core.InvokeTickEvent();
            }
        }

        void Update()
        {
            if (initResponse != Error.None)
            {
                return;
            }

            Core.InvokeUpdateEvent(Time.deltaTime);
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
            CognitiveVR_Preferences prefs = CognitiveVR_Preferences.Instance;

            if (!prefs.SendDataOnHotkey) { return; }
            if (Input.GetKeyDown(prefs.SendDataHotkey))
            {
                if (prefs.HotkeyShift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) { return; }
                if (prefs.HotkeyAlt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) { return; }
                if (prefs.HotkeyCtrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) { return; }

                Core.InvokeSendDataEvent();
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
            if (initResponse == Error.None)
            {
                double playtime = Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp;
                new CustomEvent("c3d.sessionEnd").SetProperty("sessionlength", playtime).Send();

                Core.InvokeSendDataEvent();
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
                initResponse = Error.NotInitialized;
                Core.Reset();
            }
        }

        private void Core_EndSessionEvent()
        {
            Core.EndSessionEvent -= Core_EndSessionEvent;
#if CVR_OMNICEPT
            var gliaBehaviour = FindObjectOfType<HP.Omnicept.Unity.GliaBehaviour>();

            if (gliaBehaviour != null)
            {
                gliaBehaviour.OnEyePupillometry.RemoveListener(RecordEyePupillometry);
                gliaBehaviour.OnHeartRate.RemoveListener(RecordHeartRate);
                gliaBehaviour.OnCognitiveLoad.RemoveListener(RecordCognitiveLoad);
            }
#endif
        }

        void OnDestroy()
        {
            if (instance != this) { return; }
            if (!Application.isPlaying) { return; }

            Core.InvokeQuitEvent();

            if (Core.IsInitialized)
            {
                Core.Reset();
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            initResponse = Error.NotInitialized;
        }

        void OnApplicationPause(bool paused)
        {
            if (!Core.IsInitialized) { return; }
            if (CognitiveVR_Preferences.Instance.SendDataOnPause)
            {
                new CustomEvent("c3d.pause").SetProperty("is paused", paused).Send();
                Core.InvokeSendDataEvent();
            }
        }

        bool hasCanceled = false;
        void OnApplicationQuit()
        {
            IsQuitting = true;
            if (hasCanceled) { return; }
            if (InitResponse != Error.None) { return; }

            double playtime = Util.Timestamp(Time.frameCount) - Core.SessionTimeStamp;
            CognitiveVR.Util.logDebug("session length " + playtime);
            if (Core.IsQuitEventBound())
            {
                new CustomEvent("Session End").SetProperty("sessionlength",playtime).Send();
                return;
            }
            new CustomEvent("Session End").SetProperty("sessionlength", playtime).Send();
            Application.CancelQuit();

            Core.InvokeQuitEvent();
            Core.QuitEventClear();
            

            Core.InvokeSendDataEvent();
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