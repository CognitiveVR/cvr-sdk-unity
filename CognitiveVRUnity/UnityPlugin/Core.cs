using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CognitiveVR 
{
    public delegate void Callback(Error error);

    /// <summary>
    /// The most central pieces of the CognitiveVR Framework.
    /// </summary>
    public static class Core
    {
        public delegate void onSendData(); //send data
        /// <summary>
        /// called when CognitiveVR_Manager.SendData is called. this is called when the data is actually sent to the server
        /// </summary>
        public static event onSendData OnSendData;
        public static void SendDataEvent() { if (OnSendData != null) { OnSendData(); } }


        private const string SDK_NAME_PREFIX = "unity";
        public const string SDK_VERSION = "0.10.0";

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

        public static void SetTrackingScene(string sceneName)
        {
            var scene = CognitiveVR_Preferences.FindScene(sceneName);
            SetTrackingScene(scene);
        }

        public static void SetTrackingScene(CognitiveVR_Preferences.SceneSettings scene)
        {
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

        public static bool Initialized { get; private set; }

        public static void reset()
        {
            // Reset all of the static vars to their default values
            UserId = null;
            _sessionId = null;
            _timestamp = 0;
            _uniqueId = null;
            DeviceId = null;
            Initialized = false;
            TrackingSceneId = "";
            TrackingSceneVersionNumber = 0;
            TrackingSceneName = "";
            TrackingScene = null;
        }

        /// <summary>
        /// Initializes CognitiveVR Framework for use, including instrumentation and tuning.
        /// </summary>
        /// <param name="initParams">Initialization parameters</param>
        /// <param name="cb">Application defined callback which will occur on completion</param>
        public static Error init()
        {
            CognitiveStatics.Initialize();
            Error error = Error.None;
            // Have we already initialized CognitiveVR?
            if (Initialized)
            {
                Util.logWarning("CognitiveVR has already been initialized, no need to re-initialize");
                error = Error.AlreadyInitialized;
            }

            if (Error.None == error)
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

                //Util.cacheDeviceAndAppInfo();
                DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;
                SetSessionProperty("c3d.deviceid", DeviceId);

                //initialize Network Manager early, before gameplay actually starts
                var temp = NetworkManager.Sender;

                //set session timestamp
                CheckSessionId();
                Util.logDebug("Begin session " + SessionID);

                Initialized = true;
            }

            return error;
        }

        public static Dictionary<string, object> GetNewSessionProperties(bool clearNewProperties)
        {
            if (clearNewProperties)
            {
                if (newSessionProperties.Count > 0)
                {
                    Dictionary<string, object> returndict = new Dictionary<string, object>(newSessionProperties);
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
        static Dictionary<string, object> newSessionProperties = new Dictionary<string, object>(32);
        static Dictionary<string, object> knownSessionProperties = new Dictionary<string, object>(32);

        [System.Obsolete("use SetSessionProperties")]
        public static void UpdateSessionState(Dictionary<string, object> dictionary)
        {
            SetSessionProperties(dictionary);
        }
        public static void SetSessionProperties(Dictionary<string, object> dictionary)
        {
            if (dictionary == null) { dictionary = new Dictionary<string, object>(); }
            foreach (var kvp in dictionary)
            {
                if (knownSessionProperties.ContainsKey(kvp.Key) && knownSessionProperties[kvp.Key] != kvp.Value) //update value
                {
                    if (newSessionProperties.ContainsKey(kvp.Key))
                    {
                        newSessionProperties[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        newSessionProperties.Add(kvp.Key, kvp.Value);
                    }
                    knownSessionProperties[kvp.Key] = kvp.Value;
                }
                else if (!knownSessionProperties.ContainsKey(kvp.Key)) //add value
                {
                    knownSessionProperties.Add(kvp.Key, kvp.Value);
                    newSessionProperties.Add(kvp.Key, kvp.Value);
                }
            }
        }
        [System.Obsolete("use SetSessionProperty")]
        public static void UpdateSessionState(string key, object value)
        {
            SetSessionProperty(key, value);
        }
        public static void SetSessionProperty(string key, object value)
        {
            if (knownSessionProperties.ContainsKey(key) && knownSessionProperties[key] != value) //update value
            {
                if (newSessionProperties.ContainsKey(key))
                {
                    newSessionProperties[key] = value;
                }
                else
                {
                    newSessionProperties.Add(key, value);
                }
                knownSessionProperties[key] = value;
            }
            else if (!knownSessionProperties.ContainsKey(key)) //add value
            {
                knownSessionProperties.Add(key, value);
                newSessionProperties.Add(key, value);
            }
        }

        /// <summary>
        /// writes a value into the session properties if the key has not already been added
        /// for easy use of 'addon' sdks
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        [System.Obsolete("use SetSessionPropertyIfEmpty")]
        public static void UpdateSessionStateIfEmpty(string key, object value)
        {
            SetSessionPropertyIfEmpty(key, value);
        }
        public static void SetSessionPropertyIfEmpty(string key, object value)
        {
            if (knownSessionProperties.ContainsKey(key)) { return; }
            if (knownSessionProperties.ContainsKey(key) && knownSessionProperties[key] != value) //update value
            {
                if (newSessionProperties.ContainsKey(key))
                {
                    newSessionProperties[key] = value;
                }
                else
                {
                    newSessionProperties.Add(key, value);
                }
                knownSessionProperties[key] = value;
            }
            else if (!knownSessionProperties.ContainsKey(key)) //add value
            {
                knownSessionProperties.Add(key, value);
                newSessionProperties.Add(key, value);
            }
        }
    }
}
