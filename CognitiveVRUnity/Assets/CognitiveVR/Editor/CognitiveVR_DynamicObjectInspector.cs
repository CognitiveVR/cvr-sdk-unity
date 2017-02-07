using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;

namespace CognitiveVR
{
    [CustomEditor(typeof(DynamicObject))]
    public class CognitiveVR_DynamicObjectInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var dynamic = target as DynamicObject;

            EditorGUI.BeginDisabledGroup(true);
            serializedObject.Update();
            SerializedProperty prop = serializedObject.FindProperty("m_Script");
            EditorGUILayout.PropertyField(prop, true, new GUILayoutOption[0]);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();

            GUILayout.Label("Setup", EditorStyles.boldLabel);

            dynamic.meshName = EditorGUILayout.TextField("Mesh Name", dynamic.meshName);

            dynamic.SnapshotOnEnable = EditorGUILayout.Toggle("Snapshot On Enable",dynamic.SnapshotOnEnable);
            if (dynamic.SnapshotOnEnable)
            {
                EditorGUI.indentLevel++;
                dynamic.UpdateTicksOnEnable = EditorGUILayout.Toggle("Update Ticks on Enable",dynamic.UpdateTicksOnEnable);
                EditorGUI.indentLevel--;
            }

            GUILayout.Label("Ids", EditorStyles.boldLabel);
            dynamic.UseCustomId = EditorGUILayout.Toggle("Use Custom Id", dynamic.UseCustomId);
            if (dynamic.UseCustomId)
            {
                EditorGUI.indentLevel++;
                dynamic.CustomId = EditorGUILayout.IntField("Custom Id", dynamic.CustomId);
                EditorGUI.indentLevel--;
            }
            dynamic.ReleaseIdOnDestroy = EditorGUILayout.Toggle("Release Id OnDestroy", dynamic.ReleaseIdOnDestroy);

            GUILayout.Label("Snapshot Threshold", EditorStyles.boldLabel);
            dynamic.updateRate = EditorGUILayout.FloatField("Update Interval", dynamic.updateRate);
            dynamic.PositionThreshold = EditorGUILayout.FloatField("Position Threshold", dynamic.PositionThreshold);
            dynamic.RotationThreshold = EditorGUILayout.FloatField("Rotation Threshold", dynamic.RotationThreshold);

            if (GUI.changed)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }
    }
}