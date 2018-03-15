using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class MenuItems
    {
        [MenuItem("cognitive3D/Add Cognitive Manager", priority = 0)]
        static void Cognitive3DManager()
        {
            var found = Object.FindObjectOfType<CognitiveVR_Manager>();
            if (found != null)
            {
                Selection.activeGameObject = found.gameObject;
                return;
            }
            else
            {
                //spawn prefab
                GameObject newManager = new GameObject("CognitiveVR_Manager");
                Selection.activeGameObject = newManager;
                Undo.RegisterCreatedObjectUndo(newManager, "Create CognitiveVR Manager");
                newManager.AddComponent<CognitiveVR_Manager>();
            }
        }

        [MenuItem("cognitive3D/Open Web Dashboard...", priority = 5)]
        static void Cognitive3DDashboard()
        {
            Application.OpenURL("http://dashboard.cognitivevr.io");
        }

        [MenuItem("cognitive3D/Check for Updates...", priority = 10)]
        static void CognitiveCheckUpdates()
        {
            EditorCore.ForceCheckUpdates();
            //Application.OpenURL("http://dashboard.cognitivevr.io");
        }

        [MenuItem("cognitive3D/Scene Setup", priority = 55)]
        static void Cognitive3DSceneSetup()
        {
            //open window
            InitWizard.Init();
        }

        [MenuItem("cognitive3D/Manage Dynamic Objects", priority = 60)]
        static void Cognitive3DManageDynamicObjects()
        {
            //open window
            ManageDynamicObjects.Init();
            //CognitiveVR_ObjectManifestWindow.Init();
        }

        [MenuItem("cognitive3D/Advanced Options", priority = 65)]
        static void Cognitive3DOptions()
        {
            //open window
            Selection.activeObject = EditorCore.GetPreferences();

            //CognitiveVR_ComponentSetup.Init();
            //CognitiveVR_Settings.Init();
        }


        //--------------old 



        [MenuItem("Window/cognitiveVR/Open Web Dashboard...", priority = 0)]
        static void CognitiveVRDashboard()
        {
            Application.OpenURL("http://dashboard.cognitivevr.io");
        }

        [MenuItem("Window/cognitiveVR/Account Settings Window", priority = 5)]
        static void CognitiveSettingsWindow()
        {
            CognitiveVR_Settings.Init();
        }

        [MenuItem("Window/cognitiveVR/Preferences Window", priority = 10)]
        static void CognitiveComponentWindow()
        {
            CognitiveVR_ComponentSetup.Init();
        }

        [MenuItem("Window/cognitiveVR/Scene Export Window", priority = 15)]
        static void CognitiveExportWindow()
        {
            CognitiveVR_SceneExportWindow.Init();
        }

        [MenuItem("Window/cognitiveVR/Add cognitiveVR Manager", priority = 55)]
        static void AddCognitiveVRManager()
        {
            CognitiveVR_ComponentSetup.AddCognitiveVRManager();
        }

        [MenuItem("Window/cognitiveVR/Export Selected Dynamic Objects",priority = 105)]
        public static void ExportSelectedObjectsPrefab()
        {
            List<Transform> entireSelection = new List<Transform>();
            entireSelection.AddRange(Selection.GetTransforms(SelectionMode.Editable));

            Debug.Log("Starting export of " + entireSelection.Count + " dynamic objects");

            List<Transform> sceneObjects = new List<Transform>();
            sceneObjects.AddRange(Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab));

            List<Transform> prefabsToSpawn = new List<Transform>();

            //add prefab objects to a list
            foreach (var v in entireSelection)
            {
                if (!sceneObjects.Contains(v))
                {
                    prefabsToSpawn.Add(v);
                }
            }

            //spawn prefabs
            var temporarySpawnedPrefabs = new List<GameObject>();
            foreach (var v in prefabsToSpawn)
            {
                var newPrefab = GameObject.Instantiate(v.gameObject);
                temporarySpawnedPrefabs.Add(newPrefab);
                sceneObjects.Add(newPrefab.transform);
            }

            //export all the objects
            int successfullyExportedCount = 0;
            List<string> exportedMeshNames = new List<string>();

            foreach (var v in sceneObjects)
            {
                var dynamic = v.GetComponent<DynamicObject>();
                if (dynamic == null) { continue; }
                if (v.GetComponent<Canvas>() != null)
                {
                    //TODO merge this deeper in the export process. do this recurively ignoring child dynamics
                    //take a snapshot
                    var width = v.GetComponent<RectTransform>().sizeDelta.x * v.localScale.x;
                    var height = v.GetComponent<RectTransform>().sizeDelta.y * v.localScale.y;

                    var screenshot = CognitiveVR_SceneExplorerExporter.Snapshot(v);

                    var mesh = CognitiveVR_SceneExplorerExporter.ExportQuad(dynamic.MeshName, width, height, v, UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name, screenshot);
                    CognitiveVR_SceneExplorerExporter.ExportDynamicObject(mesh, dynamic.MeshName, screenshot, dynamic.MeshName);
                    successfullyExportedCount++;
                }
                else if (CognitiveVR_SceneExplorerExporter.ExportDynamicObject(v))
                {
                    successfullyExportedCount++;
                }

                foreach (var common in System.Enum.GetNames(typeof(DynamicObject.CommonDynamicMesh)))
                {
                    if (common.ToLower() == dynamic.MeshName.ToLower())
                    {
                        //don't export common meshes!
                        continue;
                    }
                }
                
                if (!exportedMeshNames.Contains(dynamic.MeshName))
                {
                    exportedMeshNames.Add(dynamic.MeshName);
                }
            }

            //destroy the temporary prefabs
            foreach (var v in temporarySpawnedPrefabs)
            {
                GameObject.DestroyImmediate(v);
            }

            if (successfullyExportedCount == 1 && entireSelection.Count == 1)
            {
                EditorUtility.DisplayDialog("Objects exported", "Successfully exported " + successfullyExportedCount + " dynamic object", "Ok");
            }
            else
            {
                EditorUtility.DisplayDialog("Objects exported", "Successfully exported " + successfullyExportedCount + "/" + entireSelection.Count + " dynamic objects using " + exportedMeshNames.Count + " unique mesh names", "Ok");
            }
        }

        [MenuItem("Window/cognitiveVR/Export Selected Dynamic Objects", true)]
        static bool ValidateExportSelectedObjectsPrefab()
        {
            // Return false if no transform is selected.
            return Selection.activeGameObject != null;
            //return Selection.activeTransform != null;
        }

        [MenuItem("Window/cognitiveVR/Upload Dynamic Objects",priority = 110)]
        static void UploadDynamicObjects()
        {
            CognitiveVR.CognitiveVR_SceneExportWindow.UploadDynamicObjects();
        }

        [MenuItem("Window/cognitiveVR/Upload Dynamic Objects", true)]
        static bool ValidateUploadDynamicObjects()
        {
            // Return false if no dynamic directory doesn't exist
            return System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + System.IO.Path.DirectorySeparatorChar + "Dynamic");
        }

        //set custom ids on dynamic objects in scene if not already set
        //custom ids are used for aggregation
        [MenuItem("Window/cognitiveVR/Update Dynamic Object Manifest...", priority = 110)]
        static void UpdateDynamicObjectManifest()
        {
            CognitiveVR_ObjectManifestWindow.Init();
        }


#if CVR_FOVE
        [MenuItem("Window/cognitiveVR/Add Fove Prefab",priority=60)]
        static void MakeFovePrefab()
        {
            GameObject foveRigGo = new GameObject("Fove Rig");
            GameObject foveInterfaceGo = new GameObject("Fove Interface");

            foveInterfaceGo.transform.SetParent(foveRigGo.transform);

            foveInterfaceGo.AddComponent<FoveInterface>();
            
            foveInterfaceGo.AddComponent<Camera>();
            foveInterfaceGo.tag = "MainCamera";
            Undo.RecordObjects(new Object[] { foveRigGo, foveInterfaceGo }, "Create Fove Prefab");
            Undo.FlushUndoRecordObjects();

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#else
        [MenuItem("Window/cognitiveVR/Add Fove Prefab", priority = 60)]
        static void MakeFovePrefab()
        {

        }

        [MenuItem("Window/cognitiveVR/Add Fove Prefab", true)]
        static bool ValidateMakeFovePrefab()
        {
            return false;
        }
#endif
    }
}