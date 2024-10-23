using UnityEngine;
using Cognitive3D;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using Cognitive3D.Serialization;
#if C3D_STEAMVR2
using Valve.VR;
#endif

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Cognitive3DEditor")]

/// <summary>
/// Initializes Cognitive3D Analytics. Add components to track additional events
/// Persists between scenes
/// init components
/// update ticks + events
/// level change events
/// quit and destroy events
/// </summary>

//TODO CONSIDER static framecount variable to avoid Time.frameCount access

namespace Cognitive3D
{
    [HelpURL("https://docs.cognitive3d.com/unity/minimal-setup-guide/")]
    [AddComponentMenu("Cognitive3D/Common/Cognitive 3D Manager",1)]
    [DefaultExecutionOrder(-50)]
    public class Cognitive3D_Manager : MonoBehaviour
    {
        public static readonly string SDK_VERSION = "1.6.1";
    
        private static Cognitive3D_Manager instance;
        public static Cognitive3D_Manager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<Cognitive3D_Manager>();
                }
                return instance;
            }
        }

        private static YieldInstruction playerSnapshotInverval;
        internal static YieldInstruction PlayerSnapshotInverval
        {
            get
            {
                return playerSnapshotInverval;
            }
        }
        
        private static YieldInstruction automaticSendInterval;

        [Tooltip("Start recording analytics when this gameobject becomes active (and after the StartupDelayTime has elapsed)")]
        public bool BeginSessionAutomatically = true;

        [Tooltip("Delay before starting a session. This delay can ensure other SDKs have properly initialized")]
        public float StartupDelayTime = 0;

        /// <summary>
        /// the list of all actively loaded C3D scenes. TODO consider changing this to Cognitive3D_Preferences.SceneSettings type
        /// </summary>
        private readonly List<Scene> sceneList = new List<Scene>();

        [HideInInspector]
        public Transform trackingSpace;

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

        IEnumerator Start()
        {
            GameObject.DontDestroyOnLoad(gameObject);
            if (StartupDelayTime > 0)
            {
                yield return new WaitForSeconds(StartupDelayTime);
            }
            if (BeginSessionAutomatically)
            {
                BeginSession();
            }
        }

        [System.NonSerialized]
        public IGazeRecorder gaze;
        [System.NonSerialized]
        public FixationRecorder fixationRecorder;

        [Obsolete("use Cognitive3D_Manager.BeginSession instead")]
        public void Initialize(string participantName = "", string participantId = "", List<KeyValuePair<string, object>> participantProperties = null)
        {
            BeginSession();

            if (!string.IsNullOrEmpty(participantName))
                SetParticipantFullName(participantName);
            if (!string.IsNullOrEmpty(participantId))
                SetParticipantId(participantId);
            if (participantProperties != null)
                SetSessionProperties(participantProperties);
        }
        //TODO comment the different parts of this startup
        /// <summary>
        /// Start recording a session. Sets SceneId, records basic hardware information, starts coroutines to record other data points on intervals
        /// </summary>
        /// <param name="participantName">friendly name for identifying participant</param>
        /// <param name="participantId">unique id for identifying participant</param>
        public void BeginSession()
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

            SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
            Application.wantsToQuit += WantsToQuit;
            RoomTrackingSpace.TrackingSpaceChanged += UpdateTrackingSpace;

            //sets session properties for system hardware
            //also constructs network and local cache files/readers

            CognitiveStatics.Initialize();

#if UNITY_WEBGL
            DeviceId = System.Guid.NewGuid().ToString();
#else
            DeviceId = SystemInfo.deviceUniqueIdentifier;
