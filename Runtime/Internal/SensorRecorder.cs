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
        internal class SensorData
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

        //holds the latest value of each sensor type. can be appended to custom events
        //TODO merge LastSensorValues into sensorData collection
        public static Dictionary<string, float> LastSensorValues = new Dictionary<string, float>();

        static bool hasDisplayedSceneIdWarning;
        static bool hasDisplayedInitializeWarning;

        //called each time a session starts
        internal static void Initialize()
        {
            Cognitive3D_Manager.OnPostSessionEnd += Core_OnPostSessionEnd;

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
            LastSensorValues.Add(sensorName, initialValue);
            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(sensorName, initialValue);
        }

        public static void RecordDataPoint(string category, float value)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                if (!hasDisplayedInitializeWarning)
                {
                    hasDisplayedInitializeWarning = true;
                    Cognitive3D.Util.logWarning("SensorRecorder session has not started!");
                }
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("SensorRecorder invalid SceneId");
                }
                return;
            }

            if (float.IsNaN(value))
            {
                Cognitive3D.Util.logWarning("SensorRecorder category:"+ category + " is value: NaN");
                return;
            }

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
            LastSensorValues[category] = value;

            CoreInterface.RecordSensor(category, value, Util.Timestamp(Time.frameCount));

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(category, value);
        }

        ///doubles are recorded raw, but cast to float for Active Session View
        public static void RecordDataPoint(string category, double value)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                if (!hasDisplayedInitializeWarning)
                {
                    hasDisplayedInitializeWarning = true;
                    Cognitive3D.Util.logWarning("SensorRecorder session has not started!");
                }
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("SensorRecorder invalid SceneId");
                }
                return;
            }

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
            LastSensorValues[category] = (float)value;

            CoreInterface.RecordSensor(category, (float)value, Util.Timestamp(Time.frameCount));

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(category, (float)value);
        }

        public static void RecordDataPoint(string category, float value, double unixTimestamp)
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                if (!hasDisplayedInitializeWarning)
                {
                    hasDisplayedInitializeWarning = true;
                    Cognitive3D.Util.logWarning("SensorRecorder session has not started!");
                }
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null)
            {
                if (!hasDisplayedSceneIdWarning)
                {
                    hasDisplayedSceneIdWarning = true;
                    Cognitive3D.Util.logWarning("SensorRecorder invalid SceneId");
                }
                return;
            }

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
            LastSensorValues[category] = value;

            CoreInterface.RecordSensor(category, (float)value, unixTimestamp);

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(category, value);
        }

        //used by active session view
        public delegate void onNewSensorRecorded(string sensorName, float sensorValue);
        public static event onNewSensorRecorded OnNewSensorRecorded;

        //happens after the network has sent the request, before any response
        //used by active session view
        public static event Cognitive3D_Manager.onSendData OnSensorSend;
        internal static void SensorSendEvent()
        {
            if (OnSensorSend != null)
                OnSensorSend.Invoke(false);
        }

        private static void Core_OnPostSessionEnd()
        {
            Cognitive3D_Manager.OnPostSessionEnd -= Core_OnPostSessionEnd;
            LastSensorValues.Clear();
            sensorData.Clear();
        }
    }
}