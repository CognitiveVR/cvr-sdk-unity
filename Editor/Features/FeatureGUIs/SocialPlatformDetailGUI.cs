using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class SocialPlatformDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Social Platform", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Social Platform documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/components/#social-platform");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Automatically record platform user and app identity data (e.g., App ID, User ID, Display Name) by adding a component that performs an entitlement check using the platform’s SDK.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("1. Prepare Your App (Developer Portal)", EditorCore.styles.FeatureTitle);

            GUILayout.Label(
                "Set up your app ID on the platform's developer dashboard, enable the necessary user data permissions, publish the app.",
                EditorStyles.wordWrappedLabel
            );

#if C3D_OCULUS
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "To enable this feature for Meta (Oculus):\n" +
                "• Set up your Oculus App ID in the Meta Developer Dashboard.\n" +
                "• Enable the necessary user data permissions.\n" +
                "• Publish your app.\n" +
                "• Add the App ID in Unity under Oculus > Platform > Edit Settings.",
                MessageType.Info
            );
#endif

            EditorGUILayout.Space(10);

            GUILayout.Label("2. Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the Social Platform component to the Cognitive3D_Manager prefab to record platform-specific user data.", EditorStyles.wordWrappedLabel);

            var btnLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.SocialPlatform>() ? "Remove Social Platform" : "Add Social Platform";
            if (GUILayout.Button(btnLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.SocialPlatform>();
            }
        }
    }
}
