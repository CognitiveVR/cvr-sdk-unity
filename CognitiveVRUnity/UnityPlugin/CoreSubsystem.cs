
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using CognitiveVR.External.MiniJSON; 

namespace CognitiveVR
{
    /// <summary>
    /// A delegate for notifying the application when certain async operations have completed
    /// </summary>
    public delegate void Callback(Error error);

	public static class CoreSubsystem
	{
		// Private "constants"
		private const string WS_VERSION              = "4";
		private const string DEVICEID_KEY_NAME       = "deviceId";

        private static List<string> sRegisteredUsers = new List<string>();
        internal static List<string> getRegisteredUsers() { return sRegisteredUsers; }

		public static string UserId { get; private set; }
		public static string DeviceId { get; private set; }

        public static string UniqueID
        {
            get
            {
                if (string.IsNullOrEmpty(UserId))
                {
                    return UserId;
                }
                return DeviceId;
            }
        }

        private static double _timestamp;
        public static double SessionTimeStamp
        {
             get { if (_timestamp < 1) _timestamp = Util.Timestamp(); return _timestamp; }
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

        /*public static string SimpleHMDName { get; private set; }
        public static void SetSimpleHMDName(string name)
        {
            SimpleHMDName = name;
        }*/

        internal static bool Initialized { get; private set; }
		internal static int ReqTimeout { get; private set; }
		internal static string Host { get; private set; }

		private static string sCustomerId;
		private static string sSDKName;
		private static string sSDKVersion;

        public static void init(string customerId, TuningUpdater tuningUpdater, string userId, Dictionary<string, object> userProperties, string deviceId, Dictionary<string, object> deviceProperties, int reqTimeout, string host, bool logEnabled, string sdkNamePre, string sdkVersion, Callback cb, string hubObjName, bool isWebPlayer) 
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
				ReqTimeout = reqTimeout;    
				Host = host;
				sSDKName = Util.getSDKName(sdkNamePre);
				sSDKVersion = sdkVersion;

                Util.cacheDeviceAndAppInfo();
		
				// First see if we have a deviceId stored off locally that we can use
                string savedDeviceId;
                if (!isValidId(deviceId) && Util.TryGetPrefValue(DEVICEID_KEY_NAME, out savedDeviceId))
                {
                    if (isValidId(savedDeviceId))
                    {
                        deviceId = savedDeviceId;
                    }
                }

				// set up device id & user id now, in case initial server call doesn't make it back (offline usage, etc)
				if (isValidId(deviceId)) DeviceId = deviceId;
				if (isValidId(userId)) UserId = userId;
				
				// add any auto-scraped device state
                IDictionary<string, object> deviceAndAppInfo = Util.getDeviceAndAppInfo();
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
				}

                HttpRequest.init(hubObjName, isWebPlayer);

				// No device Id, so let's retrieve one and save it off
				string url = Host + "/isos-personalization/ws/interface/application_init" + getQueryParms();
				IList allArgs = new List<object>(6);
				double curTimeStamp = Util.Timestamp();
				allArgs.Add(curTimeStamp);
				allArgs.Add(curTimeStamp);
				allArgs.Add(userId);
				allArgs.Add(deviceId);
				allArgs.Add(userProperties);
				allArgs.Add(deviceProperties);
				
				try
				{
                    HttpRequest.executeAsync(new Uri(url), ReqTimeout, Json.Serialize(allArgs), new InitRequestListener(tuningUpdater, userProperties, deviceProperties, cb));
				}
				catch (WebException e)
				{
					reset();
					
					Util.logError("WebException during the HttpRequest.  Check your host and customerId values: " + e.Message);
					ret = Error.InvalidArgs;
				}
			    catch (Exception e)
				{
					reset();

					Util.logError("Error during HttpRequest: " + e.Message);
					ret = Error.Generic;
				}
                _timestamp = Util.Timestamp();
            }

