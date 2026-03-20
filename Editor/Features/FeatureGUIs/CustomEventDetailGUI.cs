using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class CustomEventDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Custom Events", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Custom Events documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/customevents/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "A Custom Event is a feature to highlight specific interactions and incidents during the session.\nYou are able to view these Custom Events in the session details page or real-time in Scene Explorer.",
                EditorStyles.wordWrappedLabel
            );

            string codeSample = "new CustomEvent(\"Event Name\").Send();";

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Example:", EditorStyles.boldLabel);

            // Creates a 1x1 texture with grey background color
            Texture2D bgTexture = new Texture2D(1, 1);
            bgTexture.SetPixel(0, 0, new Color(0.35f, 0.35f, 0.35f));
            bgTexture.Apply();
            EditorCore.styles.CodeSnippet.normal.background = bgTexture;

            Rect rect = GUILayoutUtility.GetRect(
                new GUIContent(codeSample),
                EditorCore.styles.CodeSnippet,
                GUILayout.ExpandWidth(true),
                GUILayout.MinHeight(60)
            );

            EditorGUI.SelectableLabel(rect, codeSample, EditorCore.styles.CodeSnippet);

            EditorGUILayout.Space(5);
            codeSample = "new Cognitive3D.CustomEvent(\"Event Name\")\n.SetProperty(\"Property Name\", Property Value)\n.Send()";

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Example with properties:", EditorStyles.boldLabel);

            rect = GUILayoutUtility.GetRect(
                new GUIContent(codeSample),
                EditorCore.styles.CodeSnippet,
                GUILayout.ExpandWidth(true),
                GUILayout.MinHeight(90)
            );

            EditorGUI.SelectableLabel(rect, codeSample, EditorCore.styles.CodeSnippet);
        }
    }
}
