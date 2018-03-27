using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR
{
    /// <summary>
    /// holds category and properties of events
    /// </summary>
    [System.Obsolete("Use CustomEvent instead")]
    public class Transaction
    {
        private string _category;
        private Dictionary<string, object> _properties = new Dictionary<string, object>();

        public Transaction(string category)
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
        public Transaction setProperties(Dictionary<string, object> properties)
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
        public Transaction setProperty(string key, object value)
        {
            _properties.Add(key, value);
            return this;
        }
        
        public enum TimeoutMode
        {
            Transaction,
            Any
        }

        private const string TXN_SUCCESS = "success";
        private const string TXN_ERROR = "error";
        
        public void end(string result = TXN_SUCCESS)
        {
            Instrumentation.SendCustomEvent(_category, _properties, HMD.position);
        }
        
        public void end(Vector3 position, string result = TXN_SUCCESS)
        {
            Instrumentation.SendCustomEvent(_category, _properties, position);
        }
        
        public void begin(Vector3 position, double timeout = 0, TimeoutMode mode = TimeoutMode.Transaction)
        {
            Instrumentation.SendCustomEvent(_category, _properties, position);
        }
        
        public void begin(double timeout = 0, TimeoutMode mode = TimeoutMode.Transaction)
        {
            Instrumentation.SendCustomEvent(_category, _properties, HMD.position);
        }
        
        public void beginAndEnd(string result = TXN_SUCCESS)
        {
            Instrumentation.SendCustomEvent(_category, _properties, HMD.position);
        }
        
        public void beginAndEnd(Vector3 position, string result = TXN_SUCCESS)
        {
            Instrumentation.SendCustomEvent(_category, _properties, position);
        }
        
        public void update(int progress)
        {
            Instrumentation.SendCustomEvent(_category, _properties, HMD.position);
        }
    }
}
