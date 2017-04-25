using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using CognitiveVR.External.MiniJSON;

namespace CognitiveVR 
{
    /// <summary>
    /// The most central pieces of the CognitiveVR Framework.
    /// </summary>
    public class Core
    {
        private const string SDK_NAME_PREFIX = "unity";
        private const string SDK_VERSION = "0.6.2";
        public static string SDK_Version { get { return SDK_VERSION; } }
        internal const string HUB_OBJECT = "CognitiveVR_Manager";

        /// <summary>
        /// Gets the registered id for the currently active user
        /// </summary>
        /// <value>The user id</value>
        public static string userId
        {
            get
            {
                return CoreSubsystem.UserId;
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
                return CoreSubsystem.DeviceId;
            }
        }

        /// <summary>
        /// returns userID. if userID is empty, returns deviceID
        /// </summary>
        public static string UniqueID
        {
            get
            {
                return CoreSubsystem.UniqueID;
            }
        }

        /// <summary>
        /// Initializes CognitiveVR Framework for use, including instrumentation and tuning.
        /// </summary>
        /// <param name="initParams">Initialization parameters</param>
        /// <param name="cb">Application defined callback which will occur on completion</param>
        public static void init(InitParams initParams, Callback cb)
        {
            Debug.Log("CognitivrVR.Core.init()");
            // this should only be enabled during android development!!!
            //AndroidJNIHelper.debug = true;

            Error error = Error.Success;

            // Enable/disable logging
            Util.setLogEnabled(initParams.logEnabled);

            if (null == initParams)
            {
                Util.logError("No init parameters provided");
                error = Error.InvalidArgs;
            }
            else if (null == cb)
            {
                Util.logError("Please provide a valid CognitiveVR.Callback");
                error = Error.InvalidArgs;
            }
            else if (Constants.ENTITY_TYPE_USER != initParams.userInfo.type)
            {
                Util.logError("To provide intitial user settings, be sure to use createUserInfo");
                error = Error.InvalidArgs;
            }
            else if (Constants.ENTITY_TYPE_DEVICE != initParams.deviceInfo.type)
            {
                Util.logError("To provide intitial device settings, be sure to use createDeviceInfo");
                error = Error.InvalidArgs;
            }

            GameObject go = GameObject.Find(HUB_OBJECT);
            if (null == go) go = new GameObject(HUB_OBJECT);
            GameObject.DontDestroyOnLoad(go);

            if (Error.Success == error)
            {
                InstrumentationSubsystem.init();

                // Builds targeting the web player need to be handled specially due to the security model
                // Unfortunately, there is no good way to determine that at run time within the plugin.
                #if UNITY_WEBPLAYER
				const bool isWebPlayer = true;
                #else
                const bool isWebPlayer = false;
                #endif

                TuningSubsystem.init(delegate (Error err)
                {
                    CoreSubsystem.init(initParams.customerId, new TuningSubsystem.Updater(), initParams.userInfo.entityId, initParams.userInfo.properties, initParams.deviceInfo.entityId, initParams.deviceInfo.properties, initParams.requestTimeout, initParams.host, initParams.logEnabled, SDK_NAME_PREFIX, SDK_VERSION, cb, HUB_OBJECT, isWebPlayer);
                });
            }
            else if (null != cb)
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
        }

        /// <summary>
        /// Register a user with CognitiveVR and make them the currently active user.  This can be done at any point when a new user is interacted with by 
        /// the application. Note that if the active user is known at startup, it is generally ideal to provide their info directly to CognitiveVR.Core.init instead
        /// </summary>
        /// <param name="userInfo">An EntityInfo created with InitParams.createUserInfo</param>
        /// <param name="cb">Application defined callback which will occur on completion</param>
        public static void registerUser(EntityInfo userInfo, Callback cb)
        {
            Error error = Error.Success;

            if (null == cb)
            {
                Util.logError("Please provide a valid CognitiveVR.Callback");
                error = Error.InvalidArgs;
            }
            else if (Constants.ENTITY_TYPE_USER != userInfo.type)
            {
                Util.logError("To provide user settings, be sure to use createUserInfo");
                error = Error.InvalidArgs;
            }

            if (Error.Success == error)
            {
                //#if (UNITY_IPHONE || UNITY_ANDROID) && !UNITY_EDITOR
                //string userProperties = Json.Serialize(userInfo.properties);

                //				registerCallback("onCognitiveVRRegisterUserComplete", cb);
                //#endif

                //#if UNITY_IPHONE && !UNITY_EDITOR
                //cognitivevr_core_registeruser(userInfo.entityId, userProperties, HUB_OBJECT);
                //#elif UNITY_ANDROID && !UNITY_EDITOR
                //callNativeAsync("cognitivevr_core_registeruser", new object[] {userInfo.entityId, userProperties, HUB_OBJECT});
                //				#else
                CoreSubsystem.registerUser(userInfo.entityId, userInfo.properties, new TuningSubsystem.Updater(), cb);
                //#endif
            }
            else if (null != cb)
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
        }

        /// <summary>
        /// Explicitly sets the active user id. Generally only required when multiple concurrent users are required/supported, since
        /// init() and registerUser() both activate the provided user by default.
        /// </summary>
        /// <returns>An error code</returns>
        /// <param name="userId">The user id, which had been previously registered with CognitiveVR.Core.registerUser</param>
        public static Error setActiveUser(string userId)
        {
            return CoreSubsystem.setActiveUser(userId);
        }

        /// <summary>
        /// Useful when the logged in user logs out of the application.  Clearing the active user allows CognitiveVR to provide non user-specific 
        /// tuning values and report telemetry which is not linked to a user
        /// </summary>
        /// <returns>An error code</returns>
        public static Error clearActiveUser()
        {
            return CoreSubsystem.setActiveUser(null);
        }

        /// <summary>
        /// Pause CognitiveVR.  This causes CognitiveVR to save off its state to Internal Storage and stop checking for events to send.
        /// One would typically call this whenever the application is sent to the background.
        /// 
        /// <b>Note:</b> On some platforms, one can still make calls to CognitiveVR functions even when it's paused, but doing so will trigger 
        /// reads and writes to Internal Storage, so it should be done judiciously
        /// </summary>
        [System.Obsolete("Core.pause() is no longer used")]
        public static void pause()
        {
            //CoreSubsystem.pause();
        }

        /// <summary>
        /// Resume CognitiveVR.  This causes CognitiveVR read its last known state from Internal Storage and restart polling for events to send.
        /// One would typically call this whenever the application is brought to the foreground.
        /// </summary>
        [System.Obsolete("Core.resume() is no longer used")]
        public static void resume()
        {
            //CoreSubsystem.resume();
        }
    }
}
