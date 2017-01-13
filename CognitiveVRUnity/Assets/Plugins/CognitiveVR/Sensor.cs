using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CognitiveVR.Plugins
{
    public class SensorSnapshot
    {
        public double timestamp;
        public double sensorValue;

        public SensorSnapshot(double time, double value)
        {
            timestamp = time;
            sensorValue = value;
        }
    }

    /// <summary>
    /// This CognitiveVR plugin provides a simple interface for instrumenting sensor in an application.
    /// </summary>
    public static class Sensor
	{
        static Dictionary<string,List<SensorSnapshot>> CachedSnapshots = new Dictionary<string, List<SensorSnapshot>>();

        public static void RecordDataPoint(string category, float value)
        {
            if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(new SensorSnapshot(Util.Timestamp(), value));
            }
            else
            {
                CachedSnapshots.Add(category,new List<SensorSnapshot>());
                CachedSnapshots[category].Add(new SensorSnapshot(Util.Timestamp(), value));
            }
        }

        public static void SendData()
        {
            if (CachedSnapshots.Keys.Count <= 0) { CognitiveVR.Util.logDebug("Sensor.SendData found no data"); return; }

            //var sceneSettings = CognitiveVR_Preferences.Instance.FindScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            //if (sceneSettings == null) { CognitiveVR.Util.logDebug("Sensor.SendData found no SceneKeySettings"); return; }

            byte[] serializedData = SerializeSensorData();
            //www send
            string sceneURLSensors = "https://sceneexplorer.com/api/sessions/" + CognitiveVR_Preferences.SessionID;
            SendRequest(serializedData, sceneURLSensors);

            //clear sensor data list
            CachedSnapshots = new Dictionary<string, List<SensorSnapshot>>();
        }

        private static void SendRequest(byte[] bytes, string url)
        {
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/json");
            headers.Add("X-HTTP-Method-Override", "POST");

            var www = new UnityEngine.WWW(url, bytes, headers);
            //because this is not a monobehaviour, this cannot hold a coroutine and get a response
        }

        #region json

        static byte[] SerializeSensorData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\"userid\": \"");
            sb.Append(Core.userId);
            sb.Append("\",\"sessionid\": \"");
            sb.Append(CognitiveVR_Preferences.SessionID);
            sb.Append("\",\"timestamp\": \"");
            sb.Append(CognitiveVR_Preferences.TimeStamp);
            sb.Append("\",\"data\":[");
            
            foreach (var kvp in CachedSnapshots)
            {
                AppendSensorData(kvp,sb);
            }

            sb.Remove(sb.Length - 1, 1); //remove the last comma
            sb.Append("]}");

            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(sb.ToString());
            return outBytes;
        }

        static void AppendSensorData(KeyValuePair<string,List<SensorSnapshot>> kvp, StringBuilder sb)
        {
            sb.Append("{\"name\":\"");
            sb.Append(kvp.Key);
            sb.Append("\",\"data\":[");
            
            //loop through list
            for (int i = 0; i<kvp.Value.Count; i++)
            {
                sb.Append("[");
                sb.Append(kvp.Value[i].timestamp);
                sb.Append(",");
                sb.Append(kvp.Value[i].sensorValue);
                sb.Append("],");
            }

            sb.Remove(sb.Length - 1, 1); //remove the last comma
            sb.Append("]},");
        }

        #endregion
    }
}

