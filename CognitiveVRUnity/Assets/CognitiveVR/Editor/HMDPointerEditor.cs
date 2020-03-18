using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//hides options in advanced foldout

namespace CognitiveVR
{
    [CustomEditor(typeof(HMDPointer))]
    public class HMDPointerEditor : Editor
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
                var markerTransform = serializedObject.FindProperty("MarkerTransform");
                var distance = serializedObject.FindProperty("Distance");
                var speed = serializedObject.FindProperty("Speed");
                

                EditorGUILayout.PropertyField(markerTransform, new GUIContent("Marker Transform"));
                EditorGUI.BeginDisabledGroup(markerTransform.objectReferenceValue == null);
                EditorGUILayout.PropertyField(distance, new GUIContent("Distance"));
                EditorGUILayout.PropertyField(speed, new GUIContent("Speed"));
                EditorGUI.EndDisabledGroup();

                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }
        }
    }
}