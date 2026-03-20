using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class PreferencesGUI
    {
        private Editor preferencesInspectorEditor;

        internal void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope(EditorCore.styles.DetailContainer))
            {
                GUILayout.Label(
                    "View the current Cognitive3D project preferences, including application keys, tracking options, data sending settings, and scene export configurations.",
                    EditorCore.styles.ItemDescription
                );

                GUILayout.Space(5);

                // Load current preferences
                var currentPrefs = EditorCore.GetPreferences();

                if (currentPrefs == null)
                {
                    EditorGUILayout.HelpBox("Cognitive3D Preferences asset not found!", MessageType.Error);
                    return;
                }

                // Draw the preferences asset in a read-only state
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ObjectField(
                    "Preferences Asset",
                    currentPrefs,
                    typeof(Cognitive3D_Preferences),
                    false
                );
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                EditorGUILayout.LabelField("Current Preferences Asset", EditorCore.styles.IssuesTitleBoldLabel);
                EditorGUILayout.Space(5);

                // Use existing preferences inspector UI
                if (preferencesInspectorEditor == null)
                {
                    preferencesInspectorEditor = Editor.CreateEditor(currentPrefs, typeof(PreferencesInspector));
                }
                preferencesInspectorEditor.OnInspectorGUI();
            }
        }
    }
}
