using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitive3D
{
    public static class AppVariableManager
    {
        static AppVariableData appVariableData = new AppVariableData();

        public delegate void onAppVariablesAvailable();
        /// <summary>
        /// Called after app variables are available (also called after a delay if no response)
        /// </summary>
        public static event onAppVariablesAvailable OnAppVariablesAvailable;
        internal static void InvokeOnAppVariablesAvailable() { if (OnAppVariablesAvailable != null) { OnAppVariablesAvailable.Invoke(); } }

        /// <summary>
        /// Resets the app variable data
        /// </summary>
        internal static void Reset()
        {
            appVariableData = new AppVariableData();
        }

        /// <summary>
        /// Adds a new AB test entry to the list of AB tests in app variable data
        /// </summary>
        /// <param name="entry">The AB test item to add to the list.</param>
        internal static void SetABTest(AppVariableItem entry)
        {
            appVariableData.abTests.Add(entry);
        }

        /// <summary>
        /// Adds a new tuning configuration entry to the list of tuning configurations in app variable data
        /// </summary>
        /// <param name="entry">The tuning configuration item to add to the list.</param>
        internal static void SetTuningConfiguration(AppVariableItem entry)
        {
            appVariableData.tuningConfigurations.Add(entry);
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
            foreach (var abTest in appVariableData.abTests)
            {
                if (abTest.appVariableName == variableName)
                {
                    return ConvertValue<T>(abTest.valueString, abTest.valueBool, abTest.valueInt, defaultValue);
                }
            }

            foreach (var tuningConfiguration in appVariableData.tuningConfigurations)
            {
                if (tuningConfiguration.appVariableName == variableName)
                {
                    return ConvertValue<T>(tuningConfiguration.valueString, tuningConfiguration.valueBool, tuningConfiguration.valueInt, defaultValue);
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
    }

    [System.Serializable]
    public class AppVariableItem
    {
        public string name;
        public string description;
        public string appVariableName;
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
    public class AppVariableData
    {
        public List<AppVariableItem> abTests = new List<AppVariableItem>();
        public List<AppVariableItem> tuningConfigurations = new List<AppVariableItem>();
    }
}
