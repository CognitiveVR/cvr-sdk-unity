using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    public class MenuItems
    {
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
                if (CognitiveVR_SceneExplorerExporter.ExportDynamicObject(v))
                {
                    successfullyExportedCount++;
                }
                var dynamic = v.GetComponent<DynamicObject>();
                if (dynamic == null) { continue; }
                
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

#if CVR_PUPIL
        [MenuItem("Window/cognitiveVR/Add Pupil Labs Vive Prefab", priority = 65)]
        static void AddPupilLabsVivePrefab()
        {
            GameObject maincam = new GameObject("Main Camera");
            GameObject pupilgaze = new GameObject("PupilGaze");
            GameObject calibrationcam = new GameObject("Calibration Camera");

            GameObject canvas = new GameObject("Canvas");
            GameObject calib = new GameObject("Calibration Target");
            GameObject left = new GameObject("Left Eye");
            GameObject right = new GameObject("Right Eye");
            GameObject center = new GameObject("Center");


            //main camera
            var cam = maincam.AddComponent<Camera>();
            maincam.tag = "MainCamera";
            cam.depth = -5;
            cam.farClipPlane = 200;

            //pupil gaze tracker
            var gazetracker = pupilgaze.AddComponent<PupilGazeTracker>();
            gazetracker.ServerIP = "127.0.0.1";

            //calibration camera
            var calibcam = calibrationcam.AddComponent<Camera>();
            calibcam.clearFlags = CameraClearFlags.Depth;
            calibcam.cullingMask = 1 << 5; //UI layer
            calibcam.orthographic = true;
            calibcam.orthographicSize = 325.4f;
            calibcam.depth = -1;

            //calibration canvas
            var calibcanvas = canvas.AddComponent<Canvas>();
            calibcanvas.renderMode = RenderMode.ScreenSpaceCamera;
            calibcanvas.worldCamera = calibcam;
            calibcanvas.planeDistance = 569.4f;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            canvas.transform.SetParent(calibrationcam.transform);
            canvas.layer = LayerMask.NameToLayer("UI");

            calib.AddComponent<CanvasRenderer>();
            calib.AddComponent<UnityEngine.UI.Image>(); //this is just a square, not the bullseye
            calib.AddComponent<PupilCalibMarker>();
            calib.transform.SetParent(canvas.transform);
            calib.layer = LayerMask.NameToLayer("UI");

            left.AddComponent<CanvasRenderer>();
            left.AddComponent<UnityEngine.UI.Image>().color = Color.red;
            left.AddComponent<EyeGazeRenderer>().Gaze = PupilGazeTracker.GazeSource.LeftEye;
            left.transform.SetParent(canvas.transform);
            left.transform.localScale = Vector3.one * 0.1f;
            left.layer = LayerMask.NameToLayer("UI");

            right.AddComponent<CanvasRenderer>();
            right.AddComponent<UnityEngine.UI.Image>().color = new Color(0.3f, 0, 1);
            right.AddComponent<EyeGazeRenderer>().Gaze = PupilGazeTracker.GazeSource.RightEye;
            right.transform.SetParent(canvas.transform);
            right.transform.localScale = Vector3.one * 0.1f;
            right.layer = LayerMask.NameToLayer("UI");

            center.AddComponent<CanvasRenderer>();
            center.AddComponent<UnityEngine.UI.Image>().color = Color.green;
            center.AddComponent<EyeGazeRenderer>().Gaze = PupilGazeTracker.GazeSource.BothEyes;
            center.transform.SetParent(canvas.transform);
            center.transform.localScale = Vector3.one * 0.33f;
            center.layer = LayerMask.NameToLayer("UI");

            Undo.RecordObjects(new Object[] { maincam, pupilgaze, calibrationcam, canvas, calib, left, right, center }, "Create Pupil Labs Vive Prefab");
            Undo.FlushUndoRecordObjects();

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }
#else
        [MenuItem("Window/cognitiveVR/Add Pupil Labs Vive Prefab", priority = 65)]
        static void AddPupilLabsVivePrefab()
        {

        }

        [MenuItem("Window/cognitiveVR/Add Pupil Labs Vive Prefab", true)]
        static bool ValidateAddPupilLabsVivePrefab()
        {
            return false;
        }
#endif
    }
}