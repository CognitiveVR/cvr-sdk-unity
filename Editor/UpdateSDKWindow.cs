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

        public static void Init(string version, string summary)
        {
            UpdateSDKWindow window = (UpdateSDKWindow)EditorWindow.GetWindow(typeof(UpdateSDKWindow), true, "Cognitive3D Update");
            window.sdkSummary = FormatSummary(summary);
            window.newVersion = version;
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        static string FormatSummary(string summary)
        {
            string formattedSummary = string.Empty;
            var lines = summary.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("##"))
                {
                    lines[i] = lines[i].Replace("##", "<b>") + "</b>";
                }
                formattedSummary += lines[i];
                if (i != lines.Length)
                {
                    formattedSummary += '\n';
                }
            }
            return formattedSummary;
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

            //summary
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.Label(sdkSummary);
            GUILayout.EndScrollView();
            GUILayout.FlexibleSpace();

            //centered download button
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.color = EditorCore.GreenButton;
            //TODO open github page or open package manager, depending on package install type
            if (GUILayout.Button("Download Latest Version", GUILayout.Height(40), GUILayout.MaxWidth(300)))
            {
                Application.OpenURL(CognitiveStatics.GITHUB_RELEASES);
            }
            //TODO add a button to open documentation page - how to update SDK with package manager or download+reimport package from github
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUIHorizontalLine();

            //reminder buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip this version", GUILayout.MaxWidth(200)))
            {
                EditorPrefs.SetString("c3d_skipVersion", newVersion);
                Close();
            }
            if (GUILayout.Button("Remind me next week", GUILayout.MaxWidth(300)))
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