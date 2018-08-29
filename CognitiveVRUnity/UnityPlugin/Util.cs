using UnityEngine;
using System;
using System.Text;
using System.Collections.Generic;
using CognitiveVR.External;

namespace CognitiveVR
{
	public static class Util
	{
        public static Vector3 vector_zero = new Vector3(0, 0, 0);
        public static Vector3 vector_forward = new Vector3(0, 0, 1);


        private const string LOG_TAG = "[COGNITIVE3D] ";
		
		static IDictionary<string, object> sDeviceAndAppInfo = new Dictionary<string, object>();
        public static IDictionary<string, object> GetDeviceProperties() { return sDeviceAndAppInfo; }
        internal static string getSDKName(string namePrefix) { return namePrefix; }

        //private static HashSet<string> sValidCurrencyCodes = new HashSet<string>();
        //private static IDictionary<string, HashSet<string>> sCurrencyCodesBySymbol = new Dictionary<string, HashSet<string>>();

        public static void logWarning(string msg, UnityEngine.Object context = null)
        {
            if (CognitiveVR_Preferences.Instance.EnableLogging)
            {
                if (context != null)
                    Debug.LogWarning(LOG_TAG + msg, context);
                else
                    Debug.LogWarning(LOG_TAG + msg);
            }
        }

        // Internal logging.  These can be enabled by calling Util.setLogEnabled(true)
        public static void logDebug(string msg)
		{
			if (CognitiveVR_Preferences.Instance.EnableLogging)
			{
				Debug.Log(LOG_TAG + msg);
			}
		}
		
		public static void logError(Exception e)
		{
			if (CognitiveVR_Preferences.Instance.EnableLogging)
			{
				Debug.LogException(e);
			}
		}
		
		public static void logError(string msg)
		{
			if (CognitiveVR_Preferences.Instance.EnableLogging)
			{
				Debug.LogError(LOG_TAG + msg);
			}
		}

        static int uniqueId;
        /// <summary>
        /// this should be used instead of System.Guid.NewGuid(). these only need to be unique, not complicated
        /// </summary>
        /// <returns></returns>
        public static string GetUniqueId()
        {
            return uniqueId++.ToString();
        }

        internal static void cacheDeviceAndAppInfo()
        {
            // Clear out any previously set data
            sDeviceAndAppInfo.Clear();

            // Get the rest of the information about the device
            sDeviceAndAppInfo.Add("cvr.app.name", Application.productName);
            sDeviceAndAppInfo.Add("cvr.app.version", Application.version);
            sDeviceAndAppInfo.Add("cvr.unity.version", Application.unityVersion);
            sDeviceAndAppInfo.Add("cvr.device.model", SystemInfo.deviceModel);
            sDeviceAndAppInfo.Add("cvr.device.type", SystemInfo.deviceType.ToString());
            sDeviceAndAppInfo.Add("cvr.device.platform", Application.platform.ToString());
            sDeviceAndAppInfo.Add("cvr.device.os", SystemInfo.operatingSystem);
            sDeviceAndAppInfo.Add("cvr.device.graphics.name", SystemInfo.graphicsDeviceName);
            sDeviceAndAppInfo.Add("cvr.device.graphics.type", SystemInfo.graphicsDeviceType.ToString());
            sDeviceAndAppInfo.Add("cvr.device.graphics.vendor", SystemInfo.graphicsDeviceVendor);
            sDeviceAndAppInfo.Add("cvr.device.graphics.version", SystemInfo.graphicsDeviceVersion);
            sDeviceAndAppInfo.Add("cvr.device.graphics.memory", SystemInfo.graphicsMemorySize);
            sDeviceAndAppInfo.Add("cvr.device.processor", SystemInfo.processorType);
            sDeviceAndAppInfo.Add("cvr.device.memory", SystemInfo.systemMemorySize);
        }

