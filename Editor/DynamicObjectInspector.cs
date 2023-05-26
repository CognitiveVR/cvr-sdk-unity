using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

namespace Cognitive3D
{
    [CustomEditor(typeof(Cognitive3D.DynamicObject))]
    [CanEditMultipleObjects]
    public class DynamicObjectInspector : Editor
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
            
            bool basicGUIChanged = false;
            EditorGUI.BeginChangeCheck();

            //consider adding an actual property for id source - instead of infering from useCustomId and objectPool references. should simplify property display in inspector
            var script = serializedObject.FindProperty("m_Script");
            var updateRate = serializedObject.FindProperty("UpdateRate");
            var positionThreshold = serializedObject.FindProperty("PositionThreshold");
            var rotationThreshold = serializedObject.FindProperty("RotationThreshold");
            var scaleThreshold = serializedObject.FindProperty("ScaleThreshold");
            var useCustomId = serializedObject.FindProperty("UseCustomId");
            var customId = serializedObject.FindProperty("CustomId");
            //var commonMeshName = serializedObject.FindProperty("CommonMesh");
            var meshname = serializedObject.FindProperty("MeshName");
            //var useCustomMesh = serializedObject.FindProperty("UseCustomMesh");
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
                                dynamic.CustomId = s;
                            }
                        }
                    }
                }
            }

            //display script on component
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(script, true);
            EditorGUI.EndDisabledGroup();

            //use custom mesh and mesh text field
            if (targets.Length == 1)
            {
                var dyn = targets[0] as DynamicObject;
                if (!dyn.UseCustomMesh)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(new GUIContent("Mesh Name"), "Generated at Runtime");
                    EditorGUI.EndDisabledGroup();
                }
                else
                {
                    EditorGUILayout.PropertyField(meshname, new GUIContent("Mesh Name"));
                    dyn.MeshName = ValidateMeshName(dyn.MeshName);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(meshname, new GUIContent("Mesh Name"));
                foreach (var t in targets)
                {
                    var dyn = t as DynamicObject;
                    dyn.MeshName = ValidateMeshName(dyn.MeshName);
                }
            }

            int targetIdType = -1;
            bool allSelectedShareIdType = true;

            //dynamic id sources - custom id, generate at runtime, id pool asset
            var primaryDynamic = targets[0] as DynamicObject;
            if (primaryDynamic.UseCustomId)
            {
                targetIdType = 0;
                idType = 0;
            }
            else if (primaryDynamic.IdPool != null)
            {
                targetIdType = 2;
                idType = 2;
            }
            else
            {
                targetIdType = 1;
                idType = 1;
            }

            //check if all selected objects have the same idtype
            foreach (var t in targets)
            {
                var tdyn = (DynamicObject)t;
                if (tdyn.UseCustomId) { if (targetIdType != 0) { allSelectedShareIdType = false; break; } }
                else if (tdyn.IdPool != null) {if (targetIdType != 2) { allSelectedShareIdType = false; break; } }
                else if (targetIdType != 1) { allSelectedShareIdType = false; break; }
            }

            //if all id sources from selected objects are the same, display shared property fields
            //otherwise display 'multiple values'
            if (!allSelectedShareIdType)
            {
                EditorGUILayout.LabelField("Id Source", "Multiple Values");
            }
            else
            {
                GUILayout.BeginHorizontal();
                idType = EditorGUILayout.Popup(new GUIContent("Id Source"), idType, idTypeNames);

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
                        if (GUILayout.Button("New Dynamic Object Id Pool")) //this can overwrite an existing id pool with the same name. should this just find a pool?
                        {
                            string poolMeshName = dyn.MeshName;
                            string assetPath = "Assets/" + poolMeshName + " Id Pool.asset";

                            //check if asset exists
                            var foundPool = (DynamicObjectIdPool)AssetDatabase.LoadAssetAtPath(assetPath, typeof(DynamicObjectIdPool));
                            if (foundPool == null)
                            {
                                var pool = GenerateNewIDPoolAsset(assetPath, dyn);
                                idPool.objectReferenceValue = pool;
                            }
                            else
                            {
                                //popup - new pool asset, add to existing pool (if matching mesh name), cancel
                                int result = EditorUtility.DisplayDialogComplex("Found Id Pool", "An existing Id Pool with the same mesh name was found. Do you want to use this Id Pool instead?", "New Asset", "Cancel", "Use Existing");

                                if (result == 0)//new asset with unique name
                                {
                                    string finalAssetPath = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(assetPath);
                                    var pool = GenerateNewIDPoolAsset(finalAssetPath, dyn);
                                    idPool.objectReferenceValue = pool;
                                }
                                if (result == 2) //reference existing pool
                                {
                                    idPool.objectReferenceValue = foundPool;
                                }
                                if (result == 1) { }//cancel
                            }
                        }
                    }
                }
            }

            basicGUIChanged = EditorGUI.EndChangeCheck();


            foldout = EditorGUILayout.Foldout(foldout, "Advanced");
            bool advancedGUIChanged = false;
            EditorGUI.BeginChangeCheck();
            if (foldout)
            {
                //Export Button
                GUILayout.Label("Export and Upload", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Export Mesh", "ButtonLeft", GUILayout.Height(30)))
                {
                    ExportUtility.ExportAllSelectedDynamicObjects();
                }

                //Thumbnail Button
                EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                if (GUILayout.Button("Save Thumbnail\nfrom SceneView", "ButtonMid", GUILayout.Height(30)))
                {
                    foreach (var v in serializedObject.targetObjects)
                    {
                        EditorCore.SaveDynamicThumbnailSceneView((v as DynamicObject).gameObject);
                    }
                }
                EditorGUI.EndDisabledGroup();

                //Upload Button
                EditorGUI.BeginDisabledGroup(!EditorCore.HasDynamicExportFiles(meshname.stringValue));
                if (GUILayout.Button("Upload Mesh", "ButtonRight", GUILayout.Height(30)))
                {
                    List<GameObject> uploadList = new List<GameObject>(Selection.gameObjects);
                    ExportUtility.UploadSelectedDynamicObjectMeshes(uploadList, true);
                    UploadCustomIdForAggregation();
                }
                EditorGUI.EndDisabledGroup();

                //texture export settings
                GUILayout.EndHorizontal();
                GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter")/*, new GUIContent("Eighth"), new GUIContent("Sixteenth"), new GUIContent("Thirty Second"), new GUIContent("Sixty Fourth") */};
                int[] textureQualities = new int[] { 1, 2, 4/*, 8, 16, 32, 64*/ };
                Cognitive3D_Preferences.Instance.TextureResize = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), Cognitive3D_Preferences.Instance.TextureResize, textureQualityNames, textureQualities);
                GUILayout.Space(5);

                //Controller Settings
                if (targets.Length == 1)
                {
                    GUILayout.Label("Controller Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;
                    var dyn = targets[0] as DynamicObject;
                    dyn.IsController = EditorGUILayout.Toggle(new GUIContent("Is Controller","Visualize on SceneExplorer with a common mesh.\nInclude metadata to display button inputs."),dyn.IsController);
                    EditorGUI.BeginDisabledGroup(!dyn.IsController);
                    dyn.IsRight = EditorGUILayout.Toggle("Is Right Hand",dyn.IsRight);
                    dyn.IdentifyControllerAtRuntime = EditorGUILayout.Toggle(new GUIContent("Identify Controller at Runtime","Use Unity's API to try to identify the InputDevice name"), dyn.IdentifyControllerAtRuntime);
                    dyn.FallbackControllerType = (DynamicObject.ControllerType)EditorGUILayout.EnumPopup(new GUIContent("Fallback Controller Type","Used if this controller cannot be identified at runtime"),dyn.FallbackControllerType);
                    EditorGUI.EndDisabledGroup();
                    EditorGUI.indentLevel--;
                }

                //Snapshot Threshold
                GUILayout.Label("Data Snapshot", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(syncWithGaze, new GUIContent("Sync with Gaze", "Records the transform of the Dynamic Object on the same frame as gaze. This will smooth the movement of this object in SceneExplorer relative to the player"));
                EditorGUI.BeginDisabledGroup(syncWithGaze.boolValue);
                EditorGUILayout.PropertyField(updateRate, new GUIContent("Update Rate", "This indicates the time interval to check if this Dynamic Object has moved"), GUILayout.MinWidth(50));
                updateRate.floatValue = Mathf.Max(0.1f, updateRate.floatValue);
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.PropertyField(positionThreshold, new GUIContent("Position Threshold", "Meters the object must move to write a new snapshot"));
                positionThreshold.floatValue = Mathf.Max(0, positionThreshold.floatValue);

                EditorGUILayout.PropertyField(rotationThreshold, new GUIContent("Rotation Threshold", "Degrees the object must rotate to write a new snapshot"));
                rotationThreshold.floatValue = Mathf.Max(0, rotationThreshold.floatValue);

                EditorGUILayout.PropertyField(scaleThreshold, new GUIContent("Scale Threshold", "Scale multiplier that must be exceeded to write a new snapshot"));
                scaleThreshold.floatValue = Mathf.Max(0, scaleThreshold.floatValue);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            } //advanced foldout

            advancedGUIChanged = EditorGUI.EndChangeCheck();

            if (basicGUIChanged || advancedGUIChanged)
            {
                foreach (var t in targets)
                {
                    var dyn = t as DynamicObject;
                    if (dyn.UseCustomMesh)
                    {
                        dyn.MeshName = ValidateMeshName(dyn.MeshName);
                    }
                }
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
        }

        void UploadCustomIdForAggregation()
        {
            var dyn = target as DynamicObject;
            if (dyn.UseCustomId)
            {
                Debug.Log("Cognitive3D Dynamic Object: upload custom id to scene");
                EditorCore.RefreshSceneVersion(delegate ()
                {
                    AggregationManifest manifest = new AggregationManifest();
                    manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dyn.gameObject.name, dyn.MeshName, dyn.CustomId,
                        new float[3] { dyn.transform.lossyScale.x, dyn.transform.lossyScale.y, dyn.transform.lossyScale.z },
                        new float[3] { dyn.transform.position.x, dyn.transform.position.y, dyn.transform.position.z },
                        new float[4] { dyn.transform.rotation.x, dyn.transform.rotation.y, dyn.transform.rotation.z, dyn.transform.rotation.w }));
                    DynamicObjectsWindow.UploadManifest(manifest, null);
                });
            }
            else if (dyn.IdPool != null)
            {
                Debug.Log("Cognitive3D Dynamic Object: Upload id pool to scene");
                EditorCore.RefreshSceneVersion(delegate ()
                {
                    AggregationManifest manifest = new AggregationManifest();
                    for (int i = 0; i < dyn.IdPool.Ids.Length; i++)
                    {
                        manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(dyn.gameObject.name, dyn.MeshName, dyn.IdPool.Ids[i],
                            new float[3] { dyn.transform.lossyScale.x, dyn.transform.lossyScale.y, dyn.transform.lossyScale.z },
                            new float[3] { dyn.transform.position.x, dyn.transform.position.y, dyn.transform.position.z },
                            new float[4] { dyn.transform.rotation.x, dyn.transform.rotation.y, dyn.transform.rotation.z, dyn.transform.rotation.w }));
                    }
                    DynamicObjectsWindow.UploadManifest(manifest, null);
                });
            }
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
                    string guid = System.Guid.NewGuid().ToString();
                    dynamics[i].CustomId = guid;
                    usedids.Add(guid);
                    Util.logDebug(dynamics[i].gameObject.name + " has same customid, set new guid " + guid);
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

        DynamicObjectIdPool GenerateNewIDPoolAsset(string assetPath, DynamicObject dynamic)
        {
            var pool = ScriptableObject.CreateInstance<DynamicObjectIdPool>();
            pool.Ids = new string[1] { System.Guid.NewGuid().ToString() };
            pool.MeshName = dynamic.MeshName;
            pool.PrefabName = dynamic.gameObject.name;
            AssetDatabase.CreateAsset(pool, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return pool;
        }
    }
}
 