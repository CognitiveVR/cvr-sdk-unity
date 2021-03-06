﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CognitiveVR;
using System.Text;
using CognitiveVR.External;

namespace CognitiveVR
{
    public static class SensorRecorder
    {
        private static int jsonPart = 1;
        private static Dictionary<string, List<string>> CachedSnapshots = new Dictionary<string, List<string>>();
        private static int currentSensorSnapshots = 0;
        public static int CachedSensors { get { return currentSensorSnapshots; } }

        //holds the latest value of each sensor type. can be appended to custom events
        public static Dictionary<string, float> LastSensorValues = new Dictionary<string, float>();

        static SensorRecorder()
        {
            Core.OnSendData += Core_OnSendData;
            nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.SensorSnapshotMaxTimer;
            NetworkManager.Sender.StartCoroutine(AutomaticSendTimer());
        }

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

            if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            }
            else
            {
                CachedSnapshots.Add(category, new List<string>(512));
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            }

            if (LastSensorValues.ContainsKey(category))
            {
                LastSensorValues[category] = value;
            }
            else
            {
                LastSensorValues.Add(category, value);
                if (OnNewSensorRecorded != null)
                    OnNewSensorRecorded(category, value);
            }

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

            if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            }
            else
            {
                CachedSnapshots.Add(category, new List<string>(512));
                CachedSnapshots[category].Add(GetSensorDataToString(Util.Timestamp(Time.frameCount), value));
            }

            if (LastSensorValues.ContainsKey(category))
            {
                LastSensorValues[category] = (float)value;
            }
            else
            {
                LastSensorValues.Add(category, (float)value);
                if (OnNewSensorRecorded != null)
                    OnNewSensorRecorded(category, (float)value);
            }

            currentSensorSnapshots++;
            if (currentSensorSnapshots >= CognitiveVR_Preferences.Instance.SensorSnapshotCount)
            {
                TrySendData();
            }
        }

        public static void RecordDataPoint(string category, float value, double timestamp)
        {
            if (Core.IsInitialized == false)
            {
                CognitiveVR.Util.logWarning("Sensor cannot be sent before Session Begin!");
                return;
            }
            if (Core.TrackingScene == null) { CognitiveVR.Util.logDevelopment("Sensor recorded without SceneId"); return; }

            if (CachedSnapshots.ContainsKey(category))
            {
                CachedSnapshots[category].Add(GetSensorDataToString(timestamp, value));
            }
            else
            {
                CachedSnapshots.Add(category, new List<string>(512));
                CachedSnapshots[category].Add(GetSensorDataToString(timestamp, value));
            }

            if (LastSensorValues.ContainsKey(category))
            {
                LastSensorValues[category] = value;
            }
            else
            {
                LastSensorValues.Add(category, value);
                if (OnNewSensorRecorded != null)
                    OnNewSensorRecorded(category, value);
            }

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
            if (CachedSnapshots.Keys.Count <= 0) { CognitiveVR.Util.logDebug("Sensor.SendData found no data"); return; }

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
            JsonUtil.SetString("formatversion", "1.0", sb);
            sb.Append(",");


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

            string url = CognitiveStatics.POSTSENSORDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);
            string content = sb.ToString();

            if (copyDataToCache)
            {
                if (NetworkManager.lc != null && NetworkManager.lc.CanAppend(url, content))
                {
                    NetworkManager.lc.Append(url, content);
                }
            }

            NetworkManager.Post(url, content);
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