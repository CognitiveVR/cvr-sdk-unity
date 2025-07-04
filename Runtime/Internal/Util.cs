using UnityEngine;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Cognitive3D
{
	internal static class Util
	{
        private const string LOG_TAG = "[COGNITIVE3D] ";

        internal static void logWarning(string msg, UnityEngine.Object context = null)
        {
            if (Cognitive3D_Preferences.Instance.EnableLogging)
            {
                if (context != null)
                    Debug.LogWarning(LOG_TAG + msg, context);
                else
                    Debug.LogWarning(LOG_TAG + msg);
            }
        }

        // Internal logging.  These can be enabled by calling Util.setLogEnabled(true)
        internal static void logDebug(string msg)
		{
			if (Cognitive3D_Preferences.Instance.EnableLogging)
			{
				Debug.Log(LOG_TAG + msg);
			}
		}

        // Internal logging.  These can be enabled by calling Util.setLogEnabled(true)
        internal static void logDevelopment(string msg)
        {
            if (Cognitive3D_Preferences.Instance.EnableDevLogging)
            {
                Debug.Log("[COGNITIVE3D DEV] "+ msg);
            }
        }

        internal static void logError(Exception e)
		{
			if (Cognitive3D_Preferences.Instance.EnableLogging)
			{
				Debug.LogException(e);
			}
		}

        internal static void logError(string msg)
		{
			if (Cognitive3D_Preferences.Instance.EnableLogging)
			{
				Debug.LogError(LOG_TAG + msg);
			}
		}

        private static HashSet<string> logs = new HashSet<string>();

        /// <summary>
        /// Logs a message once, preventing duplicate logging of the same message
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="logType">The type of log: Error, Warning, or Info</param>
        internal static void LogOnce(string msg, LogType logType)
        {
            if (Cognitive3D_Preferences.Instance.EnableLogging)
			{
                if (!logs.Contains(msg))
                {
                    switch(logType)
                    {
                        case LogType.Error:
                            Debug.LogError(LOG_TAG + msg);
                            break;
                        case LogType.Warning:
                            Debug.LogWarning(LOG_TAG + msg);
                            break;
                        default:
                            Debug.Log(LOG_TAG + msg);
                            break;
                    }
                    logs.Add(msg);
                }
            }
        }

        /// <summary>
        /// Clears the logs, allowing messages to be logged again
        /// </summary>
        internal static void ResetLogs()
        {
            logs.Clear();
        }

        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static double lastTime;
        static int lastFrame = -1;

        //timestamp can be cached per frame, reducing number of timespan calculations
        internal static double Timestamp(int frame)
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
		internal static double Timestamp()
		{
			TimeSpan span = DateTime.UtcNow - epoch;
			return span.TotalSeconds;
		}

        /// <summary>
        /// Extracts the Unix timestamp from a line and return it as a string.
        /// </summary>
        /// <param name="line">The line from which to extract the Unix timestamp.</param>
        /// <return>The extracted Unix timestamp as a string, if successful; otherwise, returns current unix time</return>
        internal static string ExtractUnixTime(string line)
        {
            // Regular expression pattern to match the Unix timestamp with fractional seconds
            string pattern = @"^\s*(\d+\.\d+)";

            // Match the pattern in the log line
            Match match = Regex.Match(line, pattern);

            if (match.Success)
            {
                // Contains the first capturing group (\d+\.\d+)
                return match.Groups[1].Value;
            }

            return Timestamp().ToString();
        }

        //https://forum.unity3d.com/threads/how-to-load-an-array-with-jsonutility.375735/
        internal static T[] GetJsonArray<T>(string json)
        {
            string newJson = "{\"array\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        //used for serializing object manifest data
        [Serializable]
        internal class Wrapper<T>
        {
#pragma warning disable 0649
            public T[] array;
#pragma warning restore 0649
        }
    }
}