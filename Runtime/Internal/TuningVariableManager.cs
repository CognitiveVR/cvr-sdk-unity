using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public static class TuningVariableManager
    {
        static Dictionary<string, Cognitive3D.TuningVariableItem> tuningVariables = new Dictionary<string, Cognitive3D.TuningVariableItem>();

        public delegate void onTuningVariablesAvailable();
        /// <summary>
        /// Called after tuning variables are available (also called after a delay if no response)
        /// </summary>
        public static event onTuningVariablesAvailable OnTuningVariablesAvailable;
        internal static void InvokeOnTuningVariablesAvailable() { if (OnTuningVariablesAvailable != null) { OnTuningVariablesAvailable.Invoke(); } }

        internal static void Reset()
        {
            tuningVariables.Clear();
        }
        internal static void SetVariable(TuningVariableItem entry)
        {
            tuningVariables.Add(entry.name, entry);
        }

        /// <summary>
        /// Returns a variable of a type using the variableName. Returns the default value if no variable is found
        /// </summary>
        /// <typeparam name="T">The expected type of variable</typeparam>
        /// <param name="variableName">The name of the variable to read</param>
        /// <param name="defaultValue">The value to return if no variable is found</param>
        /// <returns>The value of the tuning varible, or the defaultValue if not found</returns>
        public static T GetValue<T>(string variableName, T defaultValue)
        {
            Cognitive3D.TuningVariableItem returnItem;
            if (tuningVariables.TryGetValue(variableName, out returnItem))
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)returnItem.valueString;
                }
                if (typeof(T) == typeof(bool))
                {
                    return (T)(object)returnItem.valueBool;
                }
                if (typeof(T) == typeof(int) || typeof(T) == typeof(long) || typeof(T) == typeof(float) || typeof(T) == typeof(double))
                {
                    return (T)(object)returnItem.valueInt;
                }
            }
            return defaultValue;
        }

        public static Dictionary<string, Cognitive3D.TuningVariableItem> GetAllTuningVariables()
        {
            return tuningVariables;
        }
    }

    [System.Serializable]
    public class TuningVariableItem
    {
        public string name;
        public string description;
        public string type;
        public int valueInt;
        public string valueString;
        public bool valueBool;

        public override string ToString()
        {
            if (type == "string")
            {
                return string.Format("name:{0}, description:{1}, type:{2}, value:{3}", name, description, type, valueString);
            }
            if (type == "int")
            {
                return string.Format("name:{0}, description:{1}, type:{2}, value:{3}", name, description, type, valueInt);
            }
            if (type == "bool")
            {
                return string.Format("name:{0}, description:{1}, type:{2}, value:{3}", name, description, type, valueBool);
            }

            return string.Format("name:{0}, description:{1}, type:{2}", name, description, type);
        }
    }
}
