using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

//TODO merge record sensor overrides together

namespace CognitiveVR
{
    public static class SensorRecorder
    {
        public class SensorData
        {
            public string Name;
            public string Rate;
            public float NextRecordTime;
            public float UpdateInterval;

            public SensorData(string name, float rate)
            {
                Name = name;
                Rate = rate.ToString("{0:0.000}");
                UpdateInterval = 1 / rate;
            }
        }

        static Dictionary<string, SensorData> sensorData = new Dictionary<string, SensorData>();

        private static int jsonPart = 1;
        private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        private static int currentSensorSnapshots = 0;
        public static int CachedSensors { get { return currentSensorSnapshots; } }

        //holds the latest value of each sensor type. can be appended to custom events
        //TODO merge LastSensorValues into sensorData collection
        public static Dictionary<string, float> LastSensorValues = new Dictionary<string, float>();

        static SensorRecorder()
        {

        }

        internal static void Initialize()
        {
            Core.OnSendData -= Core_OnSendData;
            Core.OnSendData += Core_OnSendData;
            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.SensorSnapshotMaxTimer;
            if (automaticTimerActive == false)
            {
                automaticTimerActive = true;
                Core.NetworkManager.StartCoroutine(AutomaticSendTimer());
            }
        }

        /// <summary>
        /// optional method to declare a sensor with a custom rate
        /// </summary>
        /// <param name="sensorName"></param>
        /// <param name="HzRate"></param>
        /// <param name="initialValue"></param>
        public static void InitializeSensor(string sensorName, float HzRate = 10, float initialValue = 0)
        {
            if (sensorData.ContainsKey(sensorName))
            {
                return;
            }
            sensorData.Add(sensorName, new SensorData(sensorName, HzRate));
            CachedSnapshots.Add(sensorName, new List<string>(512));
            LastSensorValues.Add(sensorName, initialValue);
            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(sensorName, initialValue);
        }

        static bool automaticTimerActive = false;
        static float nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            while (true)
            {
                while (nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.SensorSnapshotMaxTimer;
                if (!Core.IsInitialized)
                {
                    if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                        Util.logDevelopment("check to automatically send sensors");
                    Core_OnSendData(false);
                }
            }
        }

        public static void RecordDataPoint(string category, float value)
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Sensor recorded without SceneId"); return; }

            //check next valid write time
            if (sensorData.ContainsKey(category))
            {
                if (Time.realtimeSinceStartup < sensorData[category].NextRecordTime)
                {
                    //recording too fast!
                    return;
                }
            }
            else
            {
                InitializeSensor(category, 10, value);
            }

            //update internal values and record data
            sensorData[category].NextRecordTime = Time.realtimeSinceStartup + sensorData[category].UpdateInterval;
            CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            LastSensorValues[category] = value;

            currentSensorSnapshots++;
            if (currentSensorSnapshots >= CognitiveVR_Preferences.Instance.SensorSnapshotCount)
            {
                TrySendData();
            }
        }

        ///doubles are recorded raw, but cast to float for Active Session View
        public static void RecordDataPoint(string category, double value)
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Sensor recorded without SceneId"); return; }

            //check next valid write time
            if (sensorData.ContainsKey(category))
            {
                if (Time.realtimeSinceStartup < sensorData[category].NextRecordTime)
                {
                    //recording too fast!
                    return;
                }
            }
            else
            {
                InitializeSensor(category, 10, (float)value);
            }

            //update internal values and record data
            sensorData[category].NextRecordTime = Time.realtimeSinceStartup + sensorData[category].UpdateInterval;
            CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            LastSensorValues[category] = (float)value;

