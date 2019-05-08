using UnityEngine;
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

            Instrumentation.SendCustomEvent(category, _properties, pos, dynamicObjectId);
        }
    }
}
