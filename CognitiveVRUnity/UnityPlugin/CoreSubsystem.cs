
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace CognitiveVR
{
    /// <summary>
    /// A delegate for notifying the application when certain async operations have completed
    /// </summary>
    public delegate void Callback(Error error);

	public static class CoreSubsystem
	{
		public static string UserId { get; private set; }
		public static string DeviceId { get; private set; }

        static string _uniqueId;
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

        public static void init(string customerId, string userId, Dictionary<string, object> userProperties, string deviceId, Dictionary<string, object> deviceProperties,string sdkVersion, Callback cb, string hubObjName) 
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

                HttpRequest.init(hubObjName, false);

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

        public static void SendOnQuitRequest(string url, string data)
        {
            HttpRequest.executeAsync(url, data);
        }

        private static bool isValidId(string id)
		{
			return !string.IsNullOrEmpty(id);
		}

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
    }
}

