using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Cognitive3D.Components;

namespace Cognitive3D
{
    internal class AudioRecordingDetailGUI : IFeatureDetailGUI
    {
        private string channelName = "default";

        private Vector2 scrollPos;


        bool audioSourceExist = true;

        AppAudioRecorder[] _appAudioRecorders;
        AppAudioRecorder[] appAudioRecorders
        {
            get
            {
                if (_appAudioRecorders == null || _appAudioRecorders.Length == 0 || System.Array.Exists(_appAudioRecorders, x => x == null))
                {
                    _appAudioRecorders = GameObject.FindObjectsByType<AppAudioRecorder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                }
                return _appAudioRecorders;
            }
        }

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
                "This feature captures user speech through the device’s microphone, and it also supports recording in-app audio on Android.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("1. Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the Microphone Audio Recorder component to the Cognitive3D_Manager prefab to record the audio.", EditorStyles.wordWrappedLabel);

            var btnLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.MicrophoneAudioRecorder>() ? "Remove Audio Recorder" : "Add Audio Recorder";
            if (GUILayout.Button(btnLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.MicrophoneAudioRecorder>();
            }

            EditorGUILayout.Space(10);

            GUILayout.Label("2. (Optional) Add App Audio Recording", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Add audio recording to specific GameObjects in your scene.", EditorStyles.wordWrappedLabel);

            // Add to selected section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Add to Selected GameObjects", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Channel Name:", GUILayout.Width(100));
            channelName = EditorGUILayout.TextField(channelName);
            GUILayout.EndHorizontal();

            if (audioSourceExist)
            {
                EditorGUILayout.HelpBox(
                    "Select GameObjects with AudioSource components, enter a unique channel name, then click Add.",
                    MessageType.Info
                );
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Failed to add the component. No AudioSource component found on selected GameObject(s).",
                    MessageType.Warning
                );
            }

            EditorGUI.BeginDisabledGroup(Selection.gameObjects.Length == 0 || string.IsNullOrEmpty(channelName));
            if (GUILayout.Button($"Add to {Selection.gameObjects.Length} Selected GameObject(s)", GUILayout.Height(25)))
            {
                audioSourceExist = CheckAudioSourceComponent();
                if (audioSourceExist)
                {
                    AttachAppAudioRecorderToSelected(channelName);
                    RefreshList();
                }
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            // List section
            GUILayout.BeginHorizontal();
            GUILayout.Label("Current App Audio Recorders", EditorCore.styles.FeatureTitle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Scene: {UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name}", EditorStyles.miniLabel);
            GUILayout.EndHorizontal();

            if (appAudioRecorders.Length == 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label("No App Audio Recorders found in the current scene.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.Label("Select GameObjects with AudioSource components and use the section above to add them.", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label($"Found {appAudioRecorders.Length} recorder(s):", EditorStyles.miniLabel);
                EditorGUILayout.Space(3);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                DrawHeader();
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
                foreach (var obj in appAudioRecorders)
                {
                    DrawAudioRecorderRow(obj);
                }
                GUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("GameObject", GUILayout.Width(150));
            DrawColumnSeparator();

            GUILayout.Label("Audio Channel Name", GUILayout.Width(150));
            DrawColumnSeparator();

            GUILayout.FlexibleSpace();

            GUILayout.Label("Actions", GUILayout.Width(60));
            DrawColumnSeparator();

            if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh List"), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                RefreshList();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColumnSeparator()
        {
            var rect = GUILayoutUtility.GetRect(1, 18, GUILayout.Width(1));
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
        }

        private void DrawAudioRecorderRow(AppAudioRecorder obj)
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(); // Inner padding container
            GUILayout.Space(3); // Top padding

            EditorGUILayout.BeginHorizontal();

            // GameObject name (clickable)
            GUILayout.Space(5);
            if (GUILayout.Button(obj.gameObject.name, EditorStyles.linkLabel, GUILayout.Width(150)))
            {
                Selection.activeGameObject = obj.gameObject;
                EditorGUIUtility.PingObject(obj.gameObject);
            }

            // Channel name
            GUILayout.Space(5);
            GUILayout.Label(obj.audioChannelName, GUILayout.Width(150));

            GUILayout.FlexibleSpace();

            // Remove button
            if (GUILayout.Button(new GUIContent("Remove", "Remove the App Audio Recorder component from this GameObject"), EditorStyles.miniButton, GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Remove App Audio Recorder",
                    $"Remove App Audio Recorder from '{obj.gameObject.name}'?",
                    "Remove", "Cancel"))
                {
                    Undo.DestroyObjectImmediate(obj);
                    RefreshList();
                }
            }

            GUILayout.Space(30);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void RefreshList()
        {
            _appAudioRecorders = null;
            audioSourceExist = true;
        }

        #region Audio Utilities
        internal static void AttachAppAudioRecorderToSelected(string channelName)
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.GetComponent<AppAudioRecorder>() == null)
                {
                    var recorder = Undo.AddComponent<AppAudioRecorder>(obj);
                    recorder.audioChannelName = channelName; // assumes your AppAudioRecorder has a ChannelName property
                }
                else
                {
                    obj.GetComponent<AppAudioRecorder>().audioChannelName = channelName;
                }
            }
        }

        internal static bool CheckAudioSourceComponent()
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (!obj.GetComponent<AudioSource>())
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
