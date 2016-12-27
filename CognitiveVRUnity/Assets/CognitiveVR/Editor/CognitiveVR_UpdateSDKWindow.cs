using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

//this window pops up when there is a new version, this new version is not skipped and the remind date is valid (or manually checked)

namespace CognitiveVR
{
    public class CognitiveVR_UpdateSDKWindow : EditorWindow
    {
        static string newVersion;
        bool reminderSet = false;

        public static void Init(string version)
        {
            newVersion = version;
            CognitiveVR_UpdateSDKWindow window = (CognitiveVR_UpdateSDKWindow)EditorWindow.GetWindow(typeof(CognitiveVR_UpdateSDKWindow));
            window.Show();
        }

        void OnGUI()
        {
            if (GUILayout.Button("Download"))
            {
                Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/releases");
            }

            if (GUILayout.Button("skip this version"))
            {
                reminderSet = true;
                EditorPrefs.SetString("cvr_skipVersion", newVersion);
            }

            if (GUILayout.Button("remind me tomorrow"))
            {
                reminderSet = true;
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }

            if (GUILayout.Button("remind me next week"))
            {
                reminderSet = true;
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(7).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        void OnDestroy()
        {
            if (!reminderSet)
            {
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }
}