using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

//this window pops up when there is a new version, this new version is not skipped and the remind date is valid (or manually checked)
//http://www.arongranberg.com/astar/version.php

namespace CognitiveVR
{
    public class CognitiveVR_UpdateSDKWindow : EditorWindow
    {
        static string newVersion;
        bool reminderSet = false;
        static GUIStyle largeStyle;
        static GUIStyle normalStyle;
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
            if (largeStyle == null)
            {
                largeStyle = new GUIStyle(EditorStyles.largeLabel);
                largeStyle.fontSize = 20;
                largeStyle.alignment = TextAnchor.UpperCenter;
                largeStyle.richText = true;

                normalStyle = new GUIStyle(EditorStyles.label);
                normalStyle.wordWrap = true;
                normalStyle.richText = true;
            }

            GUILayout.Label("cognitiveVR SDK - New Version", largeStyle);
            GUILayout.Label("Current Version:<b>" + Core.SDK_Version + "</b>");
            GUILayout.Label("New Version:<b>" + newVersion + "</b>");

            GUILayout.Label("Changes and fixes", largeStyle);
            GUILayout.Label(sdkSummary,normalStyle);
            /*GUILayout.Label("There is a new version of the <b>A* Pathfinding Project</b> available for download.\n" +
                "The new version is <b>" + newVersion + "</b> you have <b>" + Core.SDK_Version + "</b>\n\n" +
                "<i>Summary:</i>\n" + summary, normalStyle
                );*/

            GUILayout.FlexibleSpace();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginVertical();

            Color col = GUI.color;
            GUI.backgroundColor *= new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Releases Page", GUILayout.Height(30), GUILayout.MaxWidth(300)))
            {
                Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/releases");
            }
            GUI.backgroundColor = col;

            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();



            GUILayout.FlexibleSpace();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();



            if (GUILayout.Button("Skip this version", GUILayout.MaxWidth(200)))
            {
                EditorPrefs.SetString("cvr_skipVersion", newVersion);
                Close();
            }

            if (GUILayout.Button("Remind me next week", GUILayout.MaxWidth(300)))
            {
                //EditorPrefs.SetString("AstarRemindUpdateDate", DateTime.UtcNow.AddDays(7).ToString(System.Globalization.CultureInfo.InvariantCulture));
                //EditorPrefs.SetString("AstarRemindUpdateVersion", version.ToString());

                reminderSet = true;
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(7).ToString(System.Globalization.CultureInfo.InvariantCulture));

                Close();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


            

            /*if (GUILayout.Button("Skip this version"))
            {
                reminderSet = true;
                EditorPrefs.SetString("cvr_skipVersion", newVersion);
            }

            if (GUILayout.Button("remind me next week"))
            {
                reminderSet = true;
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(7).ToString(System.Globalization.CultureInfo.InvariantCulture));
            }*/
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