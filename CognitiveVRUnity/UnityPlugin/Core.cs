using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR 
{
    /// <summary>
    /// The most central pieces of the CognitiveVR Framework.
    /// </summary>
    public static class Core
    {
        public delegate void onSendData(); //send data
        /// <summary>
        /// invoked when CognitiveVR_Manager.SendData is called or when the session ends
        /// </summary>
        public static event onSendData OnSendData;
        public static void InvokeSendDataEvent() { if (OnSendData != null) { OnSendData(); } }

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

        private const string SDK_NAME_PREFIX = "unity";
        public const string SDK_VERSION = "0.16.0";

        public static string UserId { get; set; }
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

        private static string _uniqueId;
        public static string UniqueID
        {
            get
            {
                if (string.IsNullOrEmpty(_uniqueId))
                {
                    if (!string.IsNullOrEmpty(UserId))
                        _uniqueId = UserId;
                    else
                        _uniqueId = DeviceId;
                }
                return _uniqueId;
            }
        }

        private static double _timestamp;
        public static double SessionTimeStamp
        {
            get
            {
                if (_timestamp < 1)
                    _timestamp = Util.Timestamp();
                return _timestamp;
            }
        }

        private static string _sessionId;
        public static string SessionID
        {
            get
            {
                if (string.IsNullOrEmpty(_sessionId))
                {
                    _sessionId = (int)SessionTimeStamp + "_" + UniqueID;
                }
                return _sessionId;
            }
        }

        //sets session timestamp, uniqueid and sessionid if not set otherwise
        public static void CheckSessionId()
        {
            if (string.IsNullOrEmpty(_sessionId))
            {
                _sessionId = (int)SessionTimeStamp + "_" + UniqueID;
            }
        }

        public static string TrackingSceneId { get; private set; }
        public static int TrackingSceneVersionNumber { get; private set; }
        public static string TrackingSceneName { get; private set; }

        public static CognitiveVR_Preferences.SceneSettings TrackingScene {get; private set;}

        /// <summary>
        /// Set the SceneId for recorded data by string
        /// </summary>
        public static void SetTrackingScene(string sceneName)
        {
            var scene = CognitiveVR_Preferences.FindScene(sceneName);
            SetTrackingScene(scene);
        }

        private static float SceneStartTime;

        /// <summary>
        /// Set the SceneId for recorded data by reference
        /// </summary>
        /// <param name="scene"></param>
        public static void SetTrackingScene(CognitiveVR_Preferences.SceneSettings scene)
        {
            if (scene == null)
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

            //just to send this scene change event
            Core.InvokeSendDataEvent();

            TrackingSceneId = "";
            TrackingSceneVersionNumber = 0;
            TrackingSceneName = "";
            TrackingScene = null;
            if (scene != null)
            {
                TrackingSceneId = scene.SceneId;
                TrackingSceneVersionNumber = scene.VersionNumber;
                TrackingSceneName = scene.SceneName;
                TrackingScene = scene;
            }
        }

        public static string LobbyId { get; private set; }
        public static void SetLobbyId(string lobbyId)
        {
            LobbyId = lobbyId;
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
            NetworkManager.Sender.EndSession();
            UserId = null;
            _sessionId = null;
            _timestamp = 0;
            _uniqueId = null;
            DeviceId = null;
            IsInitialized = false;
            TrackingSceneId = "";
            TrackingSceneVersionNumber = 0;
            TrackingSceneName = "";
            TrackingScene = null;
            GameObject.Destroy(NetworkManager.Sender.gameObject);
        }

        /// <summary>
        /// Starts a CognitiveVR session. Records hardware info, creates network manager
        /// </summary>
        public static Error Init(Transform HMDCamera)
        {
            _hmd = HMDCamera;
            CognitiveStatics.Initialize();

            Error error = Error.None;
            // Have we already initialized CognitiveVR?
            if (IsInitialized)
            {
                Util.logWarning("CognitiveVR has already been initialized, no need to re-initialize");
                error = Error.AlreadyInitialized;
            }

            if (error == Error.None)
            {
                SetSessionProperty("c3d.app.name", Application.productName);
                SetSessionProperty("c3d.app.version", Application.version);
                SetSessionProperty("c3d.app.engine.version", Application.unityVersion);
                SetSessionProperty("c3d.device.type", SystemInfo.deviceType.ToString());
                SetSessionProperty("c3d.device.cpu", SystemInfo.processorType);
                SetSessionProperty("c3d.device.model", SystemInfo.deviceModel);
                SetSessionProperty("c3d.device.gpu", SystemInfo.graphicsDeviceName);
                SetSessionProperty("c3d.device.os", SystemInfo.operatingSystem);
                SetSessionProperty("c3d.device.memory", Mathf.RoundToInt(SystemInfo.systemMemorySize/1024));
                
                DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
                SetSessionProperty("c3d.deviceid", DeviceId);

                //initialize Network Manager early, before gameplay actually starts
                var temp = NetworkManager.Sender;

                DynamicManager.Initialize();
                DynamicObjectCore.Initialize();

                //set session timestamp
                CheckSessionId();

                IsInitialized = true;
            }

            return error;
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

        static List<KeyValuePair<string, object>> newSessionProperties = new List<KeyValuePair<string, object>>(32);
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
    }
}
