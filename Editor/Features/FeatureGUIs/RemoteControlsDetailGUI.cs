using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class RemoteControlsDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Remote Controls", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Remote Controls documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/remote-controls/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "A/B Testing and Remote Config let you customize app behavior and settings for different user segments.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("1. Create Remote Variables", EditorCore.styles.FeatureTitle);

            GUILayout.Label(
                "Set up a Remote variable in the Dashboard to fetch in your project.",
                EditorStyles.wordWrappedLabel
            );

            if (GUILayout.Button("Open Dashboard Remote Controls Manager", GUILayout.Height(30)) && FeatureLibrary.projectID > 0)
            {
                Application.OpenURL(CognitiveStatics.GetRemoteControlsSettingsUrl(FeatureLibrary.projectID));
            }

            EditorGUILayout.Space(10);

            GUILayout.Label("2. Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the Remote Controls component to the Cognitive3D_Manager prefab to fetch remote variables in your scene.", EditorStyles.wordWrappedLabel);

            var btnLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.RemoteControls>() ? "Remove Remote Controls" : "Add Remote Controls";
            if (GUILayout.Button(btnLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.RemoteControls>();
            }
        }
    }
}