        static string _hmdname;
        //returns vive/rift/gear/unknown based on hmd model name
        public static string GetSimpleHMDName(string rawHMDName)
        {
            if (_hmdname != null)
            {
                return _hmdname;
            }
            if (rawHMDName.Contains("vive mv") || rawHMDName.Contains("vive. mv") || rawHMDName.Contains("vive dvt")){ _hmdname = "vive"; return _hmdname; }
            if (rawHMDName.Contains("rift cv1")) { _hmdname = "rift"; return _hmdname; }
            if (rawHMDName.Contains("galaxy note 4") || rawHMDName.Contains("galaxy note 5") || rawHMDName.Contains("galaxy s6")) { _hmdname = "gear"; return _hmdname; }

            _hmdname = "unknown";
            return _hmdname;
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static double lastTime;
        static int lastFrame = -1;

        public static double Timestamp(int frame)
        {
            if (frame == lastFrame)
                return lastTime;
            TimeSpan span = DateTime.UtcNow - epoch;
            lastFrame = frame;
            lastTime = span.TotalSeconds;
            return span.TotalSeconds;
        }

        /// <summary>
		/// Get the Unix timestamp
		/// </summary>
		public static double Timestamp()
		{
			TimeSpan span = DateTime.UtcNow - epoch;
			return span.TotalSeconds;
		}

        internal static void AddPref(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        internal static bool TryGetPrefValue(string key, out string value)
        {
            bool keyFound = false;
            value = default(string);
            if (PlayerPrefs.HasKey(key))
            {
                keyFound = true;
                value = PlayerPrefs.GetString(key);
            }

            return keyFound;
        }

        public static int GetResponseCode(Dictionary<string, string> headers)
        {
            if (headers == null) { return 0; }
            //HTTP/1.1 200 OK
            int returnCode = -1;
            string statusvalue = null;
            if (headers.TryGetValue("STATUS",out statusvalue))
            {
                if (int.TryParse(statusvalue.Substring(9, 3),out returnCode))
                {
                    return returnCode;
                }
            }
            return returnCode;
        }
    }

    public static class JsonUtil
    {
        //https://forum.unity3d.com/threads/how-to-load-an-array-with-jsonutility.375735/
        public static T[] GetJsonArray<T>(string json)
        {
            string newJson = "{\"array\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        //used for serializing object manifest data
        [Serializable]
        private class Wrapper<T>
        {
#pragma warning disable 0649
            public T[] array;
#pragma warning restore 0649
        }

        /// <returns>"name":["obj","obj","obj"]</returns>
        public static StringBuilder SetListString(string name, List<string> list, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append("\"");
                builder.Append(list[i]);
                builder.Append("\"");
                builder.Append(",");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[obj,obj,obj]</returns>
        public static StringBuilder SetListObject<T>(string name, List<T> list, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":{");
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append(list[i].ToString());
                builder.Append(",");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.Append("}");
            return builder;
        }

        /// <returns>"name":"stringval"</returns>
        public static StringBuilder SetString(string name, string stringValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");

            builder.Append("\"");
            builder.Append(stringValue);
            builder.Append("\"");

            return builder;
        }
        
        /// <returns>"name":"intValue"</returns>
        public static StringBuilder SetInt(string name, int intValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");

            builder.Append("\"");
            builder.Concat(intValue);
            //builder.Append(intValue);
            builder.Append("\"");

            return builder;
        }

        /// <returns>"name":"floatValue"</returns>
        public static StringBuilder SetFloat(string name, float floatValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");

            builder.Append("\"");
            builder.Concat(floatValue);
            //builder.Append(floatValue);
            builder.Append("\"");

            return builder;
        }

        /// <returns>"name":"doubleValue"</returns>
        public static StringBuilder SetDouble(string name, double doubleValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");

            builder.Append("\"");
            builder.ConcatDouble(doubleValue);
            //builder.Append(doubleValue);
            builder.Append("\"");

            return builder;
        }

        /// <returns>"name":objectValue.ToString()</returns>
        public static StringBuilder SetObject(string name, object objectValue, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":");

            if (objectValue.GetType() == typeof(bool))
                builder.Append(objectValue.ToString().ToLower());
            else
                builder.Append(objectValue.ToString());

            return builder;
        }
        
        /// <returns>"name":[0.1,0.2,0.3]</returns>
        public static StringBuilder SetVector(string name, float[] pos, StringBuilder builder, bool centimeterLimit = false)
        {
            if (pos.Length < 3) { pos = new float[3] { 0, 0, 0 }; }

            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            if (centimeterLimit)
            {
                builder.Append(string.Format("{0:0.00}", pos[0]));

                builder.Append(",");
                builder.Append(string.Format("{0:0.00}", pos[1]));

                builder.Append(",");
                builder.Append(string.Format("{0:0.00}", pos[2]));

            }
            else
            {
                builder.Concat(pos[0]);
                //builder.Append(pos[0]);
                builder.Append(",");
                builder.Concat(pos[1]);
                //builder.Append(pos[1]);
                builder.Append(",");
                builder.Concat(pos[2]);
                //builder.Append(pos[2]);
            }

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3]</returns>
        public static StringBuilder SetVector(string name, Vector3 pos, StringBuilder builder, bool centimeterLimit = false)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            if (centimeterLimit)
            {
                builder.Append(string.Format("{0:0.00}", pos.x));

                builder.Append(",");
                builder.Append(string.Format("{0:0.00}", pos.y));

                builder.Append(",");
                builder.Append(string.Format("{0:0.00}", pos.z));

            }
            else
            {
                builder.Concat(pos.x);
                //builder.Append(pos.x);
                builder.Append(",");
                builder.Concat(pos.y);
                //builder.Append(pos.y);
                builder.Append(",");
                builder.Concat(pos.z);
                //builder.Append(pos.z);
            }

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2</returns>
        public static StringBuilder SetVector2(string name, Vector2 pos, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(pos.x);
            //builder.Append(pos.x);
            builder.Append(",");
            builder.Concat(pos.y);
            //builder.Append(pos.y);

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3,0.4]</returns>
        public static StringBuilder SetQuat(string name, Quaternion quat, StringBuilder builder)
        {
            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(quat.x);
            //builder.Append(quat.x);
            builder.Append(",");
            builder.Concat(quat.y);
            //builder.Append(quat.y);
            builder.Append(",");
            builder.Concat(quat.z);
            //builder.Append(quat.z);
            builder.Append(",");
            builder.Concat(quat.w);
            //builder.Append(quat.w);

            builder.Append("]");
            return builder;
        }

        /// <returns>"name":[0.1,0.2,0.3,0.4]</returns>
        public static StringBuilder SetQuat(string name, float[] quat, StringBuilder builder)
        {
            if (quat.Length < 4) { quat = new float[4] { 0, 0, 0, 0 }; }

            builder.Append("\"");
            builder.Append(name);
            builder.Append("\":[");

            builder.Concat(quat[0]);
            //builder.Append(quat[0]);
            builder.Append(",");
            builder.Concat(quat[1]);
            //builder.Append(quat[1]);
            builder.Append(",");
            builder.Concat(quat[2]);
            //builder.Append(quat[2]);
            builder.Append(",");
            builder.Concat(quat[3]);
            //builder.Append(quat[3]);

            builder.Append("]");
            return builder;
        }
    }
}