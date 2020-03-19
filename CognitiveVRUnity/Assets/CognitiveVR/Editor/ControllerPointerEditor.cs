using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//hides options in advanced foldout

namespace CognitiveVR
{
    [CustomEditor(typeof(ControllerPointer))]
    public class ControllerPointerEditor : Editor
    {
        static bool foldout = false;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var script = serializedObject.FindProperty("m_Script");
            
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            foldout = EditorGUILayout.Foldout(foldout, "Advanced");
            if (foldout)
            {
                var displayRenderer = serializedObject.FindProperty("DisplayLineRenderer");
                var lineOverride = serializedObject.FindProperty("LineRendererOverride");
                var samppleResolution = serializedObject.FindProperty("SampleResolution");
                var angle = serializedObject.FindProperty("Angle");
                var localPositionOffset = serializedObject.FindProperty("LocalPositionOffset");
                var requiredHmdParallel = serializedObject.FindProperty("RequireHMDParallel");

                EditorGUILayout.PropertyField(displayRenderer, new GUIContent("Display Line Renderer"));
                EditorGUILayout.PropertyField(lineOverride, new GUIContent("Line Renderer Override"));
                EditorGUILayout.PropertyField(samppleResolution, new GUIContent("Sample Resolution"));
                EditorGUILayout.PropertyField(angle, new GUIContent("Angle"));
                EditorGUILayout.PropertyField(localPositionOffset, new GUIContent("Local Position Offset"));
                EditorGUILayout.PropertyField(requiredHmdParallel, new GUIContent("Require HMD Parallel"));

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
    }
}