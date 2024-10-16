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
        private const float SENSOR_VALUE_CHANGE_THRESHOLD = 0.001f;

        /// <summary>
        /// Minimum frequency at which to send sensor value - 2 seconds (0.5Hz) <br/>
        /// Send at this frequency even if not changed - as a keep alive signal
        /// </summary>
        private const float MIN_FREQUENCY_KEEP_ALIVE_SIGNAL = 2f;

        /// <summary>
        /// A representation of the sensor data
        /// </summary>
        internal class SensorData
        {
            /// <summary>
            /// Name of the sensor
            /// </summary>
            public string Name;

            /// <summary>
            /// The recording rate (or frequency)
            /// </summary>
            public string Rate;

            /// <summary>
            /// The time it will be next recorded (increments in UpdateInterval)
            /// </summary>
            public float NextRecordTime;

            /// <summary>
            /// 1/frequency
            /// </summary>
            public float UpdateInterval;
            
            /// <summary>
            /// Storing the previous value of this sensor
            /// </summary>
            public LastSensor LastSensorValue;

            public SensorData(string name, float rate, LastSensor lastSensor)
            {
                Name = name;
                Rate = string.Format("{0:0.00}", rate);
                
                // Default rate is 10Hz
                if (rate == 0)
                {
                    UpdateInterval = 1 / 10;
                    Util.logWarning("Initializing Sensor " + name + " at 0 hz! Defaulting to 10hz");
                }
                else
                {
                    UpdateInterval = 1 / rate;
                }

                LastSensorValue = lastSensor;
            }
        }

        /// <summary>
        /// Used to track the last recorded value of the sensor
        /// We need this to compare and check if sensor value has changed before recording it
        /// </summary>
        internal class LastSensor
        {
            /// <summary>
            /// The value of the sensor
            /// </summary>
            internal float value;

            /// <summary>
            /// The time the last value was recorded
            /// </summary>
            internal float recordedTime;

            public LastSensor(float sensorValue, float sensorRecordedTime)
            {
                value = sensorValue;
                recordedTime = sensorRecordedTime;
            }
        }

        /// <summary>
        /// A dictionary of category and SensorData
        /// </summary>
        internal static Dictionary<string, SensorData> sensorData = new Dictionary<string, SensorData>();

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
            sensorData.Add(sensorName, new SensorData(sensorName, HzRate, new LastSensor(initialValue, Time.realtimeSinceStartup)));

            CoreInterface.InitializeSensor(sensorName, HzRate);

            if (OnNewSensorRecorded != null)
                OnNewSensorRecorded(sensorName, initialValue);
        }

        /// <summary>
        /// Records sensor values as float <br/>
        /// </summary>
        /// <param name="category"></param>
        /// <param name="value"></param>
        public static void RecordDataPoint(string category, float value)
        {
            RecordDataPoint(category, value, Util.Timestamp(Time.frameCount));
        }

        /// <summary>
        /// Records sensor values as double <br/>
        /// WARNING: Casts them back to single precision float values
        /// </summary>
        /// <param name="category"></param>
        /// <param name="value"></param>
        public static void RecordDataPoint(string category, double value)
        {
            RecordDataPoint(category, (float)value, Util.Timestamp(Time.frameCount));
        }

        /// <summary>
        /// Records sensor values with a timestamp <br/>
        /// For use in case of threading
        /// </summary>
        /// <param name="category">The name/category of sensor</param>
        /// <param name="value">The value to record</param>
        /// <param name="unixTimestamp">The unix timestamp (in seconds) when it was recorded</param>
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

            //if ONLY changed enough or more than two seconds
            if (System.Math.Abs(sensorData[category].LastSensorValue.value - value) >= SENSOR_VALUE_CHANGE_THRESHOLD ||
                (Time.realtimeSinceStartup - sensorData[category].LastSensorValue.recordedTime) >= MIN_FREQUENCY_KEEP_ALIVE_SIGNAL)
            {
                //update internal values and record data
                sensorData[category].NextRecordTime = Time.realtimeSinceStartup + sensorData[category].UpdateInterval;
                sensorData[category].LastSensorValue = new LastSensor(value, Time.realtimeSinceStartup);

                CoreInterface.RecordSensor(category, value, unixTimestamp);

                if (OnNewSensorRecorded != null)
                    OnNewSensorRecorded(category, value);
            }
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
            sensorData.Clear();
        }
    }
}