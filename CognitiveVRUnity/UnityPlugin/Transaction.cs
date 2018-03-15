using UnityEngine;
using System.Collections.Generic;

namespace CognitiveVR
{
    /// <summary>
    /// holds category and properties of events
    /// </summary>
    public class Transaction
    {
        private string _category;
        private Dictionary<string, object> _properties = new Dictionary<string, object>();
        //internal Transaction(string category, string transactionId = null) { this._category = category; }

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

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void begin(Vector3 position, double timeout = 0)
        {
            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = position.x;
            pos[1] = position.y;
            pos[2] = position.z;

            Instrumentation.beginTransaction(_category, _properties, pos);
        }

        /// <summary>
        /// Send telemetry to report the beginning of a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="timeout">How long to keep the transaction 'open' without new activity</param>
        /// <param name="mode">The type of activity which will keep the transaction open</param>
        public void begin(double timeout = 0)
        {
            if (HMD == null) { return; }

            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = HMD.position.x;
            pos[1] = HMD.position.y;
            pos[2] = HMD.position.z;

            Instrumentation.beginTransaction(_category, _properties, pos);
        }

        /// <summary>
        /// Send telemetry to report an update to a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="progress">A value between 1 and 99, which should increase between subsequent calls to update</param>
        public void update(int progress)
        {
            Instrumentation.updateTransaction(_category, progress, _properties);
        }

        /// <summary>
        /// Send telemetry to report an end to a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="result">CognitiveVR.Constants.TXN_SUCCESS, CognitiveVR.Constants.TXN_ERROR, or any application defined string describing the result</param>
        public void end()
        {
            if (HMD == null) { return; }

            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = HMD.position.x;
            pos[1] = HMD.position.y;
            pos[2] = HMD.position.z;

            Instrumentation.endTransaction(_category, _properties, pos);
        }

        /// <summary>
        /// Send telemetry to report an end to a transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="position">Overload the position of this event for Scene Explorer</param>
        /// <param name="result">CognitiveVR.Constants.TXN_SUCCESS, CognitiveVR.Constants.TXN_ERROR, or any application defined string describing the result</param>
        public void end(Vector3 position)
        {
            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = position.x;
            pos[1] = position.y;
            pos[2] = position.z;

            Instrumentation.endTransaction(_category, _properties, pos);
        }

        /// <summary>
        /// Send telemetry to report an instantaneous transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="result">CognitiveVR.Constants.TXN_SUCCESS, CognitiveVR.Constants.TXN_ERROR, or any application defined string describing the result</param>
        public void beginAndEnd()
        {
            if (HMD == null) { return; }

            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = HMD.position.x;
            pos[1] = HMD.position.y;
            pos[2] = HMD.position.z;

            Instrumentation.endTransaction(_category, _properties, pos);
        }

        /// <summary>
        /// Send telemetry to report an instantaneous transaction, including any state properties which have been set.
        /// </summary>
        /// <param name="position">Overload the position of this event for Scene Explorer</param>
        /// <param name="result">CognitiveVR.Constants.TXN_SUCCESS, CognitiveVR.Constants.TXN_ERROR, or any application defined string describing the result</param>
        public void beginAndEnd(Vector3 position)
        {
            float[] pos = new float[3] { 0, 0, 0 };

            pos[0] = position.x;
            pos[1] = position.y;
            pos[2] = position.z;

            Instrumentation.endTransaction(_category, _properties, pos);
        }
    }
}