#endif

            ExitpollHandler = new ExitPollLocalDataHandler(Application.persistentDataPath + "/c3dlocal/exitpoll/");

            if (Cognitive3D_Preferences.Instance.LocalStorage)
            {
                try
                {
                    DataCache = new DualFileCache(Application.persistentDataPath + "/c3dlocal/");
                }
                catch
                {
                    //data cache can fail if multiple game instances are running at once on the same computer
                    //for example, when testing multiplayer
                }
            }
            GameObject networkGo = new GameObject("Cognitive Network");
            networkGo.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            NetworkManager = networkGo.AddComponent<NetworkManager>();
            NetworkManager.Initialize(DataCache, ExitpollHandler);

            GameplayReferences.Initialize();
            DynamicManager.Initialize();
            CustomEvent.Initialize();
            SensorRecorder.Initialize();

            _timestamp = Util.Timestamp();
            //set session timestamp
            if (string.IsNullOrEmpty(_sessionId))
            {
                _sessionId = (int)SessionTimeStamp + "_" + DeviceId;
            }

            string hmdName = "unknown";
            var hmdDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head);
            if (hmdDevice.isValid)
            {
                hmdName = hmdDevice.name;
            }

            CoreInterface.Initialize(SessionID, SessionTimeStamp, DeviceId, hmdName);
            IsInitialized = true;
            //TODO support skipping spatial gaze data but still recording session properties for XRPF

            // get all loaded scenes. if one has a sceneid, use that
            // if more than one scene has ids (additive scenes), use the first scene in the scene manager list that has id
            var count = SceneManager.sceneCount;
            Scene scene = new Scene();
            for(int i = 0; i < count;i++)
            {
                scene = SceneManager.GetSceneAt(i);
                var cogscene = Cognitive3D_Preferences.FindSceneByPath(scene.path);
                if (cogscene != null && !string.IsNullOrEmpty(cogscene.SceneId))
                {
                    if (!sceneList.Contains(scene))
                    {
                        sceneList.Insert(0, scene);
                    }
                    SetTrackingSceneByPath(cogscene.ScenePath);
                    break;
                }
            }
            
            if (TrackingScene == null)
            {
                Util.logWarning("The scene has not been uploaded to the dashboard. The user activity will not be captured.");
            }

            // TODO: support for additive scenes? According to doc, it'll be somehow considered single mode
            InvokeLevelLoadedEvent(scene, UnityEngine.SceneManagement.LoadSceneMode.Single, true);

            CustomEvent startEvent = new CustomEvent("c3d.sessionStart");
            startEvent.Send();
            playerSnapshotInverval = new WaitForSeconds(Cognitive3D_Preferences.SnapshotInterval);
            automaticSendInterval = new WaitForSeconds(Cognitive3D_Preferences.Instance.AutomaticSendTimer);
            StartCoroutine(Tick());
            Util.logDebug("Cognitive3D Initialized");

            //TODO support for 360 skysphere media recording
            // Initializes gaze through gaze recorder interface
            gaze = gameObject.GetComponent<PhysicsGaze>();
            if (gaze == null)
            {
                gaze = gameObject.AddComponent<PhysicsGaze>();
            }
            gaze.Initialize();

#if C3D_OCULUS
            //eye tracking can be enabled successfully here, but there is a delay when calling OVRPlugin.eyeTrackingEnabled
            //this is used for adding the fixation recorder
            GameplayReferences.EyeTrackingEnabled = false;
            if (GameplayReferences.SDKSupportsEyeTracking)
            {
                //check permissions
                bool eyePermissionGranted = false;
                bool facePermissionGranted = false;

                string FaceTrackingPermission = "com.oculus.permission.FACE_TRACKING";
                string EyeTrackingPermission = "com.oculus.permission.EYE_TRACKING";
                
                eyePermissionGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(EyeTrackingPermission);
                facePermissionGranted = UnityEngine.Android.Permission.HasUserAuthorizedPermission(FaceTrackingPermission);

                if (eyePermissionGranted && facePermissionGranted)
                {
                    //these return true even if they're already started elsewhere
                    var startEyeTrackingResult = OVRPlugin.StartEyeTracking();
                    var faceTrackingResult = OVRPlugin.StartFaceTracking();

                    if (startEyeTrackingResult && faceTrackingResult)
                    {
                        GameplayReferences.EyeTrackingEnabled = true;
                        //everything will be supported and enabled for the fixation recorder
                        fixationRecorder = gameObject.GetComponent<FixationRecorder>();
                        if (fixationRecorder == null)
                        {
                            fixationRecorder = gameObject.AddComponent<FixationRecorder>();
                        }
                        fixationRecorder.Initialize();
                    }
                }
            }
#else
            if (GameplayReferences.SDKSupportsEyeTracking)
            {
                fixationRecorder = gameObject.GetComponent<FixationRecorder>();
                if (fixationRecorder == null)
                {
                    fixationRecorder = gameObject.AddComponent<FixationRecorder>();
                }
                fixationRecorder.Initialize();
            }
