using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Cognitive3D;
using System.Text;

//TODO merge record sensor overrides together

namespace Cognitive3D
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
                Rate = string.Format("{0:0.00}", rate);
                if (rate == 0)
                {
                    UpdateInterval = 1 / 10;
                    Util.logWarning("Initializing Sensor " + name + " at 0 hz! Defaulting to 10hz");
                }
                else
                {
                    UpdateInterval = 1 / rate;
                }
            }
        }

        static Dictionary<string, SensorData> sensorData = new Dictionary<string, SensorData>();

        //private static int jsonPart = 1;
        //private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        //private static int currentSensorSnapshots = 0;
        //public static int CachedSensors { get { return currentSensorSnapshots; } }

        //holds the latest value of each sensor type. can be appended to custom events
        //TODO merge LastSensorValues into sensorData collection
        public static Dictionary<string, float> LastSensorValues = new Dictionary<string, float>();

        static SensorRecorder()
        {

        }

        //called each time a session starts
        internal static void Initialize()
        {
            Cognitive3D_Manager.OnPostSessionEnd += Core_OnPostSessionEnd;
            //Cognitive3D_Manager.OnSendData -= Core_OnSendData;
            //Cognitive3D_Manager.OnSendData += Core_OnSendData;
            //nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.SensorSnapshotMaxTimer;
            //if (automaticTimerActive == false)
            //{
            //    automaticTimerActive = true;
            //    Cognitive3D_Manager.NetworkManager.StartCoroutine(AutomaticSendTimer());
            //}
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
            //CachedSnapshots.Add(sensorName, new List<string>(512));
            LastSensorValues.Add(sensorName, initialValue);
            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(sensorName, initialValue);
        }

        static bool automaticTimerActive = false;
        static float nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            while (Cognitive3D_Manager.IsInitialized)
            {
                while (nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.SensorSnapshotMaxTimer;
                if (!Cognitive3D_Manager.IsInitialized)
                {
                    if (Cognitive3D_Preferences.Instance.EnableDevLogging)
                        Util.logDevelopment("check to automatically send sensors");
                    Core_OnSendData(false);
                }
            }
        }

        public static void RecordDataPoint(string category, float value)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Sensor recorded without SceneId"); return; }

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
            //CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            LastSensorValues[category] = value;

            CoreInterface.RecordSensor(category, value, Util.Timestamp(Time.frameCount));

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(category, value);

            //currentSensorSnapshots++;
            //if (currentSensorSnapshots >= Cognitive3D_Preferences.Instance.SensorSnapshotCount)
            //{
                //TrySendData();
            //}
        }

        ///doubles are recorded raw, but cast to float for Active Session View
        public static void RecordDataPoint(string category, double value)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Sensor recorded without SceneId"); return; }

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
            //CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            LastSensorValues[category] = (float)value;

            CoreInterface.RecordSensor(category, (float)value, Util.Timestamp(Time.frameCount));

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(category, (float)value);

            //currentSensorSnapshots++;
            //if (currentSensorSnapshots >= Cognitive3D_Preferences.Instance.SensorSnapshotCount)
            //{
                //TrySendData();
            //}
        }

        public static void RecordDataPoint(string category, float value, double unixTimestamp)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Sensor recorded without SceneId"); return; }

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
            //CachedSnapshots[category].Add(GetSensorDataToString(unixTimestamp, value));
            LastSensorValues[category] = value;

            CoreInterface.RecordSensor(category, (float)value, unixTimestamp);

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(category, value);

            //currentSensorSnapshots++;
            //if (currentSensorSnapshots >= Cognitive3D_Preferences.Instance.SensorSnapshotCount)
            //{
            //    TrySendData();
            //}
        }

        static void TrySendData()
        {
            //if (!Core.IsInitialized) { return; }
            //bool withinMinTimer = lastSendTime + Cognitive3D_Preferences.Instance.SensorSnapshotMinTimer > Time.realtimeSinceStartup;
            //bool withinExtremeBatchSize = currentSensorSnapshots < Cognitive3D_Preferences.Instance.SensorExtremeSnapshotCount;

            //within last send interval and less than extreme count
            //if (currentSensorSnapshots > Cognitive3D_Preferences.Instance.SensorSnapshotCount)
            //{
            //    Core_OnSendData(false);
            //}            
        }

        public delegate void onNewSensorRecorded(string sensorName, float sensorValue);
        public static event onNewSensorRecorded OnNewSensorRecorded;

        //happens after the network has sent the request, before any response
        public static event Cognitive3D_Manager.onSendData OnSensorSend;
        internal static void SensorSendEvent()
        {
            if (OnSensorSend != null)
                OnSensorSend.Invoke(false);
        }

        static float lastSendTime = -60;
        private static void Core_OnSendData(bool copyDataToCache)
        {
            /*
            if (CachedSnapshots.Keys.Count <= 0) { return; }

            if (Cognitive3D_Manager.TrackingScene == null)
            {
                Cognitive3D.Util.logDebug("Sensor.SendData could not find scene settings for scene! do not upload sensors to sceneexplorer");
                foreach(var k in CachedSnapshots)
                {
                    k.Value.Clear();
                }
                currentSensorSnapshots = 0;
                return;
            }

            if (!Cognitive3D_Manager.IsInitialized)
            {
                return;
            }


            nextSendTime = Time.realtimeSinceStartup + Cognitive3D_Preferences.Instance.SensorSnapshotMaxTimer;
            lastSendTime = Time.realtimeSinceStartup;

            //flush data from serialization

*/

            /*StringBuilder sb = new StringBuilder(1024);
            sb.Append("{");
            JsonUtil.SetString("name", Cognitive3D_Manager.DeviceId, sb);
            sb.Append(",");

            if (!string.IsNullOrEmpty(Cognitive3D_Manager.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Cognitive3D_Manager.LobbyId, sb);
                sb.Append(",");
            }

            JsonUtil.SetString("sessionid", Cognitive3D_Manager.SessionID, sb);
            sb.Append(",");
            JsonUtil.SetInt("timestamp", (int)Cognitive3D_Manager.SessionTimeStamp, sb);
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
                    if (sensorData[k].UpdateInterval >= 0.1f)
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

            foreach (var k in CachedSnapshots)
            {
                k.Value.Clear();
            }
            currentSensorSnapshots = 0;*/

            //string url = CognitiveStatics.POSTSENSORDATA(Cognitive3D_Manager.TrackingSceneId, Cognitive3D_Manager.TrackingSceneVersionNumber);
            //string content = sb.ToString();
            //
            //if (copyDataToCache)
            //{
            //    if (Cognitive3D_Manager.NetworkManager.runtimeCache != null && Cognitive3D_Manager.NetworkManager.runtimeCache.CanWrite(url, content))
            //    {
            //        Cognitive3D_Manager.NetworkManager.runtimeCache.WriteContent(url, content);
            //    }
            //}
            //
            //Cognitive3D_Manager.NetworkManager.Post(url, content);
            //if (OnSensorSend != null)
            //{
            //    OnSensorSend.Invoke(copyDataToCache);
            //}
        }

        private static void Core_OnPostSessionEnd()
        {
            Cognitive3D_Manager.OnPostSessionEnd -= Core_OnPostSessionEnd;
            LastSensorValues.Clear();
            sensorData.Clear();
            //CachedSnapshots.Clear();
            //jsonPart = 1;
            lastSendTime = -60;
        }
    }
}