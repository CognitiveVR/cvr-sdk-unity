using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR
{
    /// <summary>
    /// holds category and properties of events
    /// </summary>
    public class CustomEvent
    {
        public string category { get; private set; }
        public string dynamicObjectId { get; private set; }
        private Dictionary<string, object> _properties;// = new Dictionary<string, object>(); //TODO should use a list of key/value structs. only initialize if something is added

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

        private static Transform _hmd;
        private static Transform HMD
        {
            get
            {
                if (_hmd == null)
                {
                    if (Camera.main == null)
                    {
                        Camera c = Object.FindObjectOfType<Camera>();
                        if (c != null)
                            _hmd = c.transform;
                    }
                    else
                        _hmd = Camera.main.transform;
                }
                return _hmd;
            }
        }

        /// <summary>
        /// Report any known state about the transaction in key-value pairs
        /// </summary>
        /// <returns>The transaction itself (to support a builder-style implementation)</returns>
        /// <param name="properties">A key-value object representing the transaction state we want to report. This can be a nested object structure.</param>
        public CustomEvent SetProperties(Dictionary<string, object> properties)
        {
            if (properties == null) { return this; }
            if (_properties == null) { _properties = new Dictionary<string, object>(); }
            foreach (KeyValuePair<string, object> kvp in properties)
            {
                if (_properties.ContainsKey(kvp.Key))
                    _properties[kvp.Key] = kvp.Value;
                else
                    _properties.Add(kvp.Key, kvp.Value);
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
            if (_properties == null) { _properties = new Dictionary<string, object>(); }
            if (_properties.ContainsKey(key))
                _properties[key] = value;
            else
                _properties.Add(key, value);
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
                if (_properties == null) { _properties = new Dictionary<string, object>(); }
                if (_properties.ContainsKey("duration"))
                    _properties["duration"] = duration;
                else
                    _properties.Add("duration", duration);
            }

            Instrumentation.SendCustomEvent(category, _properties, pos, dynamicObjectId);
        }

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void Send()
        {
            float[] pos = new float[3] { 0, 0, 0 };

            if (HMD != null)
            {
                pos[0] = HMD.position.x;
                pos[1] = HMD.position.y;
                pos[2] = HMD.position.z;
            }

            float duration = Time.realtimeSinceStartup - startTime;
            if (duration > 0.011f)
            {
                if (_properties == null) { _properties = new Dictionary<string, object>(); }
                if (_properties.ContainsKey("duration"))
                    _properties["duration"] = duration;
                else
                    _properties.Add("duration", duration);
            }

            Instrumentation.SendCustomEvent(category, _properties, pos, dynamicObjectId);
        }
    }
}