            currentSensorSnapshots++;
            if (currentSensorSnapshots >= CognitiveVR_Preferences.Instance.SensorSnapshotCount)
            {
                TrySendData();
            }
        }

        public static void RecordDataPoint(string category, float value, double unixTimestamp)
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Sensor recorded without SceneId"); return; }

            //check next valid write time
            if (sensorData.ContainsKey(category))
            {
                if (unixTimestamp < sensorData[category].NextRecordTime)
                {
                    //recording too fast!
                    return;
                }
            }
            else
            {
                InitializeSensor(category, 10, value);
            }

            //update internal values and record data
            sensorData[category].NextRecordTime = (float)(unixTimestamp + sensorData[category].UpdateInterval);
            CachedSnapshots[category].Add(GetSensorDataToString(unixTimestamp, value));
            LastSensorValues[category] = value;

            currentSensorSnapshots++;
            if (currentSensorSnapshots >= CognitiveVR_Preferences.Instance.SensorSnapshotCount)
            {
                TrySendData();
            }
        }

        static void TrySendData()
        {
            if (!Core.IsInitialized) { return; }
            bool withinMinTimer = lastSendTime + CognitiveVR_Preferences.Instance.SensorSnapshotMinTimer > Time.realtimeSinceStartup;
            bool withinExtremeBatchSize = currentSensorSnapshots < CognitiveVR_Preferences.Instance.SensorExtremeSnapshotCount;

            //within last send interval and less than extreme count
            if (withinMinTimer && withinExtremeBatchSize)
            {
                return;
            }
            Core_OnSendData(false);
        }

        public delegate void onNewSensorRecorded(string sensorName, float sensorValue);
        public static event onNewSensorRecorded OnNewSensorRecorded;

        //happens after the network has sent the request, before any response
        public static event Core.onDataSend OnSensorSend;

        static float lastSendTime = -60;
        private static void Core_OnSendData(bool copyDataToCache)
        {
            if (CachedSnapshots.Keys.Count <= 0) { return; }

            if (Core.TrackingScene == null)
            {
                CognitiveVR.Util.logDebug("Sensor.SendData could not find scene settings for scene! do not upload sensors to sceneexplorer");
                CachedSnapshots.Clear();
                currentSensorSnapshots = 0;
                return;
            }

            if (!Core.IsInitialized)
            {
                return;
            }


            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.SensorSnapshotMaxTimer;
            lastSendTime = Time.realtimeSinceStartup;


            StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("name", Core.DeviceId, sb);
            sb.Append(",");

            if (!string.IsNullOrEmpty(Core.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Core.LobbyId, sb);
                sb.Append(",");
            }

            JsonUtil.SetString("sessionid", Core.SessionID, sb);
            sb.Append(",");
            JsonUtil.SetInt("timestamp", (int)Core.SessionTimeStamp, sb);
            sb.Append(",");
            JsonUtil.SetInt("part", jsonPart, sb);
            sb.Append(",");
            jsonPart++;
            JsonUtil.SetString("formatversion", "2.0", sb);
            sb.Append(",");


            sb.Append("\"data\":[");
            foreach (var k in CachedSnapshots.Keys)
            {
                sb.Append("{");
                JsonUtil.SetString("name", k, sb);
                sb.Append(",");
                if (sensorData.ContainsKey(k))
                {
                    JsonUtil.SetString("sensorHzLimitType", sensorData[k].Rate, sb);
                    sb.Append(",");
                    if (sensorData[k].UpdateInterval <= 0.1f)
                    {
                        JsonUtil.SetString("sensorHzLimited", "true", sb);
                        sb.Append(",");
                    }
                }
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

            string url = CognitiveStatics.POSTSENSORDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);
            string content = sb.ToString();

            if (copyDataToCache)
            {
                if (Core.NetworkManager.runtimeCache != null && Core.NetworkManager.runtimeCache.CanWrite(url, content))
                {
                    Core.NetworkManager.runtimeCache.WriteContent(url, content);
                }
            }

            Core.NetworkManager.Post(url, content);
            if (OnSensorSend != null)
            {
                OnSensorSend.Invoke();
            }
        }

        #region json

        static StringBuilder sbdatapoint = new StringBuilder(256);
        //put this into the list of saved sensor data based on the name of the sensor
        private static string GetSensorDataToString(double timestamp, double sensorvalue)
        {
            //TODO test if string concatenation is just faster/less garbage

            sbdatapoint.Length = 0;

            sbdatapoint.Append("[");
            sbdatapoint.ConcatDouble(timestamp);
            //sbdatapoint.Append(timestamp);
            sbdatapoint.Append(",");
            sbdatapoint.ConcatDouble(sensorvalue);
            //sbdatapoint.Append(sensorvalue);
            sbdatapoint.Append("]");

            return sbdatapoint.ToString();
        }

        #endregion
    }
}