#endif

            try
            {
                InvokeSessionBeginEvent();
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);
            }

            SetSessionProperties();
            FlushData();
            StartCoroutine(AutomaticSendData());
        }

        /// <summary>
        /// sets automatic session properties from scripting define symbols, device ids, etc
        /// </summary>
        private void SetSessionProperties()
        {
            SetSessionProperty("c3d.app.name", Application.productName);
            SetSessionProperty("c3d.app.version", Application.version);
            SetSessionProperty("c3d.app.engine.version", Application.unityVersion);
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete && XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed)
#endif
            {
                SendHardwareDataAsSessionProperty();
            }
            var generalSettings = UnityEngine.XR.Management.XRGeneralSettings.Instance;
            if (generalSettings != null && generalSettings.Manager != null)
            {
                var activeLoader = generalSettings.Manager.activeLoader;
                if (activeLoader != null)
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.app.xrplugin", activeLoader.name);
                }
                else
                {
                    Cognitive3D_Manager.SetSessionProperty("c3d.app.xrplugin", "null");
                }
            }
            SetSessionProperty("c3d.app.inEditor", Application.isEditor);
            SetSessionProperty("c3d.version", SDK_VERSION);
#region XRPF_PROPERTIES
#if XRPF
            if (XRPF.PrivacyFramework.Agreement.IsAgreementComplete)
            {
                SetSessionProperty("xrpf.allowed.location.data", XRPF.PrivacyFramework.Agreement.IsLocationDataAllowed);
                SetSessionProperty("xrpf.allowed.hardware.data", XRPF.PrivacyFramework.Agreement.IsHardwareDataAllowed);
                SetSessionProperty("xrpf.allowed.bio.data", XRPF.PrivacyFramework.Agreement.IsBioDataAllowed);
                SetSessionProperty("xrpf.allowed.spatial.data", XRPF.PrivacyFramework.Agreement.IsSpatialDataAllowed);
                SetSessionProperty("xrpf.allowed.social.data", XRPF.PrivacyFramework.Agreement.IsSocialDataAllowed);
            }
#endif
#endregion
        }

        private void SendHardwareDataAsSessionProperty()
        {
            SetSessionProperty("c3d.device.type", SystemInfo.deviceType.ToString());
            SetSessionProperty("c3d.device.cpu", SystemInfo.processorType);
            SetSessionProperty("c3d.device.model", SystemInfo.deviceModel);
            SetSessionProperty("c3d.device.gpu", SystemInfo.graphicsDeviceName);
            SetSessionProperty("c3d.device.os", SystemInfo.operatingSystem);
            SetSessionProperty("c3d.device.memory", Mathf.RoundToInt((float)SystemInfo.systemMemorySize / 1024));
            SetSessionProperty("c3d.deviceid", DeviceId);
            SetSessionProperty("c3d.device.hmd.type", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);

            #region SDK_SPECIFIC
#if C3D_STEAMVR2
        //other SDKs may use steamvr as a base or for controllers (ex, hp omnicept). this may be replaced below
        SetSessionProperty("c3d.device.eyetracking.enabled", false);
        SetSessionProperty("c3d.device.eyetracking.type","None");
        SetSessionProperty("c3d.app.sdktype", "Vive");
#endif

#if C3D_OCULUS
        SetSessionProperty("c3d.device.hmd.type", OVRPlugin.GetSystemHeadsetType().ToString().Replace('_', ' '));
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        //TODO delay and update 'c3d.device.eyetracking.enabled' based on GameplayReferences.EyeTrackingEnabled instead. Oculus eye tracking doesn't initialize immediately
        if (GameplayReferences.SDKSupportsEyeTracking)
        {
            SetSessionProperty("c3d.device.eyetracking.type", "OVR");
        }
        else
        {
            SetSessionProperty("c3d.device.eyetracking.type", "None");
        }
        SetSessionProperty("c3d.app.sdktype", "Oculus");
#elif C3D_PICOVR
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.device.eyetracking.type","Tobii");
        SetSessionProperty("c3d.app.sdktype", "PicoVR");
        SetSessionProperty("c3d.device.model", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);
#elif C3D_PICOXR
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.device.eyetracking.type","Tobii");
        SetSessionProperty("c3d.app.sdktype", "PicoXR");
        SetSessionProperty("c3d.device.model", UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.Head).name);
