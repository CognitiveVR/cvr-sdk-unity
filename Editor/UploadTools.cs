using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Cognitive3D
{
    internal static class UploadTools
    {
        #region Scene Upload Tools
        internal enum SceneManagementUploadState
        {
            /// <summary>
            /// We start here <br/>
            /// If target scene is open, we move to scene setup <br/>
            /// Otherwise, we open scene, and then move to scene setup
            /// </summary>
            Init,

            /// <summary>
            /// Perform basic scene setup
            /// </summary>
            SceneSetup,

            /// <summary>
            /// Sets up controllers
            /// </summary>
            GameObjectSetup,

            /// <summary>
            /// Export Scene
            /// </summary>
            Export,

            /// <summary>
            /// Wait for the delayed export operation to finish before starting upload
            /// </summary>
            WaitingForExportDelay,

            /// <summary>
            /// Begin upload process
            /// </summary>
            StartUpload,

            /// <summary>
            /// Wait for upload to complete
            /// </summary>
            Uploading,

            /// <summary>
            /// Complete process, cleanup
            /// </summary>
            Complete
        };

        static List<SceneEntry> entries = new List<SceneEntry>();

        /// <summary>
        /// If true, export dynamics from the scenes too <br/>
        /// Otherwise, just export the scenes
        /// </summary>
        static bool exportDynamics;

        /// <summary>
        /// Set to false until user clicks export button <br/>
        /// User's click sets it to true <br/>
        /// Once export complete, this variable is set back to false
        /// </summary>
        static bool isExporting;

        /// <summary>
        /// Used to track the current scene in the Update FSM
        /// </summary>
        static int sceneIndex;

        internal static SceneManagementUploadState sceneUploadState = SceneManagementUploadState.Init;

        internal delegate void onUploadScenesComplete();
        /// <summary>
        /// Called just after a session has begun
        /// </summary>
        internal static event onUploadScenesComplete OnUploadScenesComplete;
        private static void InvokeUploadScenesCompleteEvent() { if (OnUploadScenesComplete != null) { OnUploadScenesComplete.Invoke(); } }
        private static bool completedUpload;
        internal static bool CompletedUpload { get => completedUpload; set => completedUpload = value; }

        internal static List<SceneEntry> GetSelectedScenes(List<SceneEntry> sceneEntries)
        {
            var selectedScenes = new List<SceneEntry>();
            foreach (var sceneEntry in sceneEntries)
            {
                if (sceneEntry.selected)
                {
                    selectedScenes.Add(sceneEntry);
                }
            }
            return selectedScenes;
        }

        internal static void UploadScenes(List<SceneEntry> scenes)
        {
            // Asking user if they want to include dynamics for all scenes
            bool includeDynamics = EditorUtility.DisplayDialog(
                "Export and Upload Dynamics",
                "Do you want to include dynamics for all selected scenes in this upload? (Recommended)\n\n" +
                "You can also upload dynamics later via Dynamic Objects > Feature Builder.",
                "Yes",
                "No"
            );

            exportDynamics = includeDynamics;

            entries = scenes;
            sceneIndex = 0;
            isExporting = true;
            sceneUploadState = SceneManagementUploadState.Init;

            UnityEditor.EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (!isExporting)
                return;

            if (sceneIndex < entries.Count)
            {
                float progress = (float)sceneIndex / entries.Count;
                bool cancelled = UnityEditor.EditorUtility.DisplayCancelableProgressBar(
                    "Uploading Scenes",
                    $"Processing {System.IO.Path.GetFileNameWithoutExtension(entries[sceneIndex].path)}...",
                    progress
                );

                if (cancelled)
                {
                    Debug.Log("<color=yellow>Scene upload cancelled by user.</color>");
                    isExporting = false;
                    UnityEditor.EditorApplication.update -= Update;
                    UnityEditor.EditorUtility.ClearProgressBar();
                    return;
                }

                UnityEditor.SceneView.RepaintAll();
            }

            if (sceneIndex > entries.Count - 1)
            {
                isExporting = false;
                InvokeUploadScenesCompleteEvent();
                UnityEditor.EditorApplication.update -= Update;
                UnityEditor.EditorUtility.ClearProgressBar();
                return;
            }

            if (!entries[sceneIndex].selected)
            {
                sceneIndex++;
                sceneUploadState = SceneManagementUploadState.Init;
                return;
            }

            switch (sceneUploadState)
            {
                case SceneManagementUploadState.Init:
                    if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == entries[sceneIndex].path)
                    {
                        sceneUploadState = SceneManagementUploadState.SceneSetup;
                    }
                    else
                    {
                        var sceneEntry = entries[sceneIndex];
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(sceneEntry.path);
                        sceneUploadState = SceneManagementUploadState.Export;
                    }

                    // Add Cognitive3D_manager
                    var found = Object.FindAnyObjectByType<Cognitive3D_Manager>();
                    if (found == null)
                    {
                        GameObject c3dManagerPrefab = EditorCore.GetCognitive3DManagerPrefab();
                        PrefabUtility.InstantiatePrefab(c3dManagerPrefab);
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    }
                    return;

                case SceneManagementUploadState.SceneSetup:
                    // Let Unity refresh editor before continuing
                    EditorApplication.delayCall += () =>
                    {
                        sceneUploadState = SceneManagementUploadState.Export;
                    };
                    return;

                case SceneManagementUploadState.Export:
                    EditorApplication.delayCall += () =>
                    {
                        string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                        var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);

                        if (currentSettings == null)
                            Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                        float sceneSize = EditorCore.GetSceneFileSize(Cognitive3D_Preferences.FindCurrentScene());

                        if (sceneSize < 1)
                        {
                            SegmentAnalytics.TrackEvent("ExportingSceneLess1MB_SceneExportPage", "SceneExportPage", "new");
                        }
                        else if (sceneSize >= 1 && sceneSize <= 500)
                        {
                            SegmentAnalytics.TrackEvent("ExportingSceneLessOrEqual500MB_SceneExportPage", "SceneExportPage", "new");
                        }
                        else // sceneSize > 500
                        {
                            SegmentAnalytics.TrackEvent("ExportingSceneGreater500MB_SceneExportPage", "SceneExportPage", "new");
                        }

                        ExportUtility.ExportGLTFScene(false);

                        string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                        string path = EditorCore.GetSubDirectoryPath(fullName);
                        ExportUtility.GenerateSettingsFile(path, fullName);
                        DebugInformationWindow.WriteDebugToFile(path + "debug.log");

                        UnityEditor.EditorUtility.SetDirty(EditorCore.GetPreferences());
                        UnityEditor.AssetDatabase.SaveAssets();

                        // Advance state after export completes
                        sceneUploadState = SceneManagementUploadState.StartUpload;
                    };
                    // Prevent loop from running repeatedly while waiting for delayCall
                    sceneUploadState = SceneManagementUploadState.WaitingForExportDelay;
                    return;

                case SceneManagementUploadState.WaitingForExportDelay:
                    // Do nothing — waiting for delayCall to complete
                    return;

                case SceneManagementUploadState.StartUpload:
                    CompletedUpload = false;
                    UploadSceneAndDynamics(exportDynamics, exportDynamics, true, true);
                    sceneUploadState = SceneManagementUploadState.Uploading;
                    return;

                case SceneManagementUploadState.Uploading:
                    if (CompletedUpload)
                    {
                        sceneUploadState = SceneManagementUploadState.Complete;
                    }
                    return;

                case SceneManagementUploadState.Complete:
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    CompletedUpload = false;
                    sceneUploadState = SceneManagementUploadState.Init;
                    sceneIndex++;
                    return;
                default:
                    Util.logWarning($"Unexpected sceneUploadState: {sceneUploadState}. Resetting to Init.");
                    sceneUploadState = SceneManagementUploadState.Init;
                    break;
            }
        }

        /// <summary>
        /// Upload exported scene and optionally, dynamics
        /// </summary>
        /// <param name="uploadExportedDynamics">If true, upload dynamics from export directory</param>
        /// <param name="exportAndUploadDynamicsFromScene">If true, exports dynamics from scene, and uploads them</param>
        /// <param name="uploadSceneGeometry">If true, upload scene geometry</param>
        /// <param name="uploadThumbnail">If true, upload scene thumbnail</param>
        /// <param name="showPopups">If true, show popups (use false for automation)</param>
        internal static void UploadSceneAndDynamics(bool uploadExportedDynamics, bool exportAndUploadDynamicsFromScene, bool uploadSceneGeometry, bool uploadThumbnail, bool showPopups = false)
        {
            System.Action completedmanifestupload = delegate
            {
                if (uploadExportedDynamics)
                {
                    ExportUtility.UploadAllDynamicObjectMeshes(showPopups);
                }
                else if (exportAndUploadDynamicsFromScene)
                {
                    List<string> dynamicMeshNames = new List<string>();
                    var dynamicObjectsInScene = GetDynamicObjectsInScene();
                    foreach (var dyn in dynamicObjectsInScene)
                    {
                        dynamicMeshNames.Add(dyn.MeshName);
                    }
                    ExportUtility.UploadDynamicObjects(dynamicMeshNames, showPopups);
                }
                CompletedUpload = true;
            };

            // Fifth: upload manifest
            System.Action completedRefreshSceneVersion = delegate
            {
                if (uploadExportedDynamics || exportAndUploadDynamicsFromScene)
                {
                    //TODO ask if dev wants to upload disabled dynamic objects as well (if there are any)
                    AggregationManifest manifest = new AggregationManifest();
                    manifest.AddOrReplaceDynamic(GetDynamicObjectsInScene());
                    EditorCore.UploadManifest(manifest, completedmanifestupload, completedmanifestupload);
                }
                else
                {
                    completedmanifestupload.Invoke();
                }
            };

            // Fourth upload dynamics
            System.Action<int> completeSceneUpload = delegate (int responseCode)
            {
                if (responseCode == 200 || responseCode == 201)
                {
                    if (exportAndUploadDynamicsFromScene)
                    {
                        ExportAllDynamicsInScene();
                    }
                    EditorCore.RefreshSceneVersion(completedRefreshSceneVersion); // likely completed in previous step, but just in case
                }
                ProjectValidation.RegenerateItems();
            };

            //third upload scene
            System.Action completeScreenshot = delegate
            {
                Cognitive3D_Preferences.SceneSettings current = Cognitive3D_Preferences.FindCurrentScene();
                if (current == null)
                {
                    Debug.LogError("Trying to upload to a scene with no settings");
                    return;
                }

                if (uploadSceneGeometry)
                {
                    if (showPopups)
                    {
                        if (string.IsNullOrEmpty(current.SceneId))
                        {
                            // NEW SCENE
                            if (EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to " + EditorCore.DisplayValue(DisplayKey.ViewerName) + "?", "Ok", "Cancel"))
                            {
                                sceneUploadProgress = 0;
                                sceneUploadStartTime = EditorApplication.timeSinceStartup;
                                ExportUtility.UploadDecimatedScene(current, completeSceneUpload, ReceiveSceneUploadProgress);
                            }
                        }
                        else
                        {
                            // NEW SCENE VERSION
                            if (EditorUtility.DisplayDialog("Upload New Version", "Upload a new version of this existing scene? Will archive previous version", "Ok", "Cancel"))
                            {
                                sceneUploadProgress = 0;
                                sceneUploadStartTime = EditorApplication.timeSinceStartup;
                                ExportUtility.UploadDecimatedScene(current, completeSceneUpload, ReceiveSceneUploadProgress);
                            }
                        }
                    }
                    else // UPLOAD WITHOUT POPUPS
                    {
                        ExportUtility.UploadDecimatedScene(current, completeSceneUpload, ReceiveSceneUploadProgress);
                    }
                }
                else
                {
                    //check to upload the thumbnail (without the scene geo)
                    if (uploadThumbnail)
                    {
                        EditorCore.UploadSceneThumbnail(current);
                    }
                    completeSceneUpload.Invoke(200);
                }
            };

            //second save screenshot
            System.Action completedRefreshSceneVersion1 = delegate
            {
                if (uploadThumbnail)
                {
                    EditorCore.SaveScreenshot(EditorCore.GetSceneRenderTexture(), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                }
                else
                {
                    //use the existing screenshot (assuming it exists)
                    completeScreenshot.Invoke();
                    completeScreenshot = null;
                }
            };

            CompletedUpload = false;
            EditorCore.RefreshSceneVersion(completedRefreshSceneVersion1);
        }

        static float sceneUploadProgress;
        static double sceneUploadStartTime;
        //TODO styled UI element to display web request progress instead of built-in unity popup
        static void ReceiveSceneUploadProgress(float progress)
        {
            sceneUploadProgress = progress;
        }

        internal static bool EnsureSceneReady(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath)) return false;

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scenePath != activeScene.path)
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            }

            if (string.IsNullOrEmpty(activeScene.name))
            {
                bool saveNow = EditorUtility.DisplayDialog(
                    "Scene Not Saved",
                    "Cannot proceed with a scene that is not saved.\nDo you want to save now?",
                    "Save",
                    "Cancel"
                );

                if (!saveNow) return false;

                if (!UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes())
                    return false;
            }

            return true;
        }
        #endregion

        #region Dynamic Objects Upload Tools
        internal static List<DynamicObject> GetDynamicObjectsInScene()
        {
            return GameObject.FindObjectsByType<DynamicObject>(FindObjectsSortMode.None).ToList();
        }

        internal static void ExportAllDynamicsInScene()
        {
            List<DynamicObject> dynsInSceneList = new List<DynamicObject>();

            // This array HAS TO BE reinitialized here because
            // this function can be from other places and
            // we cannot guarantee that it has been initialized
            var dynamicObjectsInScene = GetDynamicObjectsInScene();
            foreach (var dyn in dynamicObjectsInScene)
            {
                dynsInSceneList.Add(dyn);
            }
            ExportUtility.ExportDynamicObjects(dynsInSceneList);
        }

        internal static void ExportAndUploadAllDynamicsInScene()
        {
            List<DynamicObject> dynsInSceneList = new List<DynamicObject>();

            // This array HAS TO BE reinitialized here because
            // this function can be from other places and
            // we cannot guarantee that it has been initialized
            var dynamicObjectsInScene = GetDynamicObjectsInScene();
            foreach (var dyn in dynamicObjectsInScene)
            {
                dynsInSceneList.Add(dyn);
            }
            ExportUtility.ExportDynamicObjects(dynsInSceneList);

            UploadDynamics(true, true);
        }

        internal static void UploadDynamics(bool uploadExportedDynamics, bool exportAndUploadDynamicsFromScene, bool showPopups = false)
        {
            void OnManifestUploadComplete()
            {
                if (uploadExportedDynamics)
                {
                    ExportUtility.UploadAllDynamicObjectMeshes(showPopups);
                }
                else if (exportAndUploadDynamicsFromScene)
                {
                    var dynamicMeshNames = new List<string>();
                    var dynamicObjectsInScene = GetDynamicObjectsInScene();
                    foreach (var dyn in dynamicObjectsInScene)
                    {
                        dynamicMeshNames.Add(dyn.MeshName);
                    }
                    ExportUtility.UploadDynamicObjects(dynamicMeshNames, showPopups);
                }
            }

            void OnSceneVersionRefreshed()
            {
                if (uploadExportedDynamics || exportAndUploadDynamicsFromScene)
                {
                    AggregationManifest manifest = new AggregationManifest();
                    manifest.AddOrReplaceDynamic(GetDynamicObjectsInScene());
                    EditorCore.UploadManifest(manifest, OnManifestUploadComplete, OnManifestUploadComplete);
                }
                else
                {
                    OnManifestUploadComplete();
                }
            }

            EditorCore.RefreshSceneVersion(OnSceneVersionRefreshed);
        }

        internal static void ExportAndUploadDynamics(bool selectedOnly, List<DynamicObjectEntry> entries, DynamicObjectDetailGUI detailGUI = null)
        {
            EditorCore.RefreshSceneVersion(() =>
            {
                int selection = EditorUtility.DisplayDialogComplex("Export Meshes?", "Do you want to export meshes before uploading to Scene Explorer?", "Yes, export selected meshes", "No, use existing files", "Cancel");

                if (selection == 2) //cancel
                {
                    return;
                }

                List<GameObject> uploadList = new List<GameObject>();
                List<DynamicObject> exportList = new List<DynamicObject>();

                if (selection == 0) //export
                {
                    foreach (var entry in entries)
                    {
                        var dyn = entry.objectReference;
                        if (dyn == null) { continue; }
                        if (selectedOnly)
                        {
                            if (!entry.selected) { continue; }
                        }
                        //check if export files exist
                        exportList.Add(dyn);
                        uploadList.Add(dyn.gameObject);
                    }
                    ExportUtility.ExportDynamicObjects(exportList);
                }
                else if (selection == 1) //don't export
                {
                    foreach (var entry in entries)
                    {
                        var dyn = entry.objectReference;
                        if (dyn == null) { continue; }
                        if (selectedOnly)
                        {
                            if (!entry.selected) { continue; }
                        }
                        //check if export files exist
                        uploadList.Add(dyn.gameObject);
                    }
                }
                //upload meshes and ids
                EditorCore.RefreshSceneVersion(delegate
                {
                    if (ExportUtility.UploadSelectedDynamicObjectMeshes(uploadList, true))
                    {
                        var manifest = new AggregationManifest();
                        List<DynamicObject> manifestList = new List<DynamicObject>();
                        foreach (var entry in entries)
                        {
                            if (selectedOnly)
                            {
                                if (!entry.selected) { continue; }
                            }
                            var dyn = entry.objectReference;
                            if (dyn == null) { continue; }

                            if (!entry.isIdPool)
                            {
                                if (dyn.idSource == DynamicObject.IdSourceType.CustomID)
                                {
                                    manifestList.Add(entry.objectReference);
                                }
                            }
                            else
                            {
                                foreach (var poolid in entry.poolReference.Ids)
                                {
                                    manifest.objects.Add(new AggregationManifest.AggregationManifestEntry(entry.poolReference.PrefabName, entry.poolReference.MeshName, poolid, entry.objectReference.IsController, new float[] { 1, 1, 1 }, new float[] { 0, 0, 0 }, new float[] { 0, 0, 0, 1 }));
                                }
                            }
                        }
                        manifest.AddOrReplaceDynamic(manifestList);
                        System.Action refreshWindowOnManifest = delegate
                        {
                            if (detailGUI != null) detailGUI.RefreshList();
                        };

                        EditorCore.UploadManifest(manifest, refreshWindowOnManifest);
                    }
                });
            });
        }
        #endregion
    }

    #region Scene Entry Data
    internal class SceneEntry
    {
        internal string path;
        internal bool selected;
        internal bool shouldDisplay;
        internal int versionNumber;

        internal SceneEntry(string pathToScene, int versionNum, bool sceneSelected = false, bool sceneShouldDisplay = true)
        {
            path = pathToScene;
            versionNumber = versionNum;
            selected = sceneSelected;
            shouldDisplay = sceneShouldDisplay;
        }
    }
    #endregion

    #region Dynamic Entry Data
    internal class DynamicObjectEntry
    {
        //IMPROVEMENT for objects in scene, cache warning for missing collider in children
        internal bool visible = true; //currently shown in the filtered list
        internal bool selected; //not necessarily selected as a gameobject, just checked in this list
        internal string meshName;
        internal bool hasExportedMesh;
        internal bool isIdPool;
        internal int idPoolCount;
        internal DynamicObject objectReference;
        internal DynamicObjectIdPool poolReference;
        internal string gameobjectName;
        internal bool hasBeenUploaded;
        internal DynamicObjectEntry(string meshName, bool exportedMesh, DynamicObject reference, string name, bool initiallySelected, bool uploaded)
        {
            objectReference = reference;
            gameobjectName = name;
            this.meshName = meshName;
            hasExportedMesh = exportedMesh;
            selected = initiallySelected;
            hasBeenUploaded = uploaded;
        }
        internal DynamicObjectEntry(bool exportedMesh, DynamicObjectIdPool reference, bool initiallySelected, bool uploaded)
        {
            isIdPool = true;
            poolReference = reference;
            idPoolCount = poolReference.Ids.Length;
            gameobjectName = poolReference.PrefabName;
            meshName = poolReference.MeshName;
            hasExportedMesh = exportedMesh;
            selected = initiallySelected;
            hasBeenUploaded = uploaded;
        }
    }
    #endregion

    #region Aggregation Manifest
    [System.Serializable]
    internal class AggregationManifest
    {
        [System.Serializable]
        public class AggregationManifestEntry
        {
            public string name;
            public string mesh;
            public string id;
            public bool isController;
            public float[] scaleCustom = new float[] { 1, 1, 1 };
            public float[] position = new float[] { 0, 0, 0 };
            public float[] rotation = new float[] { 0, 0, 0, 1 };
            public AggregationManifestEntry(string _name, string _mesh, string _id, bool _isController, float[] _scaleCustom)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
                isController = _isController;
                scaleCustom = _scaleCustom;
            }
            public AggregationManifestEntry(string _name, string _mesh, string _id, bool _isController, float[] _scaleCustom, float[] _position, float[] _rotation)
            {
                name = _name;
                mesh = _mesh;
                id = _id;
                isController = _isController;
                scaleCustom = _scaleCustom;
                position = _position;
                rotation = _rotation;
            }
            public override string ToString()
            {
                return "{\"name\":\"" + name + "\",\"mesh\":\"" + mesh + "\",\"id\":\"" + id + "\",\"isController\":\"" + isController +
                    "\",\"scaleCustom\":[" + scaleCustom[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + scaleCustom[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + scaleCustom[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) +
                    "],\"initialPosition\":[" + position[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + position[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + position[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) +
                    "],\"initialRotation\":[" + rotation[0].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + rotation[1].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + rotation[2].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "," + rotation[3].ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "]}";
            }
        }
        public List<AggregationManifestEntry> objects = new List<AggregationManifestEntry>();

        /// <summary>
        /// adds or updates dynamic object ids in a provided manifest for aggregation
        /// </summary>
        /// <param name="scenedynamics"></param>
        public void AddOrReplaceDynamic(List<DynamicObject> scenedynamics, bool silent = false)
        {
            bool meshNameMissing = false;
            List<string> missingMeshGameObjects = new List<string>();
            foreach (var dynamic in scenedynamics)
            {

                var replaceEntry = objects.Find(delegate (AggregationManifest.AggregationManifestEntry obj) { return obj.id == dynamic.CustomId.ToString(); });
                if (replaceEntry == null)
                {
                    //don't include meshes with empty mesh names in manifest
                    if (!string.IsNullOrEmpty(dynamic.MeshName))
                    {
                        objects.Add(new AggregationManifest.AggregationManifestEntry(dynamic.gameObject.name, dynamic.MeshName, dynamic.CustomId.ToString(),
                            dynamic.IsController,
                            new float[] { dynamic.transform.lossyScale.x, dynamic.transform.lossyScale.y, dynamic.transform.lossyScale.z },
                            new float[] { dynamic.transform.position.x, dynamic.transform.position.y, dynamic.transform.position.z },
                            new float[] { dynamic.transform.rotation.x, dynamic.transform.rotation.y, dynamic.transform.rotation.z, dynamic.transform.rotation.w }));
                    }
                    else
                    {
                        missingMeshGameObjects.Add(dynamic.gameObject.name);
                        meshNameMissing = true;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(dynamic.MeshName))
                    {
                        replaceEntry.mesh = dynamic.MeshName;
                        replaceEntry.name = dynamic.gameObject.name;
                    }
                    else
                    {
                        missingMeshGameObjects.Add(dynamic.gameObject.name);
                        meshNameMissing = true;
                    }
                }
            }

            if (meshNameMissing)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("Dynamic Objects missing mesh name:\n");
                foreach (var v in missingMeshGameObjects)
                {
                    sb.Append(v);
                    sb.Append("\n");
                }
                Debug.LogWarning(sb.ToString());
                if (silent == false)
                {
                    EditorUtility.DisplayDialog("Error", "One or more Dynamic Objects are missing a mesh name and were not uploaded to scene.\n\nSee Console for details", "Ok");
                }
            }
        }
    }
    #endregion
}
