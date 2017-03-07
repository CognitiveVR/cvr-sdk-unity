using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

//TODO look into serializedproperty to support multiple object editing

namespace CognitiveVR
{
    [CustomEditor(typeof(CognitiveVR.DynamicObject))]
    [CanEditMultipleObjects]
    public class CognitiveVR_DynamicObjectInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            //display script on component
            EditorGUI.BeginDisabledGroup(true);
            serializedObject.Update();
            SerializedProperty prop = serializedObject.FindProperty("m_Script");
            EditorGUILayout.PropertyField(prop, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            //Mesh
            GUILayout.Label("Mesh", EditorStyles.boldLabel);

            var useCustomMesh = serializedObject.FindProperty("UseCustomMesh");
            EditorGUI.BeginDisabledGroup(useCustomMesh.boolValue);

            var commonMeshName = serializedObject.FindProperty("CommonMesh");
            UnityEditor.EditorGUILayout.PropertyField(commonMeshName);

            EditorGUI.EndDisabledGroup();

            var meshname = serializedObject.FindProperty("MeshName");

            if (string.IsNullOrEmpty(meshname.stringValue))
            {
                meshname.stringValue = serializedObject.targetObject.name.ToLower().Replace(" ", "");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            GUILayout.BeginHorizontal();
            
            UnityEditor.EditorGUILayout.PropertyField(useCustomMesh);
            EditorGUI.BeginDisabledGroup(!useCustomMesh.boolValue);

            UnityEditor.EditorGUILayout.PropertyField(meshname,new GUIContent(""));

            

            if (GUILayout.Button("Export", GUILayout.MaxWidth(100)))
            {
                MenuItems.ExportSelectedObjectsPrefab();
            }
            
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            //Setup
            GUILayout.Label("Setup", EditorStyles.boldLabel);
            var snapshotOnEnable = serializedObject.FindProperty("SnapshotOnEnable");
            UnityEditor.EditorGUILayout.PropertyField(snapshotOnEnable, new GUIContent("Snapshot On Enable", "Save the transform when this object is first enabled"));
            EditorGUI.BeginDisabledGroup(!snapshotOnEnable.boolValue);
            var updateTicksOnEnable = serializedObject.FindProperty("UpdateTicksOnEnable");
            UnityEditor.EditorGUILayout.PropertyField(updateTicksOnEnable, new GUIContent("Update Ticks on Enable", "Begin coroutine that saves the transform of this object when it moves"));

            EditorGUI.EndDisabledGroup();



            //Object ID
            GUILayout.Label("Ids (Basic)", EditorStyles.boldLabel);

            var releaseOnDisable = serializedObject.FindProperty("ReleaseIdOnDisable");
            EditorGUILayout.PropertyField(releaseOnDisable, new GUIContent("Release Id OnDisable", "Allow other objects to use this Id when this object is no longer active"));
            var releaseOnDestroy = serializedObject.FindProperty("ReleaseIdOnDestroy");
            EditorGUILayout.PropertyField(releaseOnDestroy, new GUIContent("Release Id OnDestroy", "Allow other objects to use this Id when this object is no longer active"));


            GUILayout.Label("Ids (Advanced)", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            var useCustomID = serializedObject.FindProperty("UseCustomId");
            EditorGUILayout.PropertyField(useCustomID, new GUIContent("Use Custom Id", "This is used to identify specific objects to aggregate the position across multiple play sessions"));
            EditorGUI.BeginDisabledGroup(!useCustomID.boolValue);
            var customId = serializedObject.FindProperty("CustomId");
            EditorGUILayout.PropertyField(customId, new GUIContent(""));
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            var groupName = serializedObject.FindProperty("GroupName");
            EditorGUILayout.PropertyField(groupName, new GUIContent("Group Name", "This is used to identify types of objects and combine aggregated data"));

            //Snapshot Threshold
            GUILayout.Label("Snapshot Threshold", EditorStyles.boldLabel);

            var syncWithPlayerUpdate = serializedObject.FindProperty("SyncWithPlayerUpdate");
            EditorGUILayout.PropertyField(syncWithPlayerUpdate, new GUIContent("Sync with Player Update", "This is the Snapshot interval in the Tracker Options Window"));

            EditorGUI.BeginDisabledGroup(syncWithPlayerUpdate.boolValue);
            var updateRate = serializedObject.FindProperty("UpdateRate");
            EditorGUILayout.PropertyField(updateRate, new GUIContent("Sync with Player Update", "This is the Snapshot interval in the Tracker Options Window"));
            updateRate.floatValue = Mathf.Max(0.1f, updateRate.floatValue);
            EditorGUI.EndDisabledGroup();

            var positionThreshold = serializedObject.FindProperty("PositionThreshold");
            EditorGUILayout.PropertyField(positionThreshold, new GUIContent("Position Threshold", "Meters the object must move to write a new snapshot. Checked each 'Tick'"));
            positionThreshold.floatValue = Mathf.Max(0, positionThreshold.floatValue);
            var rotationThreshold = serializedObject.FindProperty("RotationThreshold");
            EditorGUILayout.PropertyField(rotationThreshold, new GUIContent("Rotation Threshold", "Degrees the object must rotate to write a new snapshot. Checked each 'Tick'"));
            rotationThreshold.floatValue = Mathf.Max(0, rotationThreshold.floatValue);
            

            if (GUI.changed)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }
    }
}