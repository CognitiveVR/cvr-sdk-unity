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
            dynamic.SnapshotOnEnable = EditorGUILayout.Toggle(new GUIContent("Snapshot On Enable","Save the transform when this object is first enabled"), dynamic.SnapshotOnEnable);
            EditorGUI.BeginDisabledGroup(!dynamic.SnapshotOnEnable);
            dynamic.UpdateTicksOnEnable = EditorGUILayout.Toggle(new GUIContent("Update Ticks on Enable","Begin coroutine that saves the transform of this object when it moves"), dynamic.UpdateTicksOnEnable);
            EditorGUI.EndDisabledGroup();



            //Object ID
            GUILayout.Label("IDs (Basic)", EditorStyles.boldLabel);
            dynamic.ReleaseIdOnDisable = EditorGUILayout.Toggle(new GUIContent("Release Id OnDisable","Allow other objects to use this Id when this object is no longer active"), dynamic.ReleaseIdOnDisable);
            dynamic.ReleaseIdOnDestroy = EditorGUILayout.Toggle(new GUIContent("Release Id OnDestroy", "Allow other objects to use this Id when this object is no longer active"), dynamic.ReleaseIdOnDestroy);


            GUILayout.Label("Ids (Advanced)", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal();
            dynamic.UseCustomId = EditorGUILayout.Toggle(new GUIContent("Use Custom Id","This is used to identify specific objects to aggregate the position across multiple play sessions"), dynamic.UseCustomId);
            EditorGUI.BeginDisabledGroup(!dynamic.UseCustomId);
            dynamic.CustomId = EditorGUILayout.IntField(dynamic.CustomId);
            EditorGUI.EndDisabledGroup();
            GUILayout.EndHorizontal();

            dynamic.GroupName = EditorGUILayout.TextField(new GUIContent("Group Name","This is used to identify types of objects and combine aggregated data"), dynamic.GroupName);


            //Snapshot Threshold
            GUILayout.Label("Snapshot Threshold", EditorStyles.boldLabel);

            dynamic.SyncWithPlayerUpdate = EditorGUILayout.Toggle(new GUIContent("Sync with Player Update","This is the Snapshot interval in the Tracker Options Window"), dynamic.SyncWithPlayerUpdate);

            EditorGUI.BeginDisabledGroup(dynamic.SyncWithPlayerUpdate);
            dynamic.UpdateRate = EditorGUILayout.FloatField(new GUIContent("Update Interval","Only active used if the object is set to 'Tick'. The delay before checking if the object moved beyond the threshold"), dynamic.UpdateRate);
            dynamic.UpdateRate = Mathf.Max(0.1f, dynamic.UpdateRate);
            EditorGUI.EndDisabledGroup();

            dynamic.PositionThreshold = EditorGUILayout.FloatField(new GUIContent("Position Threshold","Meters the object must move to write a new snapshot. Checked each 'Tick'"), dynamic.PositionThreshold);
            dynamic.PositionThreshold = Mathf.Max(0, dynamic.PositionThreshold);
            dynamic.RotationThreshold = EditorGUILayout.FloatField(new GUIContent("Rotation Threshold", "Degrees the object must rotate to write a new snapshot. Checked each 'Tick'"), dynamic.RotationThreshold);
            dynamic.RotationThreshold = Mathf.Max(0, dynamic.RotationThreshold);
            

            if (GUI.changed)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }
    }
}