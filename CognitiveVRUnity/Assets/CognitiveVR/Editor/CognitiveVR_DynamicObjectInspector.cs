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
        public void OnEnable()
        {
            PrefabUtility.prefabInstanceUpdated += PrefabInstanceUpdated;
        }

        void PrefabInstanceUpdated(GameObject instance)
        {
            var dynamic = instance.GetComponent<DynamicObject>();
            if (dynamic == null) { return; }
            dynamic.editorInstanceId = 0;
            if (dynamic.editorInstanceId != dynamic.GetInstanceID() || string.IsNullOrEmpty(dynamic.CustomId))
            {
                if (dynamic.UseCustomId)
                {
                    dynamic.editorInstanceId = dynamic.GetInstanceID();
                    CheckCustomId(ref dynamic.CustomId);
                }
            }
        }

        public void OnDisable()
        {
            PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdated;
        }

        static bool foldout = false;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var script = serializedObject.FindProperty("m_Script");
            var groupName = serializedObject.FindProperty("GroupName");
            var syncWithPlayerUpdate = serializedObject.FindProperty("SyncWithPlayerUpdate");
            var updateRate = serializedObject.FindProperty("UpdateRate");
            var positionThreshold = serializedObject.FindProperty("PositionThreshold");
            var rotationThreshold = serializedObject.FindProperty("RotationThreshold");
            //var snapshotOnEnable = serializedObject.FindProperty("SnapshotOnEnable");
            var continuallyUpdateTransform = serializedObject.FindProperty("ContinuallyUpdateTransform");
            var releaseOnDisable = serializedObject.FindProperty("ReleaseIdOnDisable");
            var releaseOnDestroy = serializedObject.FindProperty("ReleaseIdOnDestroy");
            var useCustomID = serializedObject.FindProperty("UseCustomId");
            var customId = serializedObject.FindProperty("CustomId");
            var commonMeshName = serializedObject.FindProperty("CommonMesh");
            var meshname = serializedObject.FindProperty("MeshName");
            var useCustomMesh = serializedObject.FindProperty("UseCustomMesh");
            var trackGaze = serializedObject.FindProperty("TrackGaze");
            var requiresManualEnable = serializedObject.FindProperty("RequiresManualEnable");

            foreach(var t in serializedObject.targetObjects)
            {
                var dynamic = t as DynamicObject;
                if (dynamic.editorInstanceId != dynamic.GetInstanceID() || string.IsNullOrEmpty(dynamic.CustomId)) //only check if something has changed on a dynamic
                {
                    if (dynamic.UseCustomId)
                    {
                        dynamic.editorInstanceId = dynamic.GetInstanceID(); //this will often mark the scene dirty without any apparent or meaningful changes
                        CheckCustomId(ref dynamic.CustomId);
                        //TODO cache while scene active, but don't bother marking scene dirty if only editorInstanceId is dirty
                    }
                }
            }
            

#if UNITY_5_6_OR_NEWER
            //video
            //var flipVideo = serializedObject.FindProperty("FlipVideo");
            //var externalVideoSource = serializedObject.FindProperty("ExternalVideoSource");
            var videoPlayer = serializedObject.FindProperty("VideoPlayer");
