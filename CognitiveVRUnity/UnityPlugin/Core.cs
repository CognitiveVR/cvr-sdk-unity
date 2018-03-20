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
        //!!important no data is sent unless begin session has been called (TODO make this true). need some place to set timestamp and sessionid

        //public variables
        //session timestamp
        //session id

        //public functions
        //begin session + send default device data
        //end session
        //set current scene settings (id, version number, version id)
        //set user data
        //set device data


        public delegate void onSendData(); //send data
        /// <summary>
        /// called when CognitiveVR_Manager.SendData is called. this is called when the data is actually sent to the server
        /// </summary>
        public static event onSendData OnSendData;
        public static void SendDataEvent() { if (OnSendData != null) { OnSendData(); } }


        private const string SDK_NAME_PREFIX = "unity";
        private const string SDK_VERSION = "0.6.26";
        public static string SDK_Version { get { return SDK_VERSION; } }
        internal const string HUB_OBJECT = "CognitiveVR_Manager";

        internal static string UserId { get; private set; }
        internal static string DeviceId { get; private set; }

        private static string _uniqueId;
        internal static string _UniqueID
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
                    _sessionId = (int)SessionTimeStamp + "_" + _UniqueID;
                }
                return _sessionId;
            }
        }

        public static string CurrentSceneId;
        public static int CurrentSceneVersionNumber;
        public static int CurrensSceneVersionId;

        /*public static string SimpleHMDName { get; private set; }
        public static void SetSimpleHMDName(string name)
        {
            SimpleHMDName = name;
        }*/

        public static bool Initialized { get; private set; }
        private static string sCustomerId;
        private static string sSDKVersion;

        //(customerId, userInfo.entityId, userInfo.properties, deviceInfo.entityId, deviceInfo.properties, SDK_VERSION, cb, HUB_OBJECT);

        public static void init(string customerId, string userId, Dictionary<string, object> userProperties, string deviceId, Dictionary<string, object> deviceProperties, string sdkVersion, Callback cb, string hubObjName)
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
                sCustomerId = customerId;
                sSDKVersion = sdkVersion;

                Util.cacheDeviceAndAppInfo();

                // set up device id & user id now, in case initial server call doesn't make it back (offline usage, etc)
                //if (isValidId(deviceId)) DeviceId = deviceId;
                //if (isValidId(userId)) UserId = userId;

                DeviceId = UnityEngine.SystemInfo.deviceUniqueIdentifier;

                // add any auto-scraped device state
                /*IDictionary<string, object> deviceAndAppInfo = Util.GetDeviceProperties();
				if (null == deviceProperties)
				{
                    deviceProperties = deviceAndAppInfo as Dictionary<string, object>;
				}
				else
				{
                    try
                    {
                        foreach (var info in deviceAndAppInfo)
                        {
                            if (!deviceProperties.ContainsKey(info.Key))
                                deviceProperties.Add(info.Key, info.Value);
                        }
                    }
                    catch (ArgumentException)
                    {
                        Util.logError("device properties passed in have a duplicate key to the auto-scraped properties!");
                    }
				}*/

                //HttpRequest.init(hubObjName, false);

                //set session timestamp
                var sessionid = SessionID;
                Util.logDebug("Begin session " + sessionid);

                //update device state
                //update user state
                //new device - from response
                //new user - from response

                Initialized = true;

                //TODO check that app can reach dashboard
                cb.Invoke(Error.Success);
            }

            /*if ((Error.Success != ret) && (null != cb))
            {
                cb(ret);
            }*/
        }

        /*public static void SendOnQuitRequest(string url, string data)
        {
            HttpRequest.executeAsync(url, data);
        }*/

        // //////////////////////////
        // Private helper methods //
        // //////////////////////////

        public static void reset()
        {
            // Reset all of the static vars to their default values
            sCustomerId = null;
            sSDKVersion = null;
            UserId = null;
            _sessionId = null;
            _timestamp = 0;
            DeviceId = null;
            Initialized = false;
            CurrentSceneId = null;
            CurrentSceneVersionNumber = 0;
        }

        /// <summary>
        /// Gets the registered id for the currently active user
        /// </summary>
        /// <value>The user id</value>
        public static string userId
        {
            get
            {
                return UserId;
            }
        }

        /// <summary>
        /// Gets the registered id for the device
        /// </summary>
        /// <value>The device id</value>
        public static string deviceId
        {
            get
            {
                return DeviceId;
            }
        }

        /// <summary>
        /// returns userID. if userID is empty, returns deviceID
        /// </summary>
        public static string UniqueID
        {
            get
            {
                return _UniqueID;
            }
        }

        /// <summary>
        /// Initializes CognitiveVR Framework for use, including instrumentation and tuning.
        /// </summary>
        /// <param name="initParams">Initialization parameters</param>
        /// <param name="cb">Application defined callback which will occur on completion</param>
        public static void init(string customerId, EntityInfo userInfo, EntityInfo deviceInfo, Callback cb)
        {
            Debug.Log("CognitivrVR.Core.init()");
            // this should only be enabled during android development!!!
            //AndroidJNIHelper.debug = true;

            Error error = Error.Success;

            // Enable/disable logging
            //Util.setLogEnabled(initParams.logEnabled);

            if (null == deviceInfo)
                deviceInfo = EntityInfo.createDeviceInfo();
            if (null == userInfo)
                userInfo = EntityInfo.createUserInfo(null);

            if (null == cb)
            {
                Util.logError("Please provide a valid CognitiveVR.Callback");
                error = Error.InvalidArgs;
            }

            GameObject go = GameObject.Find(HUB_OBJECT);
            if (null == go) go = new GameObject(HUB_OBJECT);
            GameObject.DontDestroyOnLoad(go);

            if (Error.Success == error)
            {                
                init(customerId, userInfo.entityId, userInfo.Properties, deviceInfo.entityId, deviceInfo.Properties, SDK_VERSION, cb, HUB_OBJECT);
            }
            else if (null != cb)
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
        }
    }
}
