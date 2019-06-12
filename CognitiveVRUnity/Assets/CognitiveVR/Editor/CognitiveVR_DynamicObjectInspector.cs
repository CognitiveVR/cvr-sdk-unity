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
                if (!string.IsNullOrEmpty(dynamic.CustomId))
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
            var updateRate = serializedObject.FindProperty("UpdateRate");
            var positionThreshold = serializedObject.FindProperty("PositionThreshold");
            var rotationThreshold = serializedObject.FindProperty("RotationThreshold");
            var scaleThreshold = serializedObject.FindProperty("ScaleThreshold");
            var useCustomId = serializedObject.FindProperty("UseCustomId");
            var customId = serializedObject.FindProperty("CustomId");
            var commonMeshName = serializedObject.FindProperty("CommonMesh");
            var meshname = serializedObject.FindProperty("MeshName");
            var useCustomMesh = serializedObject.FindProperty("UseCustomMesh");
            var isController = serializedObject.FindProperty("IsController");
            var syncWithGaze = serializedObject.FindProperty("SyncWithPlayerGazeTick");

            foreach (var t in serializedObject.targetObjects) //makes sure a custom id is valid
            {
                var dynamic = t as DynamicObject;
                if (dynamic.editorInstanceId != dynamic.GetInstanceID() || string.IsNullOrEmpty(dynamic.CustomId)) //only check if something has changed on a dynamic, or if the id is empty
                {
                    if (dynamic.UseCustomId)
                    {
                        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(dynamic.gameObject)))//scene asset
                        {
                            dynamic.editorInstanceId = dynamic.GetInstanceID();
                            CheckCustomId(ref dynamic.CustomId);
                        }
                        else //project asset
                        {
                            dynamic.editorInstanceId = dynamic.GetInstanceID();
                            if (string.IsNullOrEmpty(dynamic.CustomId))
                            {
                                string s = System.Guid.NewGuid().ToString();
                                dynamic.CustomId = "editor_" + s;
                            }
                        }
                    }
                }
            }

            //video
            //var flipVideo = serializedObject.FindProperty("FlipVideo");
            //var externalVideoSource = serializedObject.FindProperty("ExternalVideoSource");
            //var videoPlayer = serializedObject.FindProperty("VideoPlayer");

            //display script on component
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true, new GUILayoutOption[0]);
            EditorGUI.EndDisabledGroup();

            //use custom mesh and mesh text field
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
                        dyn.MeshName = dyn.gameObject.name.ToLower().Replace(" ", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\"", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                        if (!Application.isPlaying)
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


            //use custom id and custom id text field
            GUILayout.BeginHorizontal();
            bool previousUseCustomId = useCustomId.boolValue;
            EditorGUILayout.PropertyField(useCustomId, new GUIContent("Custom Id"));

            if (Selection.activeGameObject)
            {
                string path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(path) && useCustomId.boolValue)
                {
                    //display small warning icon when prefab has custom id set
                    EditorGUILayout.LabelField(new GUIContent(EditorCore.Alert,"Project assets should not have CustomId set, unless there will be only 1 instance spawned during runtime"), GUILayout.Width(20));
                }
            }

            if (previousUseCustomId != useCustomId.boolValue)
            {
                if (previousUseCustomId != useCustomId.boolValue) //use custom id changed
                {
                    if (useCustomId.boolValue == false) //changed to false
                    {
                        customId.stringValue = string.Empty;
                    }
                    else
                    {
                        foreach (var t in targets)
                        {
                            var dyn = t as DynamicObject;
                            CheckCustomId(ref dyn.CustomId);
                        }
                    }
                }
            }

            if (!useCustomId.boolValue) //display custom id field
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(new GUIContent("Id will be generated at runtime","This object will not be included in aggregation metrics on the dashboard"));
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                if (targets.Length > 1)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.LabelField("multiple values");
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.PropertyField(customId, new GUIContent(""));
                }
            }            
            GUILayout.EndHorizontal();



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
                        if (!Application.isPlaying)
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }

                    GUILayout.BeginHorizontal();
                    //UnityEditor.EditorGUILayout.PropertyField(useCustomMesh);

                    //EditorGUI.BeginDisabledGroup(!useCustomMesh.boolValue);
                    //UnityEditor.EditorGUILayout.PropertyField(meshname, new GUIContent(""));
                    if (GUILayout.Button("Export Mesh", "ButtonLeft",GUILayout.Height(30)))
                    {
                        CognitiveVR_SceneExportWindow.ExportSelectedObjectsPrefab();
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

                    //GUILayout.BeginHorizontal();


                    GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter"), new GUIContent("Eighth"), new GUIContent("Sixteenth"), new GUIContent("Thirty Second"), new GUIContent("Sixty Fourth") };
                    int[] textureQualities = new int[] { 1, 2, 4, 8, 16, 32, 64 };
                    CognitiveVR_Preferences.Instance.TextureResize = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), CognitiveVR_Preferences.Instance.TextureResize, textureQualityNames, textureQualities);


                    //GUILayout.EndHorizontal();
                    GUILayout.Space(5);
                }

                //Snapshot Threshold
                
                GUILayout.Label("Snapshot", EditorStyles.boldLabel);

                //controller stuff
                GUILayout.BeginHorizontal();

                UnityEditor.EditorGUILayout.PropertyField(isController, new GUIContent("Is Controller", "If true, this will record user's inputs and display the inputs in a popup on SceneExplorer"));

                if (targets.Length == 1)
                {

                    var dyn = targets[0] as DynamicObject;

                    if (dyn.IsController)
                    {
                        string[] controllernames = new string[3] { "vivecontroller", "oculustouchleft", "oculustouchright" };
                        int selected = 0;
                        if (dyn.ControllerType == "vivecontroller") selected = 0;
                        if (dyn.ControllerType == "oculustouchleft") selected = 1;
                        if (dyn.ControllerType == "oculustouchright") selected = 2;

                        selected = EditorGUILayout.Popup(selected, controllernames);
                        dyn.ControllerType = controllernames[selected];
                    }

                    if (dyn.IsController)
                    {
                        EditorGUILayout.LabelField("Is Right", GUILayout.Width(60));
                        dyn.IsRight = EditorGUILayout.Toggle(dyn.IsRight, GUILayout.Width(20));
                    }
                }

                GUILayout.EndHorizontal();


                EditorGUILayout.PropertyField(syncWithGaze, new GUIContent("Sync with Gaze", "Records the transform of the dynamic object on the same frame as gaze. This may smooth movement of this object in SceneExplorer relative to the player's position"));
                EditorGUI.BeginDisabledGroup(syncWithGaze.boolValue);
                EditorGUILayout.PropertyField(updateRate, new GUIContent("Update Rate", "This is the Snapshot interval in the Tracker Options Window"), GUILayout.MinWidth(50));
                updateRate.floatValue = Mathf.Max(0.1f, updateRate.floatValue);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(positionThreshold, new GUIContent("Position Threshold", "Meters the object must move to write a new snapshot. Checked each 'Tick'"));
                positionThreshold.floatValue = Mathf.Max(0, positionThreshold.floatValue);

                EditorGUILayout.PropertyField(rotationThreshold, new GUIContent("Rotation Threshold", "Degrees the object must rotate to write a new snapshot. Checked each 'Tick'"));
                rotationThreshold.floatValue = Mathf.Max(0, rotationThreshold.floatValue);

                EditorGUILayout.PropertyField(scaleThreshold, new GUIContent("Scale Threshold", "Scale multiplier that must be exceeded to write a new snapshot. Checked each 'Tick'"));
                scaleThreshold.floatValue = Mathf.Max(0, scaleThreshold.floatValue);

                EditorGUI.EndDisabledGroup();
            } //advanced foldout


            if (GUI.changed)
            {
                foreach (var t in targets)
                {
                    var dyn = t as DynamicObject;
                    if (dyn.UseCustomMesh)
                    {
                        //TODO replace all invalid characters <>|?*"/\: with _
                        dyn.MeshName = dyn.MeshName.Replace(" ", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\"", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_");
                    }
                }

                //remove spaces from meshname
                //meshname.stringValue = meshname.stringValue.Replace(" ", "_");

                if (!Application.isPlaying)
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
 