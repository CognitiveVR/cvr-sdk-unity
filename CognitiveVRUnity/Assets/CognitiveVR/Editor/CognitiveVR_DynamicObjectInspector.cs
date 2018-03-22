using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

namespace CognitiveVR
{
    [CustomEditor(typeof(CognitiveVR.DynamicObject))]
    [CanEditMultipleObjects]
    public class CognitiveVR_DynamicObjectInspector : Editor
    {
        static bool foldout = false;
        public override void OnInspectorGUI()
        {
            var script = serializedObject.FindProperty("m_Script");
            var groupName = serializedObject.FindProperty("GroupName");
            var syncWithPlayerUpdate = serializedObject.FindProperty("SyncWithPlayerUpdate");
            var updateRate = serializedObject.FindProperty("UpdateRate");
            var positionThreshold = serializedObject.FindProperty("PositionThreshold");
            var rotationThreshold = serializedObject.FindProperty("RotationThreshold");
            var snapshotOnEnable = serializedObject.FindProperty("SnapshotOnEnable");
            var updateTicksOnEnable = serializedObject.FindProperty("UpdateTicksOnEnable");
            var releaseOnDisable = serializedObject.FindProperty("ReleaseIdOnDisable");
            var releaseOnDestroy = serializedObject.FindProperty("ReleaseIdOnDestroy");
            var useCustomID = serializedObject.FindProperty("UseCustomId");
            var customId = serializedObject.FindProperty("CustomId");
            var commonMeshName = serializedObject.FindProperty("CommonMesh");
            var meshname = serializedObject.FindProperty("MeshName");
            var useCustomMesh = serializedObject.FindProperty("UseCustomMesh");
            var trackGaze = serializedObject.FindProperty("TrackGaze");
            var requiresManualEnable = serializedObject.FindProperty("RequiresManualEnable");

            var iId = serializedObject.FindProperty("iId");            
            var dynamic = target as DynamicObject;
            if (dynamic.iId != dynamic.GetInstanceID() || string.IsNullOrEmpty(customId.stringValue)) //only check if something has changed on a dynamic
            {
                if (useCustomID.boolValue)
                {
                    iId.intValue = dynamic.GetInstanceID(); //this will often mark the scene dirty without any apparent or meaningful changes
                    CheckCustomId(customId);
                    //TODO cache while scene active, but don't bother marking scene dirty if only iId is dirty
                }
            }

#if UNITY_5_6_OR_NEWER
            //video
            //var flipVideo = serializedObject.FindProperty("FlipVideo");
            var externalVideoSource = serializedObject.FindProperty("ExternalVideoSource");
            var videoPlayer = serializedObject.FindProperty("VideoPlayer");
#endif

            //display script on component
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            GUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.PropertyField(useCustomMesh);
            
            if (useCustomMesh.boolValue)
            {
                if (string.IsNullOrEmpty(meshname.stringValue))
                {
                    meshname.stringValue = serializedObject.targetObject.name.ToLower().Replace(" ", "_");
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
                UnityEditor.EditorGUILayout.PropertyField(meshname, new GUIContent(""));
            }
            else
            {
                UnityEditor.EditorGUILayout.PropertyField(commonMeshName, new GUIContent(""));
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(trackGaze, new GUIContent("Track Gaze on Dynamic Object"));
            DisplayGazeTrackHelpbox(trackGaze.boolValue);

            GUILayout.Space(10);

            foldout = EditorGUILayout.Foldout(foldout, "Advanced");
            if (foldout)
            {

                //Mesh
                GUILayout.Label("Mesh", EditorStyles.boldLabel);

                EditorGUI.BeginDisabledGroup(useCustomMesh.boolValue);
                UnityEditor.EditorGUILayout.PropertyField(commonMeshName);
                EditorGUI.EndDisabledGroup();

                if (string.IsNullOrEmpty(meshname.stringValue))
                {
                    meshname.stringValue = serializedObject.targetObject.name.ToLower().Replace(" ", "_");
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }

                GUILayout.BeginHorizontal();
                UnityEditor.EditorGUILayout.PropertyField(useCustomMesh);

                EditorGUI.BeginDisabledGroup(!useCustomMesh.boolValue);
                UnityEditor.EditorGUILayout.PropertyField(meshname, new GUIContent(""));
                if (GUILayout.Button("Export", "ButtonLeft", GUILayout.MaxWidth(100)))
                {
                    CognitiveVR_SceneExportWindow.ExportSelectedObjectsPrefab();
                }

                EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                if (GUILayout.Button("Upload", "ButtonRight", GUILayout.MaxWidth(100)))
                {
                    CognitiveVR_SceneExportWindow.UploadSelectedDynamicObjects(true);
                    //CognitiveVR_SceneExportWindow.ExportSelectedObjectsPrefab();
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();


                //Setup
                GUILayout.Label("Setup", EditorStyles.boldLabel);

                UnityEditor.EditorGUILayout.PropertyField(snapshotOnEnable, new GUIContent("Snapshot On Enable", "Save the transform when this object is first enabled"));

                //EditorGUI.BeginDisabledGroup(!snapshotOnEnable.boolValue);
                UnityEditor.EditorGUILayout.PropertyField(updateTicksOnEnable, new GUIContent("Update Ticks on Enable", "Begin coroutine that saves the transform of this object when it moves"));
                //EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(trackGaze, new GUIContent("Track Gaze on Dynamic Object"));

                DisplayGazeTrackHelpbox(trackGaze.boolValue);

                EditorGUILayout.PropertyField(requiresManualEnable, new GUIContent("Requires Manual Enable", "If true, ManualEnable must be called before OnEnable will function. Used to set initial variables on an object"));


                //Object ID
                GUILayout.Label("Ids", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(useCustomID, new GUIContent("Use Custom Id", "This is used to identify specific objects to aggregate the position across multiple play sessions"));

                EditorGUI.BeginDisabledGroup(!useCustomID.boolValue);
                EditorGUILayout.PropertyField(customId, new GUIContent(""));
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(groupName, new GUIContent("Group Name", "This is used to identify types of objects and combine aggregated data"));

                EditorGUILayout.PropertyField(releaseOnDisable, new GUIContent("Release Id OnDisable", "Allow other objects to use this Id when this object is no longer active"));
                EditorGUILayout.PropertyField(releaseOnDestroy, new GUIContent("Release Id OnDestroy", "Allow other objects to use this Id when this object is no longer active"));

                //Snapshot Threshold
                GUILayout.Label("Snapshot Threshold", EditorStyles.boldLabel);


                EditorGUILayout.PropertyField(syncWithPlayerUpdate, new GUIContent("Sync with Player Update", "This is the Snapshot interval in the Tracker Options Window"));

                EditorGUI.BeginDisabledGroup(syncWithPlayerUpdate.boolValue);
                if (syncWithPlayerUpdate.boolValue)
                {
                    EditorGUILayout.FloatField(new GUIContent("Update Rate", "Synced with Player Update.\nThe interval between checking for modified position and rotation"), EditorCore.GetPreferences().SnapshotInterval);
                }
                else
                {
                    EditorGUILayout.PropertyField(updateRate, new GUIContent("Update Rate", "The interval between checking for modified position and rotation"));
                }
                updateRate.floatValue = Mathf.Max(0.1f, updateRate.floatValue);
                EditorGUI.EndDisabledGroup();


                EditorGUILayout.PropertyField(positionThreshold, new GUIContent("Position Threshold", "Meters the object must move to write a new snapshot. Checked each 'Tick'"));
                positionThreshold.floatValue = Mathf.Max(0, positionThreshold.floatValue);

                EditorGUILayout.PropertyField(rotationThreshold, new GUIContent("Rotation Threshold", "Degrees the object must rotate to write a new snapshot. Checked each 'Tick'"));
                rotationThreshold.floatValue = Mathf.Max(0, rotationThreshold.floatValue);

#if !UNITY_5_6_OR_NEWER
            GUILayout.Label("Video Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Video Player requires Unity 5.6 or newer!", MessageType.Warning);
#else
                GUILayout.Label("Video Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(videoPlayer, new GUIContent("Video Player"));

                EditorGUILayout.PropertyField(externalVideoSource, new GUIContent("External Video Source", "The URL source of the video"));
                //EditorGUILayout.PropertyField(flipVideo, new GUIContent("Flip Video Horizontally"));
#endif
            } //advanced foldout


            if (GUI.changed)
            {
                //remove spaces from meshname
                meshname.stringValue = meshname.stringValue.Replace(" ", "_");

                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        void DisplayGazeTrackHelpbox(bool trackgaze)
        {
            if (trackgaze && ((DynamicObject)serializedObject.targetObjects[0]).GetComponent<Canvas>() == null)
            {
                DynamicObject dyn = null;
                int missingCollider = 0;
                bool lots = false;
                for (int i = 0; i < serializedObject.targetObjects.Length; i++)
                {
                    dyn = serializedObject.targetObjects[i] as DynamicObject;
                    if (dyn)
                    {
                        if (EditorCore.GetPreferences().DynamicObjectSearchInParent)
                        {
                            if (!dyn.GetComponentInChildren<Collider>())
                            {
                                missingCollider++;
                                if (missingCollider > 25)
                                {
                                    lots = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (!dyn.GetComponent<Collider>())
                            {
                                missingCollider++;
                                if (missingCollider > 25)
                                {
                                    lots = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (lots)
                {
                    EditorGUILayout.HelpBox("Lots of objects requires a collider to Track Gaze!", MessageType.Warning);
                }
                else if (missingCollider == 1)
                {
                    EditorGUILayout.HelpBox("This object requires a collider to Track Gaze!", MessageType.Warning);
                }
                else if (missingCollider > 1)
                {
                    EditorGUILayout.HelpBox(missingCollider + " objects requires a collider to Track Gaze!", MessageType.Warning);
                }
            }
        }

        void CheckCustomId(SerializedProperty customId)
        {
            if (Application.isPlaying) { return; }

            HashSet<string> usedids = new HashSet<string>();

            var dynamics = FindObjectsOfType<DynamicObject>();

            for (int i = dynamics.Length - 1; i >= 0; i--) //should adjust newer dynamics instead of older
            {
                if (dynamics[i].UseCustomId == false) { continue; }
                if (usedids.Contains(dynamics[i].CustomId) || string.IsNullOrEmpty(dynamics[i].CustomId))
                {
                    string s = System.Guid.NewGuid().ToString();
                    customId.stringValue = s;
                    dynamics[i].CustomId = s;
                    usedids.Add(s);
                    Util.logDebug(dynamics[i].gameObject.name + " has same customid, set new guid " + s);
                }
                else
                {
                    usedids.Add(dynamics[i].CustomId);
                }
            }
        }
    }
}
 