#elif C3D_MRTK
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.app.sdktype", "MRTK");
#elif C3D_VIVEWAVE
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.app.sdktype", "Vive Wave");
#elif C3D_VARJOVR
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.app.sdktype", "Varjo VR");
#elif C3D_VARJOXR
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.app.sdktype", "Varjo XR");
#elif C3D_OMNICEPT
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.device.eyetracking.type","Tobii");
        SetSessionProperty("c3d.app.sdktype", "HP Omnicept");
#endif
            //eye tracker addons
#if C3D_SRANIPAL
        SetSessionProperty("c3d.device.eyetracking.enabled", GameplayReferences.SDKSupportsEyeTracking);
        SetSessionProperty("c3d.device.eyetracking.type","Tobii");
        SetSessionProperty("c3d.app.sdktype", "Vive Pro Eye");
#elif C3D_WINDOWSMR
        SetSessionProperty("c3d.app.sdktype", "Windows Mixed Reality");
#endif
        SetSessionPropertyIfEmpty("c3d.device.eyetracking.enabled", false);
        SetSessionPropertyIfEmpty("c3d.device.eyetracking.type", "None");
        SetSessionPropertyIfEmpty("c3d.app.sdktype", "Default");
        SetSessionProperty("c3d.app.engine", "Unity");
            #endregion
        }


        /// <summary>
        /// registered to unity's OnSceneLoaded callback. sends outstanding data, then sets correct tracking scene id and refreshes dynamic object session manifest
        /// </summary>
        /// <param name="loadingScene"></param>
        /// <param name="mode"></param>
        // This is not called for first loaded scene
        private void SceneManager_SceneLoaded(Scene loadingScene, LoadSceneMode mode)
        {
            SendSceneLoadEvent(loadingScene.path, loadingScene.path, mode);
            bool loadingSceneHasSceneId = TryGetSceneDataByPath(loadingScene.path, out Cognitive3D_Preferences.SceneSettings c3dscene);

            //SceneManager is clearing all scenes, also clear our stack of SceneIds
            if (mode == LoadSceneMode.Single)
            {
                ResetCachedTrackingSpace();
                Util.ResetLogs();
                sceneList.Clear();
                SceneStartTimeDict.Clear();
            }

            //send all immediately. anything on threads will be out of date when looking for what the current tracking scene is
            FlushData();

            // upload session properties to new scene
            ForceWriteSessionMetadata = true;

            // upload subscriptions to new scene
            CoreInterface.SetSubscriptionDetailsReadyToSerialize(true);


            // If id exist for loaded scene, set new tracking scene
            if (loadingSceneHasSceneId)
            {
                sceneList.Insert(0, loadingScene);
                SetTrackingSceneByPath(loadingScene.path);
            }

            InvokeLevelLoadedEvent(loadingScene, mode, loadingSceneHasSceneId);
        }

        /// <summary>
        /// registered to unity's OnSceneUnloaded callback. sends outstanding data, then removes current scene from scene list
        /// </summary>
        /// <param name="unloadingScene"></param>
        // This is not called for last unloaded scene
        private void SceneManager_SceneUnloaded(Scene unloadingScene)
        {
            SendSceneUnloadEvent(unloadingScene.name, unloadingScene.path);

            // Flush recorded data when scene unloads
            if (TrackingScene != null)
            {
                FlushData();
            }

            // upload session properties to new scene
            ForceWriteSessionMetadata = true;

            // upload subscriptions to new scene
            CoreInterface.SetSubscriptionDetailsReadyToSerialize(true);

            // If a scene unloads (useful in additive cases), the scene will be removed from dictionary
            if (SceneStartTimeDict.ContainsKey(unloadingScene.path))
            {
                SceneStartTimeDict.Remove(unloadingScene.path);
            }

            bool unloadingSceneHasSceneId = TryGetSceneDataByPath(unloadingScene.path, out Cognitive3D_Preferences.SceneSettings c3dscene);
            if (unloadingSceneHasSceneId)
            {
                int index = sceneList.IndexOf(unloadingScene);
                sceneList.RemoveAt(index);
            }

            //unloading the active scene
            if (TrackingScene != null && c3dscene == TrackingScene)
            {
                StartCoroutine(WaitForSceneEventComplete());
            }
        }

        // Waits for scene events to be fully processed before unloading the active scene
        private IEnumerator WaitForSceneEventComplete()
        {
            yield return new WaitForEndOfFrame(); // Wait until the end of frame

            //use the top scene from the scene list
            if (sceneList.Count > 0)
            {
                SetTrackingSceneByPath(sceneList[0].path);
            }
            else
            {
                TrackingScene = null;
            }
        }

        /// <summary>
        /// Sends load scene events when a new scene is loaded
        /// </summary>
        private void SendSceneLoadEvent(string sceneName, string scenePath, LoadSceneMode mode)
        {
            if (IsInitialized)
            {
                if (!string.IsNullOrEmpty(scenePath))
                {
                    if (!SceneStartTimeDict.ContainsKey(scenePath))
                    {
                        SceneStartTimeDict.Add(scenePath, Time.time);
                    }
                    if (TryGetSceneDataByPath(scenePath, out Cognitive3D_Preferences.SceneSettings c3dscene))
                    {
                        new CustomEvent("c3d.SceneLoad")
                            .SetProperty("Scene Load Mode", mode)
                            .SetProperty("Scene Name", c3dscene.SceneName)
                            .SetProperty("Scene Id", c3dscene.SceneId)
                            .Send(Vector3.zero);
                    }
                    else
                    {                  
                        new CustomEvent("c3d.SceneLoad")
                            .SetProperty("Scene Load Mode", mode)
                            .SetProperty("Scene Name", sceneName)
                            .Send(Vector3.zero);
                    }
                }
            }
        }

        /// <summary>
        /// Sends unload scene events when a scene is unloaded
        /// </summary>
        private void SendSceneUnloadEvent(string sceneName, string scenePath)
        {
            if (IsInitialized)
            {
                if (!string.IsNullOrEmpty(scenePath))
                {
                    SceneStartTimeDict.TryGetValue(scenePath, out float sceneTime);
                    float duration = Time.time - sceneTime;
                    if (TryGetSceneDataByPath(scenePath, out Cognitive3D_Preferences.SceneSettings c3dscene))
                    {
                        new CustomEvent("c3d.SceneUnload")
                            .SetProperty("Scene Duration", duration)
                            .SetProperty("Scene Name", c3dscene.SceneName)
                            .SetProperty("Scene Id", c3dscene.SceneId)
                            .Send(Vector3.zero);
                    }
                    else
                    {
                        new CustomEvent("c3d.SceneUnload")
                            .SetProperty("Scene Duration", duration)
                            .SetProperty("Scene Name", sceneName)
                            .Send(Vector3.zero);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if scene exists in Cognitive3D scene settings and has an ID
        /// </summary>
        [Obsolete]
        public static bool TryGetSceneData(string sceneName, out Cognitive3D_Preferences.SceneSettings c3dscene)
        {
            c3dscene = Cognitive3D_Preferences.FindScene(sceneName);
            if (c3dscene != null && !string.IsNullOrEmpty(c3dscene.SceneId))
            {
                return true;
            }
            return false;
        }

        public static bool TryGetSceneDataByPath(string scenePath, out Cognitive3D_Preferences.SceneSettings c3dscene)
        {
            c3dscene = Cognitive3D_Preferences.FindSceneByPath(scenePath);
            if (c3dscene != null && !string.IsNullOrEmpty(c3dscene.SceneId))
            {
                return true;
            }
            return false;
        }

        [HideInInspector]
        public int trackingSpaceIndex;
        private List<Transform> cachedTrackingSpaceList = new List<Transform>();

        /// <summary>
        /// Updates current tracking space to next valid tracking space if exists any
        /// </summary>
        /// <param name="newTrackingSpace"></param>
        private void UpdateTrackingSpace(int index, Transform newTrackingSpace)
        {
            if (newTrackingSpace)
            {
                // Adds the tracking space into list when it becomes enabled
                cachedTrackingSpaceList.Insert(index, newTrackingSpace);
                ++trackingSpaceIndex;
            }
            else
            {
                // Removes the tracking space from list when it becomes disabled
                if (index < cachedTrackingSpaceList.Count && cachedTrackingSpaceList[index])
                {
                    cachedTrackingSpaceList.RemoveAt(index);
                    --trackingSpaceIndex;
                }

                // Check to find any other active tracking space in list
                if (cachedTrackingSpaceList.Count > 0)
                {
                    foreach (Transform cachedTrackingSpace in cachedTrackingSpaceList)
                    {
                        if (cachedTrackingSpace)
                        {
                            trackingSpace = cachedTrackingSpace;
                            return;
                        }
                    }
                }
            }

            trackingSpace = newTrackingSpace;
        }

        private void ResetCachedTrackingSpace()
        {
            trackingSpaceIndex = 0;
            cachedTrackingSpaceList.Clear();
        }

#region Updates and Loops

        /// <summary>
        /// Calls the Tick event on a 0.1 second interval while the session is active. Started in BeginSession
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
        /// Calls a global 'send data' event on a set interval
        /// </summary>
        /// <returns></returns>
        IEnumerator AutomaticSendData()
        {
            while (IsInitialized)
            {
                yield return automaticSendInterval;
                if (!string.IsNullOrEmpty(TrackingSceneId))
                {
                    CoreInterface.Flush(false);
                }
            }
        }

        /// <summary>
        /// Calls update while session is initialized
        /// </summary>
        void Update()
        {
            if (!IsInitialized)
            {
                return;
            }

            InvokeUpdateEvent(Time.unscaledDeltaTime);
        }
        #endregion

#region Application Quit, Session End and OnDestroy
        /// <summary>
        /// End the Cognitive3D session. sends any outstanding data to dashboard and sceneexplorer
        /// requires calling BeginSession to begin recording analytics again with a new session id
        /// </summary>
        public void EndSession()
        {
            Application.wantsToQuit -= WantsToQuit;
            if (IsInitialized)
            {
                double playtime = Util.Timestamp(Time.frameCount) - SessionTimeStamp;
                new CustomEvent("c3d.sessionEnd")
                    .SetProperty("sessionlength", playtime)
                    .SetProperty("Reason", "Quit from script")
                    .Send();
                Util.logDebug("Session End. Duration: " + string.Format("{0:0.00}", playtime));
                ResetSessionData();
            }
        }

        void OnDestroy()
        {
            if (instance != this) { return; }
            if (!Application.isPlaying) { return; }

            if (IsInitialized)
            {
                ResetSessionData();
            }
        }

        void OnApplicationPause(bool paused)
        {
            if (!IsInitialized) { return; }
            CustomEvent pauseEvent = new CustomEvent("c3d.pause").SetProperty("ispaused", paused);
            pauseEvent.Send();
            FlushData();
        }
        bool hasCanceled = false;
        bool WantsToQuit()
        {
            if (hasCanceled)
            {
                return true;
            }
            if (!IsInitialized) { return true; }
            double playtime = Util.Timestamp(Time.frameCount) - SessionTimeStamp;
            Util.logDebug("Session End. Duration: " + string.Format("{0:0.00}", playtime));

#if UNITY_ANDROID && !UNITY_EDITOR
            // if android plugin is initialized or Android platform is used, send end session event from plugin. Otherwise, send it from unity
            if (AndroidPlugin.isInitialized)
            {
                AndroidPlugin.WantsToQuit();
            }
            else
#endif
            {
                new CustomEvent("c3d.sessionEnd").SetProperties(new Dictionary<string, object>
                {
                    { "Reason", "Quit from within app" },
                    { "sessionlength", playtime }
                }).Send();
            }
        
            StartCoroutine(SlowQuit());
            return false;
        }
        IEnumerator SlowQuit()
        {
            FlushData();
            ResetSessionData();
            yield return new WaitForSeconds(0.5f);
            hasCanceled = true;
            Application.Quit();
        }

#endregion

        //data has been sent. this is used to visualize on active session view
        public delegate void onSendData(bool copyDataToCache);

        /// <summary>
        /// call this to immediately send all outstanding data to the dashboard
        /// </summary>
        public static void FlushData()
        {
            if (string.IsNullOrEmpty(TrackingSceneId)) { return; }
            DynamicManager.SendData(true);
            CoreInterface.Flush(true);
        }

        public delegate void onSessionBegin();
        /// <summary>
        /// Called just after a session has begun
        /// </summary>
        public static event onSessionBegin OnSessionBegin;
        private static void InvokeSessionBeginEvent() { if (OnSessionBegin != null) { OnSessionBegin.Invoke(); } }

        public delegate void onSessionEnd();
        /// <summary>
        /// Called just before the session ends. Send any last data at this point
        /// </summary>
        public static event onSessionEnd OnPreSessionEnd;
        private static void InvokeEndSessionEvent() { if (OnPreSessionEnd != null) { OnPreSessionEnd.Invoke(); } }

        /// <summary>
        /// Called just after the session ends. Clean up any callbacks or internal states at this point
        /// </summary>
        public static event onSessionEnd OnPostSessionEnd;
        private static void InvokePostEndSessionEvent() { if (OnPostSessionEnd != null) { OnPostSessionEnd.Invoke(); } }

        public delegate void onUpdate(float deltaTime);
        /// <summary>
        /// Update that only runs when a Cognitive3D session is active
        /// </summary>
        public static event onUpdate OnUpdate;
        private static void InvokeUpdateEvent(float deltaTime) { if (OnUpdate != null) { OnUpdate(deltaTime); } }

        public delegate void onTick();
        /// <summary>
        /// Called on a 0.1 second interval
        /// </summary>
        public static event onTick OnTick;
        private static void InvokeTickEvent() { if (OnTick != null) { OnTick(); } }

        public delegate void onLevelLoaded(Scene scene, LoadSceneMode mode, bool newSceneId);
        /// <summary>
        /// From Unity's SceneManager.SceneLoaded event. Happens after the Manager sends outstanding data and has updated the new SceneId
        /// </summary>
        public static event onLevelLoaded OnLevelLoaded;
        private static void InvokeLevelLoadedEvent(Scene scene, LoadSceneMode mode, bool newSceneId) { if (OnLevelLoaded != null) { OnLevelLoaded(scene, mode, newSceneId); } }

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

        /// <summary>
        /// The C3D scene (with a SceneId) that session data is recording to
        /// </summary>
        public static Cognitive3D_Preferences.SceneSettings TrackingScene { get; private set; }
        /// <summary>
        /// Records the start time of every scene loaded, not just C3D scenes
        /// key: scene path
        /// value: timestamp when scene loaded
        /// </summary>
        private static Dictionary<string,float> SceneStartTimeDict = new Dictionary<string, float>();
        internal static bool ForceWriteSessionMetadata = false;

        /// <summary>
        /// Sets the C3D scene to record session data to, searched by scene name string
        /// </summary>
        /// <param name="scene"></param>
        [Obsolete]
        public static void SetTrackingScene(string sceneName)
        {
            if (IsInitialized)
            {
                if (TryGetSceneData(sceneName, out Cognitive3D_Preferences.SceneSettings c3dscene))
                {
                    TrackingScene = c3dscene;
                }
            }
            else
            {
                Util.logWarning("Trying to set scene without a session!");
            }
        }

        public static void SetTrackingSceneByPath(string scenePath)
        {
            if (IsInitialized)
            {
                if (TryGetSceneDataByPath(scenePath, out Cognitive3D_Preferences.SceneSettings c3dscene))
                {
                    TrackingScene = c3dscene;
                }
            }
            else
            {
                Util.logWarning("Trying to set scene without a session!");
            }
        }

        // Remove this when multiplayer support added
        public static void SetLobbyId(string lobbyId)
        {
            CoreInterface.SetLobbyId(lobbyId);
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
        /// Has the Cognitive3D session started?
        /// </summary>
        public static bool IsInitialized { get; private set; }
        /// <summary>
        /// Reset all of the static vars to their default values. Calls delegates for cleaning up the session
        /// </summary>
        private void ResetSessionData()
        {
            ResetCachedTrackingSpace();
            Util.ResetLogs();
            InvokeEndSessionEvent();
            FlushData();
            CoreInterface.Reset();
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
                Destroy(NetworkManager.gameObject);
            }
            HasCustomSessionName = false;
            InvokePostEndSessionEvent();

            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded -= SceneManager_SceneUnloaded;
            RoomTrackingSpace.TrackingSpaceChanged -= UpdateTrackingSpace;
            CognitiveStatics.Reset();
        }

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
        /// should first call SetParticipantFullName() and SetParticipantId()
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

        /// <summary>
        /// Sets a name for the session to be displayed on the dashboard. If not set, an autogenerated name will be used
        /// </summary>
        /// <param name="sessionName"></param>
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

        public static float GetLocalStorage()
        {
            if (DataCache == null)
                return 0;
            return DataCache.GetCacheFillAmount();
        }
    }
}
