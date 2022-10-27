using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Cognitive3D
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
            if (Cognitive3D_Preferences.Instance.EnableLogging)
            {
                if (lastFrameCount >= Time.frameCount - 1)
                {
                    if (lastFrameCount != Time.frameCount)
                    {
                        lastFrameCount = Time.frameCount;
                        consecutiveEvents++;
                        if (consecutiveEvents > 200)
                        {
                            Cognitive3D.Util.logError("Cognitive3D receiving Custom Events every frame. This is not a recommended method for implementation!\nPlease see docs.cognitive3d.com/unity/customevents");
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
            if (SensorRecorder.LastSensorValues.Count == 0) { Cognitive3D.Util.logWarning("Cannot SetSensors on Event - no Sensors recorded!"); return this; }
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
            float duration = Time.realtimeSinceStartup - startTime;
            if (duration > 0.011f)
            {
                SetProperty("duration", duration);
            }

            SendCustomEvent(category, _properties, position, dynamicObjectId);
        }

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void Send()
        {
            float duration = Time.realtimeSinceStartup - startTime;
            if (duration > 0.011f)
            {
                SetProperty("duration", duration);
            }

            Vector3 position = Vector3.zero;
            if (GameplayReferences.HMD != null)
            {
                position = GameplayReferences.HMD.position;
            }

            SendCustomEvent(category, _properties, position, dynamicObjectId);
        }

        internal static void Initialize()
        {

        }

        public delegate void onCustomEventRecorded(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time);
        //used by active session view
        public static event onCustomEventRecorded OnCustomEventRecorded;
        internal static void CustomEventRecordedEvent(string name, Vector3 pos, List<KeyValuePair<string, object>> properties, string dynamicObjectId, double time)
        {
            if (OnCustomEventRecorded != null)
            {
                OnCustomEventRecorded.Invoke(name, pos, properties, dynamicObjectId, time);
            }
        }

        //happens after the network has sent the request, before any response
        //used by active session view
        public static event Cognitive3D_Manager.onSendData OnCustomEventSend;
        internal static void CustomEventSendEvent()
        {
            if (OnCustomEventSend != null)
                OnCustomEventSend.Invoke(false);
        }

        public static void SendCustomEvent(string category, Vector3 position, string dynamicObjectId = "")
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Custom Events cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Custom Event sent without SceneId"); return; }

            CoreInterface.RecordCustomEvent(category, position, dynamicObjectId);
        }

        //writes json to display the transaction in sceneexplorer
        public static void SendCustomEvent(string category, List<KeyValuePair<string, object>> properties, Vector3 position, string dynamicObjectId = "")
        {
            if (Cognitive3D_Manager.IsInitialized == false)
            {
                Cognitive3D.Util.logWarning("Custom Events cannot be sent before Session Begin!");
                return;
            }
            if (Cognitive3D_Manager.TrackingScene == null) { Cognitive3D.Util.logDevelopment("Custom Event sent without SceneId"); return; }

            CoreInterface.RecordCustomEvent(category, properties, position, dynamicObjectId);
        }
    }
}
