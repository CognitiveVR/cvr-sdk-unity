using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    public class SensorDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("Sensors", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open Sensors documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/sensors/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "Sensors are a feature to record a value or property over time.\n\nIf you have the hardware to support it, you can record Sensor data for Heart Rate, GSR, ECG,  and view it as a graph on the dashboard.\n\nSeveral types of data are recorded by default, including FPS, as well as HMD pitch and yaw.",
                EditorStyles.wordWrappedLabel
            );

            string codeSample = "float sensorData = Random.Range(1, 100f);\nCognitive3D.SensorRecorder\n     .RecordDataPoint(\"SensorName\", sensorData);";

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
                GUILayout.MinHeight(90)
            );

            EditorGUI.SelectableLabel(rect, codeSample, EditorCore.styles.CodeSnippet);
        }
    }
}
