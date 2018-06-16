using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR
{
    /// <summary>
    /// holds category and properties of events
    /// </summary>
    public class CustomEvent
    {
        private string _category;
        private Dictionary<string, object> _properties = new Dictionary<string, object>(); //TODO should use a list of key/value structs. only initialize if something is added

        public CustomEvent(string category)
        {
            _category = category;
        }

        private static Transform _hmd;
        private static Transform HMD
        {
            get
            {
                if (_hmd == null)
                {
                    if (Camera.main == null) { return Object.FindObjectOfType<Transform>(); }
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
            if (null != properties)
            {
                foreach (KeyValuePair<string, object> kvp in properties)
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
            _properties.Add(key, value);
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

            Instrumentation.SendCustomEvent(_category, _properties, pos);
        }

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void Send()
        {
            if (HMD == null) { return; }

            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = HMD.position.x;
            pos[1] = HMD.position.y;
            pos[2] = HMD.position.z;

            Instrumentation.SendCustomEvent(_category, _properties, pos);
        }
    }
}
