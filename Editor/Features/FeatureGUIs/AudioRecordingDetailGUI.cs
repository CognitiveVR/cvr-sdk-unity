using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class AudioRecordingDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Audio Recording", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Audio Recording documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "This feature captures user speech through the device’s microphone on Android. Currently, this feature only supports microphone audio and does not capture in-app or system audio.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the Audio Recorder component to the Cognitive3D_Manager prefab to record the audio.", EditorStyles.wordWrappedLabel);

            var btnLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.MicrophoneAudioRecorder>() ? "Remove Audio Recorder" : "Add Audio Recorder";
            if (GUILayout.Button(btnLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.MicrophoneAudioRecorder>();
            }
        }
    }
}
