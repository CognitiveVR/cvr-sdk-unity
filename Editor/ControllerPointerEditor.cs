﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//hides options in advanced foldout

namespace Cognitive3D
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
                var localPositionOffset = serializedObject.FindProperty("LocalPositionOffset");

                EditorGUILayout.PropertyField(displayRenderer, new GUIContent("Display Line Renderer"));
                EditorGUILayout.PropertyField(lineOverride, new GUIContent("Line Renderer Override"));
                EditorGUILayout.PropertyField(localPositionOffset, new GUIContent("Local Position Offset"));

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
    }
}