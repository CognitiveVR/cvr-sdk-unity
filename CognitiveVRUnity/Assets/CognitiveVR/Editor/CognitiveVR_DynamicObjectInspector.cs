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
                    CheckCustomId();
                }
            }
        }

        public void OnDisable()
        {
            PrefabUtility.prefabInstanceUpdated -= PrefabInstanceUpdated;
        }

        int idType = -1;
        GUIContent[] idTypeNames = new GUIContent[] {
            new GUIContent("Custom Id", "For objects that start in the scene"),
            new GUIContent("Generate Id", "For spawned objects that DO NOT need aggregate data"),
            new GUIContent("Pool Id", "For spawned objects that need aggregate data")
        };

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
            var idPool = serializedObject.FindProperty("IdPool");

            foreach (var t in serializedObject.targetObjects) //makes sure a custom id is valid
            {
                var dynamic = t as DynamicObject;
                if (dynamic.editorInstanceId != dynamic.GetInstanceID() || string.IsNullOrEmpty(dynamic.CustomId)) //only check if something has changed on a dynamic, or if the id is empty
                {
                    if (dynamic.UseCustomId && idType == 0)
                    {
                        if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(dynamic.gameObject)))//scene asset
                        {
                            dynamic.editorInstanceId = dynamic.GetInstanceID();
                            CheckCustomId();
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
                        dyn.MeshName = ValidateMeshName(dyn.MeshName);
                        if (!Application.isPlaying)
                        {
                            UnityEditor.EditorUtility.SetDirty(dyn);
                            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                        }
                    }
                }
            }
            if (!anycustomnames)
            {
                UnityEditor.EditorGUILayout.PropertyField(commonMeshName, new GUIContent(""));
            }
            else //mesh names
            {
                UnityEditor.EditorGUILayout.PropertyField(meshname, new GUIContent(""));
                meshname.stringValue = ValidateMeshName(meshname.stringValue);
            }
            GUILayout.EndHorizontal();

            if (idType == -1)
            {
                var dyn = target as DynamicObject;
                if (dyn.UseCustomId) idType = 0;
                else if (dyn.IdPool != null) idType = 2;
                else idType = 1;
            }
            GUILayout.BeginHorizontal();
            idType = EditorGUILayout.Popup(new GUIContent("Id Source"),idType, idTypeNames);

            if (idType == 0) //custom id
            {
                EditorGUILayout.PropertyField(customId, new GUIContent(""));
                useCustomId.boolValue = true;
                idPool.objectReferenceValue = null;
            }
            else if (idType == 1) //generate id
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.LabelField(new GUIContent("Id will be generated at runtime", "This object will not be included in aggregation metrics on the dashboard"));
                EditorGUI.EndDisabledGroup();
                customId.stringValue = string.Empty;
                useCustomId.boolValue = false;
                idPool.objectReferenceValue = null;
            }
            else if (idType == 2) //id pool
            {
                EditorGUILayout.ObjectField(idPool, new GUIContent("", "Provides a consistent list of Ids to be used at runtime. Allows aggregated data from objects spawned at runtime"));
                customId.stringValue = string.Empty;
                useCustomId.boolValue = false;
            }
            GUILayout.EndHorizontal();

            if (idType == 2) //id pool
            {
                var dyn = target as DynamicObject;
                if (dyn.IdPool == null)
                {
                    if (GUILayout.Button("New Dynamic Object Id Pool"))
                    {
                        var pool = ScriptableObject.CreateInstance<DynamicObjectIdPool>();
                        //write some values
                        pool.Ids = new string[1] { System.Guid.NewGuid().ToString() };
                        pool.MeshName = dyn.MeshName;
                        pool.PrefabName = dyn.gameObject.name;
                        //save to root assets folder
                        AssetDatabase.CreateAsset(pool, "Assets/" + pool.MeshName + " Id Pool.asset");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                        //get reference to file
                        idPool.objectReferenceValue = pool;
                    }
                }
            }

            foldout = EditorGUILayout.Foldout(foldout, "Advanced");
            if (foldout)
            {
                if (useCustomMesh.boolValue)
                {
                    //Mesh
                    GUILayout.Label("Export and Upload", EditorStyles.boldLabel);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("Export Mesh", "ButtonLeft",GUILayout.Height(30)))
                    {
                        ExportUtility.ExportAllSelectedDynamicObjects();
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
                        ExportUtility.UploadSelectedDynamicObjectMeshes(true);
                    }
                    EditorGUI.EndDisabledGroup();

                    //texture export settings
                    GUILayout.EndHorizontal();
                    GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter")/*, new GUIContent("Eighth"), new GUIContent("Sixteenth"), new GUIContent("Thirty Second"), new GUIContent("Sixty Fourth") */};
                    int[] textureQualities = new int[] { 1, 2, 4/*, 8, 16, 32, 64*/ };
                    CognitiveVR_Preferences.Instance.TextureResize = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), CognitiveVR_Preferences.Instance.TextureResize, textureQualityNames, textureQualities);
                    GUILayout.Space(5);

                    //ID upload
                    var dyn = target as DynamicObject;
                    if (dyn.UseCustomId)
                    {
                        EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                        if (GUILayout.Button("Upload Custom ID for aggregation"))
                        {
                            Debug.Log("upload custom id to scene");
                            //ExportUtility.UploadSelectedDynamicObjectMeshes(true);
                            EditorCore.RefreshSceneVersion(delegate ()
                            {
                                ManageDynamicObjects.AggregationManifest manifest = new ManageDynamicObjects.AggregationManifest();
                                manifest.objects.Add(new ManageDynamicObjects.AggregationManifest.AggregationManifestEntry(dyn.gameObject.name, dyn.MeshName, dyn.CustomId, dyn.transform.lossyScale.x));
                                ManageDynamicObjects.UploadManifest(manifest, null);
                            });
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    else if (dyn.IdPool != null)
                    {
                        EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                        if (GUILayout.Button("Upload ID Pool for aggregation"))
                        {
                            Debug.Log("upload id pool to scene");
                            //ExportUtility.UploadSelectedDynamicObjectMeshes(true);
                            EditorCore.RefreshSceneVersion(delegate ()
                            {
                                ManageDynamicObjects.AggregationManifest manifest = new ManageDynamicObjects.AggregationManifest();
                                for(int i = 0; i< dyn.IdPool.Ids.Length;i++)
                                {
                                    manifest.objects.Add(new ManageDynamicObjects.AggregationManifest.AggregationManifestEntry(dyn.gameObject.name, dyn.MeshName, dyn.IdPool.Ids[i], dyn.transform.lossyScale.x));
                                }
                                ManageDynamicObjects.UploadManifest(manifest, null);
                            });
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                }

                //Snapshot Threshold
                
                GUILayout.Label("Data Snapshot", EditorStyles.boldLabel);

                //controller stuff
                GUILayout.BeginHorizontal();

                UnityEditor.EditorGUILayout.PropertyField(isController, new GUIContent("Is Controller", "If true, this will record user's inputs and display the inputs in a popup on SceneExplorer"));

                if (targets.Length == 1)
                {

                    var dyn = targets[0] as DynamicObject;

                    if (dyn.IsController)
                    {
                        dyn.ControllerType = (DynamicObject.ControllerDisplayType)EditorGUILayout.EnumPopup(dyn.ControllerType);
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
                        dyn.MeshName = ValidateMeshName(dyn.MeshName);
                    }
                }

                //IMPROVEMENT should check that some meaningful property changed, not just foldout
                if (!Application.isPlaying)
                {
                    foreach (var t in targets)
                    {
                        EditorUtility.SetDirty(t);
                    }
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
            }

            serializedObject.ApplyModifiedProperties();
            serializedObject.Update();
        }

        void CheckCustomId()
        {
            if (Application.isPlaying) { return; }
            HashSet<string> usedids = new HashSet<string>();

            //check against all dynamic object id pool contents
            var pools = EditorCore.GetDynamicObjectPoolAssets;

            foreach(var pool in pools)
            {
                foreach(var id in pool.Ids)
                {
                    usedids.Add(id);
                }
            }
            
            var dynamics = FindObjectsOfType<DynamicObject>();

            for (int i = dynamics.Length - 1; i >= 0; i--) //loop backwards to adjust newest dynamics instead of oldest
            {
                if (!dynamics[i].UseCustomId) { continue; }

                if (usedids.Contains(dynamics[i].CustomId) || string.IsNullOrEmpty(dynamics[i].CustomId))
                {
                    string s = System.Guid.NewGuid().ToString();
                    var customId = "editor_" + s;
                    dynamics[i].CustomId = customId;
                    usedids.Add(customId);
                    Util.logDebug(dynamics[i].gameObject.name + " has same customid, set new guid " + customId);
                    if (!Application.isPlaying)
                    {
                        UnityEditor.EditorUtility.SetDirty(dynamics[i]);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                }
                else
                {
                    usedids.Add(dynamics[i].CustomId);
                }
            }
        }

        string ValidateMeshName(string input)
        {
            //TODO replace non-ascii characters
            string inputMod = input.Replace(" ", "_").Replace("<", "_").Replace(">", "_").Replace("|", "_").Replace("?", "_").Replace("*", "_").Replace("\"", "_").Replace("/", "_").Replace("\\", "_").Replace(":", "_").Replace("#", "_").Replace("[", "_").Replace("]", "_").Replace("%", "_").Replace("^", "_").Replace("$", "_");
            return inputMod;
        }
    }
}
 