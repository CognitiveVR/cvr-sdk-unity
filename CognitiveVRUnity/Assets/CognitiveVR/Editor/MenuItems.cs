using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class MenuItems
    {
#if CVR_FOVE
        [MenuItem("cognitiveVR/Add Fove Prefab",priority=52)]
        static void MakeFovePrefab()
        {
            GameObject player = new GameObject("Player");
            GameObject foveInterface = new GameObject("Fove Interface");
            GameObject cameraboth = new GameObject("Fove Eye Camera both");
            GameObject cameraright = new GameObject("Fove Eye Camera right");
            GameObject cameraleft = new GameObject("Fove Eye Camera left");

            foveInterface.transform.SetParent(player.transform);

            cameraboth.transform.SetParent(foveInterface.transform);
            cameraright.transform.SetParent(foveInterface.transform);
            cameraleft.transform.SetParent(foveInterface.transform);

            var tempInterface = foveInterface.AddComponent<FoveInterface>();

            var rightcam = cameraright.AddComponent<FoveEyeCamera>();
            rightcam.whichEye = Fove.EFVR_Eye.Right;
            //cameraright.AddComponent<Camera>();

            var leftcam = cameraleft.AddComponent<FoveEyeCamera>();
            leftcam.whichEye = Fove.EFVR_Eye.Left;
            //cameraleft.AddComponent<Camera>();

            cameraboth.AddComponent<Camera>();
            cameraboth.tag = "MainCamera";
            Undo.RecordObjects(new Object[] { player, foveInterface, cameraboth, cameraright, cameraleft }, "Create Fove Prefab");
            Undo.FlushUndoRecordObjects();

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#endif
        [MenuItem("cognitiveVR/Add cognitiveVR Manager", priority = 51)]
        static void AddCognitiveVRManager()
        {
            CognitiveVR_ComponentSetup.AddCognitiveVRManager();
        }

        [MenuItem("cognitiveVR/Export Selected Dynamic Objects")]
        static void ExportSelectedObjectsPrefab()
        {
            List<Transform> entireSelection = new List<Transform>();
            entireSelection.AddRange(Selection.GetTransforms(SelectionMode.Editable));

            Debug.Log("Trying to export " + entireSelection + " dynamic objects");

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
            foreach (var v in sceneObjects)
            {
                if (CognitiveVR_SceneExplorerExporter.ExportEachSelectionToSingle(v))
                {
                    successfullyExportedCount++;
                }
            }

            //destroy the temporary prefabs
            foreach (var v in temporarySpawnedPrefabs)
            {
                GameObject.DestroyImmediate(v);
            }

            EditorUtility.DisplayDialog("Objects exported", "Successfully exported " + successfullyExportedCount + "/" + entireSelection.Count + " dynamic objects", "Ok");
        }

        [MenuItem("cognitiveVR/Export Selected Dynamic Objects", true)]
        static bool ValidateExportSelectedObjectsPrefab()
        {
            // Return false if no transform is selected.
            return Selection.activeTransform != null;
        }

        [MenuItem("cognitiveVR/Upload Dynamic Objects")]
        static void UploadDynamicObjects()
        {
            CognitiveVR.CognitiveVR_SceneExportWindow.UploadDynamicObjects();
        }

        [MenuItem("cognitiveVR/Upload Dynamic Objects", true)]
        static bool ValidateUploadDynamicObjects()
        {
            // Return false if no dynamic directory doesn't exist
            return System.IO.Directory.Exists(System.IO.Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + System.IO.Path.DirectorySeparatorChar + "Dynamic");
        }
    }
}