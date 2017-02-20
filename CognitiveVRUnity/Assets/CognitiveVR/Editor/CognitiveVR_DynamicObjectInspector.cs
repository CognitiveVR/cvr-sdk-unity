using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Reflection;

//TODO look into serializedproperty to support multiple object editing

namespace CognitiveVR
{
    [CustomEditor(typeof(CognitiveVR.DynamicObject))]
    public class CognitiveVR_DynamicObjectInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var dynamic = target as DynamicObject;

            //display script on component
            EditorGUI.BeginDisabledGroup(true);
            serializedObject.Update();
            SerializedProperty prop = serializedObject.FindProperty("m_Script");
            EditorGUILayout.PropertyField(prop, true, new GUILayoutOption[0]);
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();

            //Mesh
            GUILayout.Label("Mesh", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(dynamic.UseCustomMesh);
            dynamic.CommonMesh = (DynamicObject.CommonDynamicMesh)EditorGUILayout.EnumPopup("Common Mesh", dynamic.CommonMesh);
            EditorGUI.EndDisabledGroup();

            if (string.IsNullOrEmpty(dynamic.MeshName))
            {
                dynamic.MeshName = dynamic.gameObject.name.ToLower().Replace(" ", "");
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            GUILayout.BeginHorizontal();
            dynamic.UseCustomMesh = EditorGUILayout.Toggle("Use Custom Mesh", dynamic.UseCustomMesh);
            EditorGUI.BeginDisabledGroup(!dynamic.UseCustomMesh);
            dynamic.MeshName = EditorGUILayout.TextField(dynamic.MeshName);
            
            //GUILayout.EndHorizontal();

            if (GUILayout.Button("Export", GUILayout.MaxWidth(100)))
            {
                CognitiveVR_SceneExplorerExporter.ExportEachSelectionToSingle(dynamic.transform);
            }
            
            GUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            //Setup
            GUILayout.Label("Setup", EditorStyles.boldLabel);
            dynamic.SnapshotOnEnable = EditorGUILayout.Toggle("Snapshot On Enable",dynamic.SnapshotOnEnable);
            EditorGUI.BeginDisabledGroup(!dynamic.SnapshotOnEnable);
            dynamic.UpdateTicksOnEnable = EditorGUILayout.Toggle("Update Ticks on Enable", dynamic.UpdateTicksOnEnable);
            EditorGUI.EndDisabledGroup();



            //Object ID
            GUILayout.Label("IDs (Basic)", EditorStyles.boldLabel);
            dynamic.ReleaseIdOnDisable = EditorGUILayout.Toggle("Release Id OnDisable", dynamic.ReleaseIdOnDisable);
            dynamic.ReleaseIdOnDestroy = EditorGUILayout.Toggle("Release Id OnDestroy", dynamic.ReleaseIdOnDestroy);


            GUILayout.Label("Ids (Advanced)", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            dynamic.UseCustomId = EditorGUILayout.Toggle("Use Custom Id", dynamic.UseCustomId);
            EditorGUI.BeginDisabledGroup(!dynamic.UseCustomId);
            dynamic.CustomId = EditorGUILayout.IntField(dynamic.CustomId);
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            dynamic.GroupName = EditorGUILayout.TextField("Group Name", dynamic.GroupName);


            //Snapshot Threshold
            GUILayout.Label("Snapshot Threshold", EditorStyles.boldLabel);

            dynamic.SyncWithPlayerUpdate = EditorGUILayout.Toggle("Sync with Player Update", dynamic.SyncWithPlayerUpdate);

            EditorGUI.BeginDisabledGroup(dynamic.SyncWithPlayerUpdate);
            dynamic.UpdateRate = EditorGUILayout.FloatField("Update Interval", dynamic.UpdateRate);
            dynamic.UpdateRate = Mathf.Max(0.1f, dynamic.UpdateRate);
            dynamic.PositionThreshold = EditorGUILayout.FloatField("Position Threshold", dynamic.PositionThreshold);
            dynamic.PositionThreshold = Mathf.Max(0, dynamic.PositionThreshold);
            dynamic.RotationThreshold = EditorGUILayout.FloatField("Rotation Threshold", dynamic.RotationThreshold);
            dynamic.RotationThreshold = Mathf.Max(0, dynamic.RotationThreshold);
            EditorGUI.EndDisabledGroup();

            if (GUI.changed)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }
    }
}