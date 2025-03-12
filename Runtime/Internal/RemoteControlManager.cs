using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public static class RemoteControlManager
    {
        static RemoteVariableCollection remoteVariableCollection = new RemoteVariableCollection();
        static List<RemoteVariableItem> remoteVariables = new List<RemoteVariableItem>();

        public delegate void onRemoteControlsAvailable();
        /// <summary>
        /// Called after remote variables are available (also called after a delay if no response)
        /// </summary>
        public static event onRemoteControlsAvailable OnRemoteControlsAvailable;
        internal static void InvokeOnRemoteControlsAvailable() { if (OnRemoteControlsAvailable != null) { OnRemoteControlsAvailable.Invoke(); } }

        /// <summary>
        /// Returns true if remote variables have already been returned from the server (or loaded from the local cache as a fallback)
        /// </summary>
        public static bool HasFetchedVariables
        {
            get
            {
                return Components.RemoteControls.hasFetchedVariables;
            }
        }

        /// <summary>
        /// Resets the remote variable data
        /// </summary>
        internal static void Reset()
        {
            remoteVariableCollection = new RemoteVariableCollection();
            remoteVariables.Clear();
        }

        /// <summary>
        /// Adds a new AB test entry to the list of AB tests in remote control collection
        /// </summary>
        /// <param name="entry">The AB test item to add to the list.</param>
        internal static void SetABTest(RemoteVariableItem entry)
        {
            remoteVariableCollection.abTests.Add(entry);
            remoteVariables.Add(entry);
        }

        /// <summary>
        /// Adds a new remote configuration entry to the list of remote configurations in remote control collection
        /// </summary>
        /// <param name="entry">The remote configuration item to add to the list.</param>
        internal static void SetRemoteConfiguration(RemoteVariableItem entry)
        {
            remoteVariableCollection.remoteConfigurations.Add(entry);
            remoteVariables.Add(entry);
        }

        /// <summary>
        /// Returns a variable of a type using the variableName. Returns the default value if no variable is found
        /// </summary>
        /// <typeparam name="T">The expected type of variable</typeparam>
        /// <param name="variableName">The name of the variable to read</param>
        /// <param name="defaultValue">The value to return if no variable is found</param>
        /// <returns>The value of the app varible, or the defaultValue if not found</returns>
        public static T GetValue<T>(string variableName, T defaultValue)
        {
            foreach (var item in remoteVariables)
            {
                if (item.remoteVariableName == variableName)
                {
                    return ConvertValue<T>(item.valueString, item.valueBool, item.valueInt, defaultValue);
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Converts stored string, boolean, or integer values to the requested generic type.
        /// Falls back to the provided default value if the type is unsupported.
        /// </summary>
        /// <typeparam name="T">The target type for conversion.</typeparam>
        /// <param name="stringValue">The string representation of the value.</param>
        /// <param name="boolValue">The boolean representation of the value.</param>
        /// <param name="intValue">The integer representation of the value.</param>
        /// <param name="defaultValue">The default value to return if the type is unsupported.</param>
        /// <returns>The converted value of type T, or the default value if conversion is not possible.</returns>
        private static T ConvertValue<T>(string stringValue, bool boolValue, int intValue, T defaultValue)
        {
            if (typeof(T) == typeof(string))
                return (T)(object)stringValue;
            if (typeof(T) == typeof(bool))
                return (T)(object)boolValue;
            if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || 
                typeof(T) == typeof(float) || typeof(T) == typeof(double))
                return (T)(object)intValue;

            return defaultValue;
        }

        /// <summary>
        /// Logs all Remote Variables. For development and debugging
        /// </summary>
        public static void ListAllVariables()
        {
            foreach(var v in remoteVariables)
            {
                Debug.Log(v.ToString());
            }
        }
    }

    [System.Serializable]
    public class RemoteVariableItem
    {
        public string name;
        public string description;
        public string remoteVariableName;
        public string type;
        public int valueInt;
        public string valueString;
        public bool valueBool;

        public override string ToString()
        {
            if (type == "string")
            {
                return string.Format("name:{0}, type:{1}, value:{2}", name, type, valueString);
            }
            if (type == "int")
            {
                return string.Format("name:{0}, type:{1}, value:{2}", name, type, valueInt);
            }
            if (type == "bool")
            {
                return string.Format("name:{0}, type:{1}, value:{2}", name, type, valueBool);
            }

            return string.Format("name:{0}, type:{1}", name, type);
        }
    }

    [System.Serializable]
    internal class RemoteVariableCollection
    {
        public List<RemoteVariableItem> abTests = new List<RemoteVariableItem>();
        public List<RemoteVariableItem> remoteConfigurations = new List<RemoteVariableItem>();
    }
}
