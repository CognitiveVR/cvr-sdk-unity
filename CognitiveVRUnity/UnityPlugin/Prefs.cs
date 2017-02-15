using UnityEngine;

namespace CognitiveVR
{
    // The default implementation for Unity is to use the PlayerPrefs class
    [System.Obsolete("CognitiveVR.Prefs is no longer used. Use CognitiveVR.Util instead")]
    internal static class Prefs
    {
        // Adds an entry to the dictionary for the key-value pair.
        internal static void Add(string key, float value)
        {
            // Set the value an do an immediate save
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }

        internal static void Add(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        internal static void Add(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        // Determines if the application settings dictionary contains the specified key.s
        internal static bool Contains(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        // Gets a value for the specified key.
        internal static bool TryGetValue(string key, out float value)
        {
            bool keyFound = false;
            value = default(float);
            if (Contains(key))
            {
                keyFound = true;
                value = PlayerPrefs.GetFloat(key);
            }

            return keyFound;
        }

        internal static bool TryGetValue(string key, out int value)
        {
            bool keyFound = false;
            value = default(int);
            if (Contains(key))
            {
                keyFound = true;
                value = PlayerPrefs.GetInt(key);
            }

            return keyFound;
        }

        internal static bool TryGetValue(string key, out string value)
        {
            bool keyFound = false;
            value = default(string);
            if (Contains(key))
            {
                keyFound = true;
                value = PlayerPrefs.GetString(key);
            }

            return keyFound;
        }
    }
}
