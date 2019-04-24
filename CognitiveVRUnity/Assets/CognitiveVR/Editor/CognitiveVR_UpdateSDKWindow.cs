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
        string sdkSummary;

        public static void Init(string version, string summary)
        {
            newVersion = version;
            CognitiveVR_UpdateSDKWindow window = (CognitiveVR_UpdateSDKWindow)EditorWindow.GetWindow(typeof(CognitiveVR_UpdateSDKWindow),true,"cognitiveVR Update");
            window.sdkSummary = summary;
            window.Show();
        }

        void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUILayout.Label("cognitiveVR SDK - New Version", EditorCore.HeaderStyle);
            GUILayout.Label("Current Version:<b>" + Core.SDK_VERSION + "</b>");
            GUILayout.Label("New Version:<b>" + newVersion + "</b>");

            GUILayout.Label("Notes", EditorCore.HeaderStyle);
            GUI.skin.label.wordWrap = true;
            GUILayout.Label(sdkSummary);

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            GUI.color = EditorCore.GreenButton;

            if (GUILayout.Button("Download Latest Version", GUILayout.Height(40), GUILayout.MaxWidth(300)))
            {
                Application.OpenURL(CognitiveStatics.GITHUB_RELEASES);
            }

            GUI.color = Color.white;

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();



            GUILayout.FlexibleSpace();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(5);

            GUILayout.BeginHorizontal();



            if (GUILayout.Button("Skip this version", GUILayout.MaxWidth(200)))
            {
                EditorPrefs.SetString("cvr_skipVersion", newVersion);
                Close();
            }

            if (GUILayout.Button("Remind me next week", GUILayout.MaxWidth(300)))
            {
                reminderSet = true;
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(7).ToString(System.Globalization.CultureInfo.InvariantCulture));

                Close();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
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