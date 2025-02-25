using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public static class AppVariableManager
    {
        static Dictionary<string, AppVariableItem> appVariables = new Dictionary<string, AppVariableItem>();

        public delegate void onAppVariablesAvailable();
        /// <summary>
        /// Called after app variables are available (also called after a delay if no response)
        /// </summary>
        public static event onAppVariablesAvailable OnAppVariablesAvailable;
        internal static void InvokeOnAppVariablesAvailable() { if (OnAppVariablesAvailable != null) { OnAppVariablesAvailable.Invoke(); } }

        internal static void Reset()
        {
            appVariables.Clear();
        }
        internal static void SetVariable(AppVariableItem entry)
        {
            appVariables.Add(entry.name, entry);
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
            Cognitive3D.AppVariableItem returnItem;
            if (appVariables.TryGetValue(variableName, out returnItem))
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

        public static Dictionary<string, Cognitive3D.AppVariableItem> GetAllAppVariables()
        {
            return appVariables;
        }
    }

    [System.Serializable]
    public class AppVariableItem
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
