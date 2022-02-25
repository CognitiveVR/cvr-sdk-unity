using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR 
{
    /// <summary>
    /// The most central pieces of the CognitiveVR Framework.
    /// </summary>
    public static class Core
    {
        public const string SDK_VERSION = "0.26.16";

        public delegate void onSendData(bool copyDataToCache); //send data
        /// <summary>
        /// invoked when CognitiveVR_Manager.SendData is called or when the session ends
        /// </summary>
        public static event onSendData OnSendData;

        /// <summary>
        /// call this to send all outstanding data to the dashboard
        /// </summary>
        public static void InvokeSendDataEvent(bool copyDataToCache) { if (OnSendData != null) { OnSendData(copyDataToCache); } }

        public delegate void CoreInitHandler(Error initError);
        /// <summary>
        /// CognitiveVR Core.Init callback
        /// </summary>
        public static event CoreInitHandler InitEvent;
        public static void InvokeInitEvent(Error initError) { if (InitEvent != null) { InitEvent.Invoke(initError); } }

        public delegate void CoreEndSessionHandler();
        /// <summary>
        /// CognitiveVR Core.Init callback
        /// </summary>
        public static event CoreEndSessionHandler EndSessionEvent;
        public static void InvokeEndSessionEvent() { if (EndSessionEvent != null) { EndSessionEvent.Invoke(); } }

        public static event CoreEndSessionHandler OnPostSessionEnd;
        public static void InvokePostEndSessionEvent() { if (OnPostSessionEnd != null) { OnPostSessionEnd.Invoke(); } }

        public delegate void UpdateHandler(float deltaTime);
        /// <summary>
        /// Update. Called through Manager's update function
        /// </summary>
        public static event UpdateHandler UpdateEvent;
        public static void InvokeUpdateEvent(float deltaTime) { if (UpdateEvent != null) { UpdateEvent(deltaTime); } }

        public delegate void TickHandler();
        /// <summary>
        /// repeatedly called if the sceneid is valid. interval is CognitiveVR_Preferences.Instance.PlayerSnapshotInterval
        /// </summary>
        public static event TickHandler TickEvent;
        public static void InvokeTickEvent() { if (TickEvent != null) { TickEvent(); } }

        public delegate void QuitHandler();
        /// <summary>
        /// called from Unity's built in OnApplicationQuit. Cancelling quit gets weird - do all application quit stuff in Manager
        /// </summary>
        public static event QuitHandler QuitEvent;
        public static void InvokeQuitEvent() { if (QuitEvent != null) { QuitEvent(); } }
        public static bool IsQuitEventBound() { return QuitEvent != null; }
        public static void QuitEventClear() { QuitEvent = null; }

        public delegate void LevelLoadedHandler(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId);
        /// <summary>
        /// from Unity's SceneManager.SceneLoaded event. happens after manager sends outstanding data and updates new SceneId
        /// </summary>
        public static event LevelLoadedHandler LevelLoadedEvent;
        public static void InvokeLevelLoadedEvent(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode, bool newSceneId) { if (LevelLoadedEvent != null) { LevelLoadedEvent(scene, mode, newSceneId); } }

        public delegate void onDataSend();

        private static Transform _hmd;
        internal static Transform HMD
        {
            get
            {
                if (_hmd == null)
                {
                    if (Camera.main == null)
                    {
                        Camera c = UnityEngine.Object.FindObjectOfType<Camera>();
                        if (c != null)
                            _hmd = c.transform;
                    }
                    else
                        _hmd = Camera.main.transform;
                }
                return _hmd;
            }
        }

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
        public static string TrackingSceneName
        {
            get
            {
                if (TrackingScene == null) { return ""; }
                return TrackingScene.SceneName;
            }
        }

        public static CognitiveVR_Preferences.SceneSettings TrackingScene {get; private set;}

        /// <summary>
        /// Set the SceneId for recorded data by string
        /// </summary>
        public static void SetTrackingScene(string sceneName, bool writeSceneChangeEvent)
        {
            var scene = CognitiveVR_Preferences.FindScene(sceneName);
            SetTrackingScene(scene, writeSceneChangeEvent);
        }

        private static float SceneStartTime;
        internal static bool ForceWriteSessionMetadata = false;

        /// <summary>
        /// Set the SceneId for recorded data by reference
        /// </summary>
        /// <param name="scene"></param>
        public static void SetTrackingScene(CognitiveVR_Preferences.SceneSettings scene, bool WriteSceneChangeEvent)
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
                Core.InvokeSendDataEvent(false);
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
        /// has the CognitiveVR session started?
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

        internal static Error InitError;
        public static Error GetInitError()
        {
            return InitError;
        }

        /// <summary>
        /// Starts a CognitiveVR session. Records hardware info, creates network manager
        /// </summary>
        public static Error Init(Transform HMDCamera)
        {
            _hmd = HMDCamera;
            CognitiveStatics.Initialize();

            InitError = Error.None;
            // Have we already initialized CognitiveVR?
            if (IsInitialized)
            {
                Util.logWarning("CognitiveVR has already been initialized, no need to re-initialize");
                InitError = Error.AlreadyInitialized;
            }

            if (InitError == Error.None)
            {
                DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;

                ExitpollHandler = new ExitPollLocalDataHandler(Application.persistentDataPath + "/c3dlocal/exitpoll/");

                if (CognitiveVR_Preferences.Instance.LocalStorage)
                    DataCache = new DualFileCache(Application.persistentDataPath + "/c3dlocal/");
                GameObject networkGo = new GameObject("Cognitive Network");
                networkGo.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
                NetworkManager = networkGo.AddComponent<NetworkManager>();
                NetworkManager.Initialize(DataCache, ExitpollHandler);

                DynamicManager.Initialize();
                DynamicObjectCore.Initialize();
                CustomEvent.Initialize();
                SensorRecorder.Initialize();

                _timestamp = Util.Timestamp();
                //set session timestamp
                if (string.IsNullOrEmpty(_sessionId))
                {
                    _sessionId = (int)SessionTimeStamp + "_" + DeviceId;
                }

                IsInitialized = true;
                if (CognitiveVR_Preferences.Instance.EnableGaze == false)
                    GazeCore.SendSessionProperties(false);
            }

            return InitError;
        }

        public static List<KeyValuePair<string, object>> GetNewSessionProperties(bool clearNewProperties)
        {
            if (clearNewProperties)
            {
                if (newSessionProperties.Count > 0)
                {
                    List<KeyValuePair<string, object>> returndict = new List<KeyValuePair<string, object>>(newSessionProperties);
                    newSessionProperties.Clear();
                    return returndict;
                }
                else
                {
                    return newSessionProperties;
                }
            }
            return newSessionProperties;
        }

        public static List<KeyValuePair<string, object>> GetAllSessionProperties(bool clearNewProperties)
        {
            if (clearNewProperties)
            {
                newSessionProperties.Clear();
            }
            return knownSessionProperties;
        }

        //any changed properties that have not been written to the session
        static List<KeyValuePair<string, object>> newSessionProperties = new List<KeyValuePair<string, object>>(32);

        //all session properties, including new properties not yet sent
        static List<KeyValuePair<string, object>> knownSessionProperties = new List<KeyValuePair<string, object>>(32);
        
        public static void SetSessionProperties(List<KeyValuePair<string, object>> kvpList)
        {

            if (kvpList == null) { return; }

            for(int i = 0; i<kvpList.Count;i++)
            {
                SetSessionProperty(kvpList[i].Key, kvpList[i].Value);
            }
        }

        public static void SetSessionProperties(Dictionary<string, object> properties)
        {
            if (properties == null) { return; }

            foreach(var prop in properties)
            {
                SetSessionProperty(prop.Key, prop.Value);
            }
        }
        
        public static void SetSessionProperty(string key, object value)
        {
            if (value == null) { return; }
            int foundIndex = 0;
            bool foundKey = false;
            for(int i = 0; i< knownSessionProperties.Count;i++)
            {
                if (knownSessionProperties[i].Key == key)
                {
                    foundKey = true;
                    foundIndex = i;
                    break;
                }
            }

            if (foundKey) //update value
            {
                if (knownSessionProperties[foundIndex].Value == value) //skip setting property if it hasn't actually changed
                {
                    return;
                }
                else
                {
                    knownSessionProperties[foundIndex] = new KeyValuePair<string, object>(key, value);

                    bool foundNewSessionPropKey = false;
                    int foundNewSessionPropIndex = 0;
                    for (int i = 0; i < newSessionProperties.Count; i++) //add/replace in 'newSessionProperty' (ie dirty value that will be sent with gaze)
                    {
                        if (newSessionProperties[i].Key == key)
                        {
                            foundNewSessionPropKey = true;
                            foundNewSessionPropIndex = i;
                            break;
                        }
                    }
                    if (foundNewSessionPropKey)
                    {
                        newSessionProperties[foundNewSessionPropIndex] = new KeyValuePair<string, object>(key, value);
                    }
                    else
                    {
                        newSessionProperties.Add(new KeyValuePair<string, object>(key, value));
                    }
                }
            }
            else
            {
                knownSessionProperties.Add(new KeyValuePair<string, object>(key, value));
                newSessionProperties.Add(new KeyValuePair<string, object>(key, value));
            }
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
            for (int i = 0; i < knownSessionProperties.Count; i++)
            {
                if (knownSessionProperties[i].Key == key)
                {
                    return;
                }
            }

            knownSessionProperties.Add(new KeyValuePair<string, object>(key, value));
            newSessionProperties.Add(new KeyValuePair<string, object>(key, value));
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
            ap.attributionKey = CognitiveVR_Preferences.Instance.AttributionKey;
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