#endif

            //display script on component
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            GUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.PropertyField(useCustomMesh);

            bool anycustomnames = false;
            foreach (var t in targets)
            {
                var dyn = t as DynamicObject;
                if (dyn.UseCustomMesh)
                {
                    anycustomnames = true;
                    if (string.IsNullOrEmpty(dyn.MeshName))
                    {
                        dyn.MeshName = dyn.gameObject.name.ToLower().Replace(" ", "_");
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                    if (targets.Length == 1)
                    dyn.MeshName = UnityEditor.EditorGUILayout.TextField("", dyn.MeshName);
                }
            }
            if (!anycustomnames)
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

                if (useCustomMesh.boolValue)
                {
                    //Mesh
                    GUILayout.Label("Mesh", EditorStyles.boldLabel);


                    //EditorGUI.BeginDisabledGroup(useCustomMesh.boolValue);
                    //UnityEditor.EditorGUILayout.PropertyField(commonMeshName);
                    //EditorGUI.EndDisabledGroup();

                    if (string.IsNullOrEmpty(meshname.stringValue))
                    {
                        //meshname.stringValue = serializedObject.targetObject.name.ToLower().Replace(" ", "_");
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }

                    GUILayout.BeginHorizontal();
                    //UnityEditor.EditorGUILayout.PropertyField(useCustomMesh);

                    //EditorGUI.BeginDisabledGroup(!useCustomMesh.boolValue);
                    //UnityEditor.EditorGUILayout.PropertyField(meshname, new GUIContent(""));
                    if (GUILayout.Button("Export Mesh", "ButtonLeft",GUILayout.Height(30)))
                    {
                        CognitiveVR_SceneExportWindow.ExportSelectedObjectsPrefab();
                        foreach (var t in serializedObject.targetObjects)
                        {
                            var dyn = t as DynamicObject;
                            //if (!dyn.UseCustomId) //why should this skip saving a snapshot if customid is not set
                            {
                                EditorCore.SaveDynamicThumbnailAutomatic(dyn.gameObject);
                            }
                        }
                    }

                    EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                    if (GUILayout.Button("Thumbnail from\nSceneView", "ButtonMid", GUILayout.Height(30)))
                    {
                        foreach (var v in serializedObject.targetObjects)
                        {
                            EditorCore.SaveDynamicThumbnailSceneView((v as DynamicObject).gameObject);
                        }
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                    if (GUILayout.Button("Upload Mesh", "ButtonRight", GUILayout.Height(30)))
                    {
                        CognitiveVR_SceneExportWindow.UploadSelectedDynamicObjects(true);
                    }
                    EditorGUI.EndDisabledGroup();

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    //GUILayout.FlexibleSpace();

                    GUILayout.EndHorizontal();
                }

                //Setup
                //GUILayout.Space(5);
                //GUILayout.Label("Setup", EditorStyles.boldLabel);

                //UnityEditor.EditorGUILayout.PropertyField(snapshotOnEnable, new GUIContent("Snapshot On Enable", "Save the transform when this object is first enabled"));

                


                //Object ID
                GUILayout.Space(5);
                GUILayout.Label("Ids", EditorStyles.boldLabel);

                GUILayout.BeginHorizontal();

                EditorGUILayout.PropertyField(useCustomID, new GUIContent("Use Custom Id", "This is used to identify specific objects to aggregate the position across multiple play sessions"));

                EditorGUI.BeginDisabledGroup(!useCustomID.boolValue);
                EditorGUILayout.PropertyField(customId, new GUIContent(""));
                EditorGUI.EndDisabledGroup();

                GUILayout.EndHorizontal();

                EditorGUILayout.PropertyField(groupName, new GUIContent("Group Name", "This is used to identify types of objects and combine aggregated data"));

                EditorGUILayout.PropertyField(releaseOnDisable, new GUIContent("Release Id OnDisable", "Allow other objects to use this Id when this object is no longer active"));
                EditorGUILayout.PropertyField(releaseOnDestroy, new GUIContent("Release Id OnDestroy", "Allow other objects to use this Id when this object no longer exists"));

                //Snapshot Threshold
                GUILayout.Space(5);
                GUILayout.Label("Snapshot", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(requiresManualEnable, new GUIContent("Requires Manual Enable", "If true, ManualEnable must be called before OnEnable will function. Used to set initial variables on an object"));

                //EditorGUI.BeginDisabledGroup(!snapshotOnEnable.boolValue);
                UnityEditor.EditorGUILayout.PropertyField(continuallyUpdateTransform, new GUIContent("Continually Record Transform", "Continually records the transform of this object at an interval"));
                //EditorGUI.EndDisabledGroup();

                //EditorGUILayout.PropertyField(trackGaze, new GUIContent("Track Gaze on Dynamic Object"));

                //DisplayGazeTrackHelpbox(trackGaze.boolValue);

                EditorGUI.BeginDisabledGroup(!continuallyUpdateTransform.boolValue);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(syncWithPlayerUpdate, new GUIContent("Sync with Player Update", "This is the Snapshot interval in the Tracker Options Window"));

                if (!syncWithPlayerUpdate.boolValue) //custom interval
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Update Rate",GUILayout.MaxWidth(100));
                    //EditorGUILayout.FloatField(0.5f, GUILayout.MinWidth(50));
                    EditorGUILayout.PropertyField(updateRate, new GUIContent("", "This is the Snapshot interval in the Tracker Options Window"), GUILayout.MinWidth(50));
                    EditorGUILayout.EndHorizontal();
                }
                else //player interval
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("Update Rate", GUILayout.MaxWidth(100));
                    EditorGUILayout.FloatField(EditorCore.GetPreferences().SnapshotInterval, GUILayout.MinWidth(50));
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.EndHorizontal();
                
                updateRate.floatValue = Mathf.Max(0.1f, updateRate.floatValue);


                EditorGUILayout.PropertyField(positionThreshold, new GUIContent("Position Threshold", "Meters the object must move to write a new snapshot. Checked each 'Tick'"));
                positionThreshold.floatValue = Mathf.Max(0, positionThreshold.floatValue);

                EditorGUILayout.PropertyField(rotationThreshold, new GUIContent("Rotation Threshold", "Degrees the object must rotate to write a new snapshot. Checked each 'Tick'"));
                rotationThreshold.floatValue = Mathf.Max(0, rotationThreshold.floatValue);

                EditorGUI.EndDisabledGroup();

#if !UNITY_5_6_OR_NEWER
            GUILayout.Label("Video Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Video Player requires Unity 5.6 or newer!", MessageType.Warning);
#else
                GUILayout.Space(5);
                GUILayout.Label("Video Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(videoPlayer, new GUIContent("Video Player"));

                //EditorGUILayout.PropertyField(externalVideoSource, new GUIContent("External Video Source", "The URL source of the video"));
                //EditorGUILayout.PropertyField(flipVideo, new GUIContent("Flip Video Horizontally"));
#endif
            } //advanced foldout


            if (GUI.changed)
            {
                foreach (var t in targets)
                {
                    var dyn = t as DynamicObject;
                    if (dyn.UseCustomMesh)
                    {
                        dyn.MeshName = dyn.MeshName.Replace(" ", "_");
                    }
                }

                //remove spaces from meshname
                //meshname.stringValue = meshname.stringValue.Replace(" ", "_");

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

        void CheckCustomId(ref string customId)
        {
            if (Application.isPlaying) { return; }

            HashSet<string> usedids = new HashSet<string>();

            var dynamics = FindObjectsOfType<DynamicObject>();

            for (int i = dynamics.Length - 1; i >= 0; i--) //loop backwards to adjust newest dynamics instead of oldest
            {
                if (dynamics[i].UseCustomId == false) { continue; }
                if (usedids.Contains(dynamics[i].CustomId) || string.IsNullOrEmpty(dynamics[i].CustomId))
                {
                    string s = System.Guid.NewGuid().ToString();
                    customId = "editor_" + s;
                    dynamics[i].CustomId = customId;
                    usedids.Add(customId);
                    Util.logDebug(dynamics[i].gameObject.name + " has same customid, set new guid " + customId);
                }
                else
                {
                    usedids.Add(dynamics[i].CustomId);
                }
            }
        }
    }
}
 