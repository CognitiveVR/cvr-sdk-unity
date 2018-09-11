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
        public const string SDK_VERSION = "0.8.3";

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
        public static void init(Callback cb)
        {
            Constants.Initialize();
            Error error = Error.Success;

            if (null == cb)
            {
                Util.logError("Please provide a valid CognitiveVR.Callback");
                error = Error.InvalidArgs;
            }

            if (Error.Success == error)
            {
                Error ret = Error.Success;

                // Have we already initialized CognitiveVR?
                if (Initialized)
                {
                    Util.logError("CognitiveVR has already been initialized, no need to re-initialize");
                    ret = Error.AlreadyInitialized;
                }
                else if (null == cb)
                {
                    Util.logError("Please provide a valid callback");
                    ret = Error.InvalidArgs;
                }

                if (Error.Success == ret)
                {
                    Util.cacheDeviceAndAppInfo();
                    DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;

                    //initialize Network Manager early, before gameplay actually starts
                    var temp = NetworkManager.Sender;

                    //set session timestamp
                    CheckSessionId();
                    Util.logDebug("Begin session " + SessionID);

                    Initialized = true;

                    //TODO check that app can reach dashboard
                    cb.Invoke(Error.Success);
                }
            }
            else
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
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
        static Dictionary<string, object> newSessionProperties = new Dictionary<string, object>();
        static Dictionary<string, object> knownSessionProperties = new Dictionary<string, object>();
        public static void UpdateSessionState(Dictionary<string, object> dictionary)
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
        public static void UpdateSessionState(string key, object value)
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
    }
}
