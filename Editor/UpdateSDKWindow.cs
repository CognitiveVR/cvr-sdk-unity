using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

//this window pops up when there is a new version, this new version is not skipped and the remind date is valid (or manually checked)

namespace Cognitive3D
{
    public class UpdateSDKWindow : EditorWindow
    {
        string newVersion;
        bool reminderSet = false;
        string sdkSummary;

        //display window with changelog. changelog is provivded by github web response body
        public static void Init(string latestVersion, string changelog)
        {
            UpdateSDKWindow window = (UpdateSDKWindow)EditorWindow.GetWindow(typeof(UpdateSDKWindow), true, "Cognitive3D Update");
            window.sdkSummary = FormatSummary(changelog);
            window.newVersion = latestVersion;
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        //display window. no changelog seems available from packageinfo. point developer to package manager
        public static void InitPackageManager(string latestVersion)
        {
            UpdateSDKWindow window = (UpdateSDKWindow)EditorWindow.GetWindow(typeof(UpdateSDKWindow), true, "Cognitive3D Update");
            window.newVersion = latestVersion;
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        static string FormatSummary(string summary)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(summary.Length + 50);
            var lines = summary.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("##"))
                {
                    lines[i] = lines[i].Replace("##", "<b>") + "</b>";
                }
                sb.Append(lines[i]);
                if (i != lines.Length)
                {
                    sb.Append('\n');
                }
            }
            return sb.ToString();
        }

        Vector2 scrollPos;
        void OnGUI()
        {
            GUI.skin.label.richText = true;
            GUI.skin.label.wordWrap = true;

            //header
            GUILayout.Label("Current Version: <b>" + Cognitive3D_Manager.SDK_VERSION + "</b>");
            GUILayout.Label("New Version: <b>" + newVersion + "</b>");

            GUIHorizontalLine();

#if USE_ATTRIBUTION

#else
            //summary
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.Label(sdkSummary);
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();
#endif

            //centered download button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = EditorCore.GreenButton;
#if USE_ATTRIBUTION
            if (GUILayout.Button("Update to Latest Version", GUILayout.Height(40), GUILayout.MaxWidth(300)))
            {
                UnityEditor.PackageManager.UI.Window.Open("com.cognitive3d.c3d-sdk");
            }
#else
            if (GUILayout.Button(new GUIContent("Update to Latest Version     ", "https://github.com/CognitiveVR/cvr-sdk-unity/releases"), GUILayout.Height(30), GUILayout.Width(200)))
            {
                Application.OpenURL(CognitiveStatics.GITHUB_RELEASES);
            }
            var lastRect = GUILayoutUtility.GetLastRect();
            Rect onlineRect = lastRect;
            onlineRect.x += 164;
            GUI.Label(onlineRect, EditorCore.ExternalIcon);
#endif

            //TODO add a button to open documentation page - how to update SDK with package manager or download+reimport package from github
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUIHorizontalLine();

            //reminder buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip this version"))
            {
                EditorPrefs.SetString("c3d_skipVersion", newVersion);
                Close();
            }
            if (GUILayout.Button("Remind me next week"))
            {
                reminderSet = true;
                EditorPrefs.SetString("c3d_updateRemindDate", System.DateTime.UtcNow.AddDays(7).ToString("dd-MM-yyyy"));

                Close();
            }
            GUILayout.EndHorizontal();
        }

        void GUIHorizontalLine()
        {
            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(5);
        }

        void OnDestroy()
        {
            if (!reminderSet)
            {
                EditorPrefs.SetString("c3d_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString("dd-MM-yyyy"));
            }
        }
    }
}