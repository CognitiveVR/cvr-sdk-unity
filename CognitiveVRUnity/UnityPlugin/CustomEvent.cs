using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace CognitiveVR
{
    /// <summary>
    /// holds category and properties of events
    /// </summary>
    public class CustomEvent
    {
        private string category;
        private string dynamicObjectId;
        private List<KeyValuePair<string, object>> _properties;

        static int lastFrameCount = 0;
        static int consecutiveEvents = 0;

        public CustomEvent(string category)
        {
            this.category = category;
            startTime = Time.realtimeSinceStartup;

            //checks that custom events aren't being created each frame (likely in update)
            if (CognitiveVR_Preferences.Instance.EnableLogging)
            {
                if (lastFrameCount >= Time.frameCount - 1)
                {
                    if (lastFrameCount != Time.frameCount)
                    {
                        lastFrameCount = Time.frameCount;
                        consecutiveEvents++;
                        if (consecutiveEvents > 200)
                        {
                            CognitiveVR.Util.logError("Cognitive3D receiving Custom Events every frame. This is not a recommended method for implementation!\nPlease see docs.cognitive3d.com/unity/customevents");
                        }
                    }
                }
                else
                {
                    lastFrameCount = Time.frameCount;
                    consecutiveEvents = 0;
                }
            }
        }

        private float startTime;

        /// <summary>
        /// Report any known state about the transaction in key-value pairs
        /// </summary>
        /// <returns>The transaction itself (to support a builder-style implementation)</returns>
        /// <param name="properties">A key-value object representing the transaction state we want to report. This can be a nested object structure.</param>
        public CustomEvent SetProperties(List<KeyValuePair<string, object>> properties)
        {
            if (properties == null) { return this; }
            if (_properties == null) { _properties = new List<KeyValuePair<string, object>>(); }
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                int foundIndex = 0;
                bool foundKey = false;
                for(int i = 0; i<_properties.Count;i++)
                {
                    if (_properties[i].Key == kvp.Key)
                    {
                        foundIndex = i;
                        foundKey = true;
                        break;
                    }
                }
                if (foundKey)
                {
                    _properties[foundIndex] = new KeyValuePair<string, object>(kvp.Key, kvp.Value);
                }
                else
                {
                    _properties.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                }
            }
            return this;
        }


        public CustomEvent SetProperties(Dictionary<string, object> properties)
        {
            if (properties == null) { return this; }
            if (_properties == null) { _properties = new List<KeyValuePair<string, object>>(); }
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                int foundIndex = 0;
                bool foundKey = false;
                for (int i = 0; i < _properties.Count; i++)
                {
                    if (_properties[i].Key == kvp.Key)
                    {
                        foundIndex = i;
                        foundKey = true;
                        break;
                    }
                }
                if (foundKey)
                {
                    _properties[foundIndex] = new KeyValuePair<string, object>(kvp.Key, kvp.Value);
                }
                else
                {
                    _properties.Add(new KeyValuePair<string, object>(kvp.Key, kvp.Value));
                }
            }
            return this;
        }

        /// <summary>
        /// Report a single piece of known state about the transaction
        /// </summary>
        /// <returns>The transaction itself (to support a builder-style implementation)</returns>
        /// <param name="key">Key for transaction state property</param>
        /// <param name="value">Value for transaction state property</param>
        public CustomEvent SetProperty(string key, object value)
        {
            if (_properties == null) { _properties = new List<KeyValuePair<string, object>>(); }
            int foundIndex = 0;
            bool foundKey = false;
            for (int i = 0; i < _properties.Count; i++)
            {
                if (_properties[i].Key == key)
                {
                    foundIndex = i;
                    foundKey = true;
                    break;
                }
            }
            if (foundKey)
            {
                _properties[foundIndex] = new KeyValuePair<string, object>(key, value);
            }
            else
            {
                _properties.Add(new KeyValuePair<string, object>(key, value));
            }
            return this;
        }

        /// <summary>
        /// Associates this event with a dynamic object, by Id
        /// </summary>
        /// <param name="sourceObjectId">The dynamic object that 'caused' this event</param>
        public CustomEvent SetDynamicObject(string sourceObjectId)
        {
            dynamicObjectId = sourceObjectId;
            return this;
        }
		
		/// <summary>
        /// Appends the latest value of each sensor to this event
        /// At the time this function is called
        /// This will replace existing sensors of the same name
        /// </summary>
        /// <returns></returns>
        public CustomEvent AppendSensors()
        {
            if (SensorRecorder.LastSensorValues.Count == 0) { CognitiveVR.Util.logWarning("Cannot SetSensors on Event - no Sensors recorded!"); return this; }
            if (_properties == null)
            {
                _properties = new List<KeyValuePair<string, object>>();

                //don't check for name collisions, since there are no previous properties
                foreach (var sensor in SensorRecorder.LastSensorValues)
                {
                    _properties.Add(new KeyValuePair<string, object>("c3d.sensor."+sensor.Key, sensor.Value));
                }
                return this;
            }
            int propertyCount = _properties.Count;

            foreach(var sensor in SensorRecorder.LastSensorValues)
            {
                string key = "c3d.sensor." + sensor.Key;
                bool foundExistingKey = false;
                for (int i = 0; i < propertyCount; i++)
                {
                    if (_properties[i].Key == key)
                    {
                        //replace
                        _properties[i] = new KeyValuePair<string, object>(key, sensor.Value);
                        foundExistingKey = true;
                        break;
                    }
                }
                if (!foundExistingKey)
                {
                    //add
                    _properties.Add(new KeyValuePair<string, object>(key, sensor.Value));
                }
            }
            return this;
        }

        /// <summary>
        /// Appends the latest value of each specified sensor to this event
        /// </summary>
        /// <param name="sensorNames">all the sensors to append</param>
        /// <returns></returns>
        public CustomEvent AppendSensors(params string[] sensorNames)
        {
            if (_properties == null) { _properties = new List<KeyValuePair<string, object>>(); }
            int propertyCount = _properties.Count;

            foreach (var sensorName in sensorNames)
            {
                string name = "c3d.sensor." + sensorName;

                float sensorValue = 0;
                if (SensorRecorder.LastSensorValues.TryGetValue(name, out sensorValue))
                {
                    bool foundExistingKey = false;
                    for (int i = 0; i < propertyCount; i++)
                    {
                        if (_properties[i].Key == name)
                        {
                            //replace
                            _properties[i] = new KeyValuePair<string, object>(name, sensorValue);
                            foundExistingKey = true;
                            break;
                        }
                    }
                    if (!foundExistingKey)
                    {
                        //add
                        _properties.Add(new KeyValuePair<string, object>(name, sensorValue));
                    }
                }
                //else - sensor with this name doesn't exist
            }

            return this;
        }

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void Send(Vector3 position)
        {
            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = position.x;
            pos[1] = position.y;
            pos[2] = position.z;

            float duration = Time.realtimeSinceStartup - startTime;
            if (duration > 0.011f)
            {
                SetProperty("duration", duration);
            }

            SendCustomEvent(category, _properties, pos, dynamicObjectId);
        }

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void Send()
        {
            float[] pos = new float[3] { 0, 0, 0 };

            if (Core.HMD != null)
            {
                pos[0] = Core.HMD.position.x;
                pos[1] = Core.HMD.position.y;
                pos[2] = Core.HMD.position.z;
            }

            float duration = Time.realtimeSinceStartup - startTime;
            if (duration > 0.011f)
            {
                SetProperty("duration", duration);
            }

            SendCustomEvent(category, _properties, pos, dynamicObjectId);
        }


        //public functions
        //add transaction
        //send transactions

        internal static void Initialize()
        {
            Core.OnSendData -= Core_OnSendData;
            Core.OnSendData += Core_OnSendData;
            Core.CheckSessionId();
            autoTimer_nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.TransactionSnapshotMaxTimer;

            if (automaticTimerActive == false)
                NetworkManager.Sender.StartCoroutine(AutomaticSendTimer());
        }

        private static void Core_OnSendData()
        {
            SendTransactions();
        }


        //used for unique identifier for sceneexplorer file names
        private static int partCount = 1;

        static int cachedEvents = 0;
        public static int CachedEvents { get { return cachedEvents; } }

        private static System.Text.StringBuilder eventBuilder = new System.Text.StringBuilder(512);
        private static System.Text.StringBuilder builder = new System.Text.StringBuilder(1024);

        static bool automaticTimerActive;
        static float autoTimer_nextSendTime = 0;
        internal static IEnumerator AutomaticSendTimer()
        {
            automaticTimerActive = true;
            while (true)
            {
                while (autoTimer_nextSendTime > Time.realtimeSinceStartup)
                {
                    yield return null;
                }
                //try to send!
                autoTimer_nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.TransactionSnapshotMaxTimer;
                if (CognitiveVR_Preferences.Instance.EnableDevLogging)
                    Util.logDevelopment("check to automatically send events");
                SendTransactions();
            }
        }

        //checks for min send time and extreme batch size before calling send
        static void TrySendTransactions()
        {

            bool withinMinTimer = minTimer_lastSendTime + CognitiveVR_Preferences.Instance.TransactionSnapshotMinTimer > Time.realtimeSinceStartup;
            bool withinExtremeBatchSize = cachedEvents < CognitiveVR_Preferences.Instance.TransactionExtremeSnapshotCount;

            //within last send interval and less than extreme count
            if (withinMinTimer && withinExtremeBatchSize)
            {
                //Util.logDebug("instrumentation less than timer, less than extreme batch size");
                return;
            }
            SendTransactions();
        }

        static float minTimer_lastSendTime = -60;
        static void SendTransactions()
        {
            if (cachedEvents == 0)
            {
                return;
            }

            if (!Core.IsInitialized)
            {
                return;
            }

            //TODO should hold until extreme batch size reached
            if (string.IsNullOrEmpty(Core.TrackingSceneId))
            {
                Util.logDebug("Instrumentation.SendTransactions could not find CurrentSceneId! has scene been uploaded and CognitiveVR_Manager.Initialize been called?");
                cachedEvents = 0;
                eventBuilder.Length = 0;
                return;
            }

            autoTimer_nextSendTime = Time.realtimeSinceStartup + CognitiveVR_Preferences.Instance.DynamicSnapshotMaxTimer;
            minTimer_lastSendTime = Time.realtimeSinceStartup;

            cachedEvents = 0;
            //bundle up header stuff and transaction data

            //clear the transaction builder
            builder.Length = 0;

            //CognitiveVR.Util.logDebug("package transaction event data " + partCount);
            //when thresholds are reached, etc

            builder.Append("{");

            //header
            JsonUtil.SetString("userid", Core.UniqueID, builder);
            builder.Append(",");

            if (!string.IsNullOrEmpty(Core.LobbyId))
            {
                JsonUtil.SetString("lobbyId", Core.LobbyId, builder);
                builder.Append(",");
            }

            JsonUtil.SetDouble("timestamp", Core.SessionTimeStamp, builder);
            builder.Append(",");
            JsonUtil.SetString("sessionid", Core.SessionID, builder);
            builder.Append(",");
            JsonUtil.SetInt("part", partCount, builder);
            partCount++;
            builder.Append(",");

            JsonUtil.SetString("formatversion", "1.0", builder);
            builder.Append(",");

            //events
            builder.Append("\"data\":[");

            builder.Append(eventBuilder.ToString());

            if (eventBuilder.Length > 0)
                builder.Remove(builder.Length - 1, 1); //remove the last comma
            builder.Append("]");

            builder.Append("}");

            eventBuilder.Length = 0;

            //send transaction contents to scene explorer

            string packagedEvents = builder.ToString();

            //sends all packaged transaction events from instrumentaiton subsystem to events endpoint on scene explorer
            string url = CognitiveStatics.POSTEVENTDATA(Core.TrackingSceneId, Core.TrackingSceneVersionNumber);

            NetworkManager.Post(url, packagedEvents);
            if (OnCustomEventSend != null)
            {
                OnCustomEventSend.Invoke();
            }
        }

        public static void SendCustomEvent(string category, float[] position, string dynamicObjectId = "")
        {
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", Util.Timestamp(Time.frameCount), eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);
            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            CustomEventRecordedEvent(category, new Vector3(position[0], position[1], position[2]), null, dynamicObjectId, Util.Timestamp(Time.frameCount));
            cachedEvents++;
            if (cachedEvents >= CognitiveVR_Preferences.Instance.TransactionSnapshotCount)
            {
                TrySendTransactions();
            }
        }

        public delegate void onCustomEventRecorded(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time);
        public static event onCustomEventRecorded OnCustomEventRecorded;
        private static void CustomEventRecordedEvent(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time)
        {
            if (OnCustomEventRecorded != null)
            {
                OnCustomEventRecorded.Invoke(name, pos, properties, dynamicObjectId, time);
            }
        }

        //happens after the network has sent the request, before any response
        public static event Core.onDataSend OnCustomEventSend;

        public static void SendCustomEvent(string category, Vector3 position, string dynamicObjectId = "")
        {
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", Util.Timestamp(Time.frameCount), eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);

            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            CustomEventRecordedEvent(category, position, null, dynamicObjectId, Util.Timestamp(Time.frameCount));
            cachedEvents++;
            if (cachedEvents >= CognitiveVR_Preferences.Instance.TransactionSnapshotCount)
            {
                TrySendTransactions();
            }
        }

        //writes json to display the transaction in sceneexplorer
        public static void SendCustomEvent(string category, List<KeyValuePair<string, object>> properties, Vector3 position, string dynamicObjectId = "")
        {
            SendCustomEvent(category, properties, new float[3] { position.x, position.y, position.z }, dynamicObjectId);
        }

        public static void SendCustomEvent(string category, List<KeyValuePair<string, object>> properties, float[] position, string dynamicObjectId = "")
        {
            eventBuilder.Append("{");
            JsonUtil.SetString("name", category, eventBuilder);
            eventBuilder.Append(",");
            JsonUtil.SetDouble("time", Util.Timestamp(Time.frameCount), eventBuilder);
            if (!string.IsNullOrEmpty(dynamicObjectId))
            {
                eventBuilder.Append(',');
                JsonUtil.SetString("dynamicId", dynamicObjectId, eventBuilder);
            }
            eventBuilder.Append(",");
            JsonUtil.SetVector("point", position, eventBuilder);

            if (properties != null && properties.Count > 0)
            {
                eventBuilder.Append(",");
                eventBuilder.Append("\"properties\":{");
                for (int i = 0; i < properties.Count; i++)
                {
                    if (i != 0) { eventBuilder.Append(","); }
                    if (properties[i].Value.GetType() == typeof(string))
                    {
                        JsonUtil.SetString(properties[i].Key, (string)properties[i].Value, eventBuilder);
                    }
                    else
                    {
                        JsonUtil.SetObject(properties[i].Key, properties[i].Value, eventBuilder);
                    }
                }
                eventBuilder.Append("}"); //close properties object
            }

            eventBuilder.Append("}"); //close transaction object
            eventBuilder.Append(",");

            CustomEventRecordedEvent(category, new Vector3(position[0], position[1], position[2]), properties, dynamicObjectId, Util.Timestamp(Time.frameCount));
            cachedEvents++;
            if (cachedEvents >= CognitiveVR_Preferences.Instance.TransactionSnapshotCount)
            {
                TrySendTransactions();
            }
        }
    }
}
