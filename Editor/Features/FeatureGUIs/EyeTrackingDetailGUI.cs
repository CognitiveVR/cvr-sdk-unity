using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class EyeTrackingDetailGUI : IFeatureDetailGUI
    {
        const string HarmonEyesDefine = "C3D_HARMONEYES";

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Eye Tracking", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Gaze & Fixations documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/gaze-fixations/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Gaze and fixations are captured automatically when the platform SDK supports eye tracking. No component needed, just enable eye tracking on the device.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(15);

            DrawHarmonEyesSection();
        }

        private void DrawHarmonEyesSection()
        {
            GUILayout.Label("HarmonEyes", EditorCore.styles.FeatureTitle);
            GUILayout.Label(
                "Records HarmonEyes cognitive-state metrics (Mental Workload, Fatigue, Attention, Mental Readiness) as sensors. Complete both steps.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            bool defineSet = EditorCore.GetPlayerDefines().Contains(HarmonEyesDefine);

            // Step 1: scripting define symbol
            GUILayout.Label("1. Scripting define symbol", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the C3D_HARMONEYES define so the HarmonEyes integration compiles.", EditorStyles.wordWrappedLabel);

            var defineLabel = defineSet ? "Remove C3D_HARMONEYES" : "Add C3D_HARMONEYES";
            if (GUILayout.Button(defineLabel, GUILayout.Height(30)))
            {
                if (defineSet)
                {
                    EditorCore.RemoveDefine(HarmonEyesDefine);
                }
                else
                {
                    EditorCore.SetPlayerDefine(HarmonEyesDefine);
                }
            }

            if (!defineSet)
            {
                EditorGUILayout.HelpBox("Add only after the HarmonEyes SDK is imported into your project.", MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // Step 2: add the component to the manager prefab
            GUILayout.Label("2. Add to Cognitive3D_Manager prefab", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Adds the HarmonEyes Tracking component to the Cognitive3D_Manager prefab to record the metrics.", EditorStyles.wordWrappedLabel);

            EditorGUI.BeginDisabledGroup(!defineSet);
            var btnLabel = FeatureLibrary.TryGetComponent<Cognitive3D.Components.HarmonEyesTracking>() ? "Remove HarmonEyes Tracking" : "Add HarmonEyes Tracking";
            if (GUILayout.Button(btnLabel, GUILayout.Height(30)))
            {
                FeatureLibrary.AddOrRemoveComponent<Cognitive3D.Components.HarmonEyesTracking>();
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