            if ((Error.Success != ret) && (null != cb))
            {
                cb(ret);
            }
        }

        public static void registerUser(string userId, IDictionary<string, object> userProperties, TuningUpdater tuningUpdater, Callback cb)
        {
            Error ret = Error.Success;

            if (!Initialized)
            {
                Util.logError("Cannot registerUser before calling init()");
                ret = Error.NotInitialized;
            }
            else if (!isValidId(DeviceId))
            {
                Util.logError("No device Id set.  Check for prior errors");
                ret = Error.MissingId;
            }

            if (Error.Success == ret)
            {
                string deviceId = DeviceId;

                string url = Host + "/isos-personalization/ws/interface/application_updateuser" + getQueryParms();
                List<object> allArgs = new List<object>(4);
				double curTimeStamp = Util.Timestamp();
                allArgs.Add(curTimeStamp);
                allArgs.Add(curTimeStamp);
                allArgs.Add(userId);
                allArgs.Add(deviceId);

                // TODO: It's not a good idea to go around the event depot (out of order issue)
                allArgs.Add(userProperties);

                try
                {
                    // Create an (async) request to add the user. The callback will be triggered when the request is completed
                    HttpRequest.executeAsync(new Uri(url), ReqTimeout, Json.Serialize(allArgs), new InitRequestListener(tuningUpdater, userProperties, null, cb));
				}
				catch (WebException e)
				{
					Util.logError("WebException during the HttpRequest.  Check your host and customerId values: " + e.Message);
					ret = Error.InvalidArgs;
				}
				catch (Exception e)
				{
					Util.logError("Error during HttpRequest: " + e.Message);
					ret = Error.Generic;
				}
            }

            // If we have an error at this point, then the callback will not get called through the HttpRequest, so call it now
            if ((Error.Success != ret) && (null != cb))
            {
                cb(ret);
            }
        }

        /*
         * Explicitly sets the active user id.  Only required when multiple concurrent users
         * are required/supported
         */
        public static Error setActiveUser(string userId)
        {
            if (!isValidId(userId))
            {
                UserId = null;
                return Error.Success;
            }

            if (sRegisteredUsers.Contains(userId))
            {
                UserId = userId;
                return Error.Success;
            }

            Util.logError("User ID " + userId + " has not been registered.  Be sure to call registerUser to prep an id for usage.");
            return Error.InvalidArgs;
        }

        /**
         * Pause the CognitiveVR system.
         * Note that currently this does nothing on native unity/wp8
         */
        [System.Obsolete("CoreSubsystem.pause() is no longer used")]
        public static void pause() { }

        /**
         * Resume the CognitiveVR system.
         * Note that currently this does nothing on native unity/wp8
         */
        [System.Obsolete("CoreSubsystem.resume() is no longer used")]
        public static void resume() { }
          
        /**
         * Helper class
         * 
         * @internal
         * 
         * NOTE: Using this builder pattern as opposed to a static method allows us to support "default parameters"
         */
        internal class DataPointBuilder
        {
            private string  _call;
            private List<object> _args = new List<object>();

            internal DataPointBuilder(string call)
            {
                _call = call;
            }

            internal Error send()
            {
                // Assume success
                Error ret = Error.Success;

                if (Initialized)
                {
                    // Build up the data object
                    List<object> allArgs = new List<object>();

                    double curTimeStamp = Util.Timestamp();
                    // The interface calls require two time stamps (to support batching), so we'll send the same one for both
                    allArgs.Add(curTimeStamp);
                    allArgs.Add(curTimeStamp);
                    allArgs.Add(UserId);
                    allArgs.Add(DeviceId);
                    allArgs.AddRange(_args);

                    // Build the event and store it in the depot
                    IDictionary<string, object> eventData = new Dictionary<string, object>(2);
                    eventData.Add("method", _call);
                    eventData.Add("args", allArgs);

                    ret = EventDepot.store(eventData);
                }
                else
                {
                    ret = Error.NotInitialized;
                }

                return ret;
            }

            internal DataPointBuilder setArg(object obj)
            {
                _args.Add(obj);
                return this;
            }
        }

        private static bool isValidId(string id)
		{
			return !string.IsNullOrEmpty(id);
		}

		internal static string getQueryParms()
		{
			return "?ssf_ws_version=" + WS_VERSION + "&ssf_cust_id=" + sCustomerId + "&ssf_output=json&ssf_sdk=" + sSDKName + "&ssf_sdk_version=" + sSDKVersion;
		}

        // //////////////////////////
        // Private helper methods //
        // //////////////////////////

        private static void reset()
        {
            // Reset all of the static vars to their default values
            sCustomerId = null;
            sSDKName = null;
            sSDKVersion = null;
            ReqTimeout = 0;
            Host = null;
            UserId = null;
            DeviceId = null;
            Initialized = false;
        }


        #region private helper classes
        /**
         * Request listener used for both the {@link init} and {@link registerUser} calls.  The important distinction
         * between the server response from these calls is that only the init() call contains a deviceId.  This is also
         * why the shared preferences is not provided during the registerUser() call, as it is unneeded.
         * 
         */
        private class InitRequestListener : HttpRequest.Listener
        {
            private TuningUpdater               mTuningUpdater;
            private Callback                    mCallback;
            private IDictionary<string, object> mUserProperties;
            private IDictionary<string, object> mDeviceProperties;

            internal InitRequestListener(TuningUpdater tuningUpdater, IDictionary<string, object> userProperties, IDictionary<string, object> deviceProperties, Callback cb)
            {
                mTuningUpdater = tuningUpdater;
                mUserProperties = userProperties;
                mDeviceProperties = deviceProperties;
                mCallback = cb;
            }

            void HttpRequest.Listener.onComplete(HttpRequest.Result result)
            {
                Error retError = Error.Generic;
                bool userNew = false;
                bool deviceNew = false;

                if (Error.Success == result.ErrorCode)
                {
                    try
                    {
                        var dict = Json.Deserialize(result.Response) as Dictionary<string, object>;
                        if (dict.ContainsKey("error") && (Error.Success == (Error)Enum.ToObject(typeof(Error), dict["error"])))
                        {
                            if (dict.ContainsKey("data"))
                            {
                                var ret = dict["data"] as Dictionary<string, object>;

                                // NOTE: deviceId should only be set during the callback from init()
                                if ((null != ret) && ret.ContainsKey("deviceid") && isValidId(ret["deviceid"] as string))
                                {
                                    deviceNew = ret.ContainsKey("devicenew") ? (bool)ret["devicenew"] : false;

                                    DeviceId = ret["deviceid"] as string;

                                    if (null != DeviceId)
                                    {
                                        // Save it off
                                        Util.AddPref(DEVICEID_KEY_NAME, DeviceId);

                                        if (ret.ContainsKey("devicetuning"))
                                        {
                                            var deviceTuning = ret["devicetuning"] as IDictionary<string, object>;
                                            if (null != deviceTuning)
                                            {
                                                mTuningUpdater.onUpdate(Constants.ENTITY_TYPE_DEVICE, DeviceId, deviceTuning);
                                            }
                                        }
                                    }
                                }

                                // now handle the user id if there is one
                                if ((null != ret) && ret.ContainsKey("userid") && isValidId(ret["userid"] as string))
                                {
                                    userNew = ret.ContainsKey("usernew") ? (bool)ret["usernew"] : false;

                                    UserId = ret["userid"] as string;

                                    if (null != UserId)
                                    {
                                        if (ret.ContainsKey("usertuning"))
                                        {
                                            var userTuning = ret["usertuning"] as IDictionary<string, object>;
                                            if (null != userTuning)
                                            {
                                                mTuningUpdater.onUpdate(Constants.ENTITY_TYPE_USER, UserId, userTuning);
                                            }
                                        }

                                        if (!sRegisteredUsers.Contains(UserId))
                                        {
                                            sRegisteredUsers.Add(UserId);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            string desc = null;
                            if (dict.ContainsKey("description"))
                            {
                                desc = dict["description"] as string;
                            }
                            Util.logError(String.Format("Problem on initialization [{0}]", (null != desc) ? desc : "Unknown"));
                            retError = Error.Generic;
                        }
                    }
                    catch (Exception)
                    {
                        Util.logError("Exception during intialization: " + result.Response);
                        retError = Error.Generic;
                    }

                    mTuningUpdater.commit();
                }
                else
                {
                    // Request failure (likely a timeout), pass it through
                    Util.logError("Initialization call failed: code " + result.ErrorCode);
                    retError = result.ErrorCode;
                }


                // even if the init call failed, all is well as long as we AT LEAST have a device id
                if (isValidId(DeviceId))
                {
                    // If initialization is successful, we can initialize the EventDepot
                    EventDepot.init(Host, getQueryParms(), ReqTimeout);

                    Initialized = true;

                    // queue up some telemetry for the initial state...
                    if (null != mDeviceProperties)
                        new DataPointBuilder("datacollector_updateDeviceState").setArg(mDeviceProperties).send();
                    if (null != mUserProperties)
                        new DataPointBuilder("datacollector_updateUserState").setArg(mUserProperties).send();
                    if (deviceNew)
                        new DataPointBuilder("datacollector_newDevice").send();
                    if (userNew)
                        new DataPointBuilder("datacollector_newUser").send();

                    // TODO - decide if we want to send a TuningFailed error at this point, if there was some kind of error?

                    retError = Error.Success;
                }

                // Call the callback
                if (null != mCallback)
                {
                    mCallback(retError);
                }
            }
        }
        #endregion
    }
}

