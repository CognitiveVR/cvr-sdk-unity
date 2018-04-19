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
        public const string SDK_VERSION = "0.7.3";

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
                Debug.Log("check session id create new sessionid");
                _sessionId = (int)SessionTimeStamp + "_" + UniqueID;
            }
        }

        public static string CurrentSceneId;
        public static int CurrentSceneVersionNumber;
        //public static int CurrensSceneVersionId; //was set in cognitivevr_manager on scene change; never used

        public static bool Initialized { get; private set; }

        // //////////////////////////
        // Private helper methods //
        // //////////////////////////

        public static void reset()
        {
            // Reset all of the static vars to their default values
            UserId = null;
            _sessionId = null;
            _timestamp = 0;
            _uniqueId = null;
            DeviceId = null;
            Initialized = false;
            CurrentSceneId = null;
            CurrentSceneVersionNumber = 0;
        }

        /// <summary>
        /// Initializes CognitiveVR Framework for use, including instrumentation and tuning.
        /// </summary>
        /// <param name="initParams">Initialization parameters</param>
        /// <param name="cb">Application defined callback which will occur on completion</param>
        public static void init(Callback cb)
        {
            Debug.Log("CognitivrVR.Core.init()");
            // this should only be enabled during android development!!!
            //AndroidJNIHelper.debug = true;

            Error error = Error.Success;

            // Enable/disable logging
            //Util.setLogEnabled(initParams.logEnabled);

            /*if (null == deviceInfo)
                deviceInfo = EntityInfo.createDeviceInfo();
            if (null == userInfo)
                userInfo = EntityInfo.createUserInfo(null);*/

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
                    CheckSessionId();
                    Util.logDebug("Begin session " + SessionID);

                    //update device state
                    //update user state
                    //new device - from response
                    //new user - from response

                    Initialized = true;

                    //TODO check that app can reach dashboard
                    cb.Invoke(Error.Success);
                }
            }
            else if (null != cb)
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
        }
    }
}
