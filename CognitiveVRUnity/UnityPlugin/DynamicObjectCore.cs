using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

//TODO static place for receiving data from dynamic objects/engagements/etc and sending it

namespace CognitiveVR
{
    public static class DynamicObjectCore
    {
        private static int jsonPart = 1;
        //private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        //private static int currentSensorSnapshots = 0;

        static DynamicObjectCore()
        {
            Core.OnSendData += Core_OnSendData;
            Core.CheckSessionId();
        }

        public static void RecordDynamic(double timestamp, Vector3 position, Quaternion rotation)
        {
            /*if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(), value));
            }
            else
            {
                CachedSnapshots.Add(category, new List<string>());
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(), value));
            }
            currentSensorSnapshots++;
            if (currentSensorSnapshots >= CognitiveVR_Preferences.Instance.SensorSnapshotCount)
            {
                Core_OnSendData();
            }*/
        }

        public static void RecordDynamic(double timestamp, Vector3 position, Quaternion rotation,Dictionary<string,object> properties)
        {
            /*if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(), value));
            }
            else
            {
                CachedSnapshots.Add(category, new List<string>());
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(), value));
            }
            currentSensorSnapshots++;
            if (currentSensorSnapshots >= CognitiveVR_Preferences.Instance.SensorSnapshotCount)
            {
                Core_OnSendData();
            }*/
        }

        private static void Core_OnSendData()
        {
            /*if (CachedSnapshots.Keys.Count <= 0) { CognitiveVR.Util.logDebug("Sensor.SendData found no data"); return; }

            var sceneSettings = CognitiveVR_Preferences.FindTrackingScene();
            if (sceneSettings == null) { CognitiveVR.Util.logDebug("Sensor.SendData found no SceneKeySettings"); return; }

            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("name", Core.UniqueID, sb);
            sb.Append(",");
            JsonUtil.SetString("sessionid", Core.SessionID, sb);
            sb.Append(",");
            JsonUtil.SetDouble("timestamp", (int)Core.SessionTimeStamp, sb);
            sb.Append(",");
            JsonUtil.SetInt("part", jsonPart, sb);
            sb.Append(",");
            jsonPart++;

            sb.Append("\"data\":[");
            foreach (var k in CachedSnapshots.Keys)
            {
                sb.Append("{");
                JsonUtil.SetString("name", k, sb);
                sb.Append(",");
                sb.Append("\"data\":[");
                foreach (var v in CachedSnapshots[k])
                {
                    sb.Append(v);
                    sb.Append(",");
                }
                if (CachedSnapshots.Values.Count > 0)
                    sb.Remove(sb.Length - 1, 1); //remove last comma from data array
                sb.Append("]");
                sb.Append("}");
                sb.Append(",");
            }
            if (CachedSnapshots.Keys.Count > 0)
            {
                sb.Remove(sb.Length - 1, 1); //remove last comma from sensor object
            }
            sb.Append("]}");

            CachedSnapshots.Clear();
            currentSensorSnapshots = 0;

            string url = Constants.POSTSENSORDATA(sceneSettings.SceneId, sceneSettings.VersionNumber);
            byte[] outBytes = new System.Text.UTF8Encoding(true).GetBytes(sb.ToString());
            NetworkManager.Post(url, outBytes);*/
        }
    }
}