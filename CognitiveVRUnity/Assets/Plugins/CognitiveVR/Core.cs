using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CognitiveVR 
{
    /// <summary>
    /// The most central pieces of the CognitiveVR Framework.
    /// </summary>
    public class Core
    {
        private const string SDK_NAME_PREFIX = "unity";
        private const string SDK_VERSION = "0.6.26";
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
                CoreSubsystem.init(customerId, userInfo.entityId, userInfo.Properties, deviceInfo.entityId, deviceInfo.Properties, SDK_VERSION, cb, HUB_OBJECT);
            }
            else if (null != cb)
            {
                // Some argument error, just call the callback immediately
                cb(error);
            }
        }
    }
}
