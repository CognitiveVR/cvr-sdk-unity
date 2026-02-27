using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
        /// If true, export and upload scene geometry <br/>
        /// </summary>
        static bool exportSceneGeometry;

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

        internal static void UploadScenes(List<SceneEntry> scenes, bool uploadGeometry)
        {
            if (uploadGeometry)
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
            }
            exportSceneGeometry = uploadGeometry;

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

            // Only show batch progress bar during Init state - export and upload have their own progress indicators
            if (sceneIndex < entries.Count && sceneUploadState == SceneManagementUploadState.Init)
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
                    // Clear batch progress bar - export has its own progress indicators
                    EditorUtility.ClearProgressBar();
                    EditorApplication.delayCall += () =>
                    {
                        string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                        var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);

                        if (currentSettings == null || string.IsNullOrEmpty(currentSettings.SceneId))
                        {
                            string sceneId = EditorCore.GenerateSceneIdFromPath(currentScenePath);

                            Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), sceneId, 1);
                        }

                        // Check if geometry should be exported
                        if (exportSceneGeometry)
                        {
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
                        }

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
                    // Clear any progress bar - upload has its own progress indicators
                    EditorUtility.ClearProgressBar();
                    CompletedUpload = false;
                    if (exportSceneGeometry)
                    {
                        UploadSceneAndDynamicsInternal(
                        uploadExportedDynamics: exportDynamics, 
                        exportAndUploadDynamicsFromScene: exportDynamics, 
                        uploadSceneGeometry: true, 
                        uploadThumbnail: true, 
                        useOptimizedUpload: true,
                        showPopups: false);
                    }
                    else
                    {
                        CompletedUpload = true;
                    }
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
        /// Consolidated internal method for uploading scenes and dynamics
        /// Supports both optimized (two-phase) and legacy (single-phase) upload strategies
        /// </summary>
        internal static void UploadSceneAndDynamicsInternal(
            bool uploadExportedDynamics,
            bool exportAndUploadDynamicsFromScene,
            bool uploadSceneGeometry,
            bool uploadThumbnail,
            bool useOptimizedUpload,
            bool showPopups,
            System.Action onComplete = null)
        {
            CompletedUpload = false;

            // Step 0: Initial version check - do this FIRST before any other work
            System.Action startUploadWorkflow = delegate
            {
                // Step 5: Upload dynamics and manifest
                System.Action completedManifestUpload = delegate
                {
                    HandleDynamicsUpload(uploadExportedDynamics, exportAndUploadDynamicsFromScene, showPopups);
                    CompletedUpload = true;

                    // Invoke completion callback if provided (for standalone uploads)
                    onComplete?.Invoke();
                };

                // Step 4: Upload manifest after scene version is refreshed
                System.Action completedRefreshSceneVersion = delegate
                {
                    if (uploadExportedDynamics || exportAndUploadDynamicsFromScene)
                    {
                        UploadManifestForDynamics(completedManifestUpload);
                    }
                    else
                    {
                        completedManifestUpload.Invoke();
                    }
                };

                // Step 3: Export and upload dynamics after scene upload completes
                System.Action<int> completeSceneUpload = delegate (int responseCode)
                {
                    if (responseCode == 200 || responseCode == 201)
                    {
                        if (exportAndUploadDynamicsFromScene)
                        {
                            ExportAllDynamicsInScene();
                        }
                        EditorCore.RefreshSceneVersion(completedRefreshSceneVersion);
                    }
                    else
                    {
                        CompletedUpload = true;

                        // Invoke completion callback even on error (for standalone uploads)
                        onComplete?.Invoke();
                    }
                    ProjectValidation.RegenerateItems();
                };

                // Step 2: Upload scene geometry after screenshot is saved
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
                        bool shouldProceed = true;
                        if (showPopups)
                        {
                            shouldProceed = ShowUploadConfirmationDialog(current);
                        }

                        if (shouldProceed)
                        {
                            sceneUploadProgress = 0;
                            sceneUploadStartTime = EditorApplication.timeSinceStartup;

                            // Choose upload strategy based on parameter
                            if (useOptimizedUpload)
                            {
                                UploadDecimatedSceneOptimized(current, completeSceneUpload, null);
                            }
                            else
                            {
                                UploadDecimatedScene(current, completeSceneUpload, null);
                            }
                        }
                    }
                    else
                    {
                        if (uploadThumbnail)
                        {
                            EditorCore.UploadSceneThumbnail(current);
                        }
                        completeSceneUpload.Invoke(200);
                    }
                };

                // Step 1: Save screenshot
                if (uploadThumbnail)
                {
                    EditorCore.SaveScreenshot(EditorCore.GetSceneRenderTexture(), UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, completeScreenshot);
                }
                else
                {
                    completeScreenshot.Invoke();
                }
            };

            // INITIAL STEP: Quick version check before starting upload workflow
            var currentSettings = Cognitive3D_Preferences.FindCurrentScene();
            if (currentSettings != null)
            {
                // Scene exists on server - do quick version check first
                EditorCore.RefreshSceneVersion(delegate
                {
                    startUploadWorkflow.Invoke();
                });
            }
            else
            {
                // New/untracked scene - no version to check, start workflow directly
                startUploadWorkflow.Invoke();
            }
        }

        /// <summary>
        /// Helper method to handle dynamics upload based on configuration
        /// </summary>
        private static void HandleDynamicsUpload(bool uploadExportedDynamics, bool exportAndUploadDynamicsFromScene, bool showPopups)
        {
            if (uploadExportedDynamics)
            {
                ExportUtility.UploadAllDynamicObjectMeshes(showPopups);
            }
            else if (exportAndUploadDynamicsFromScene)
            {
                List<string> dynamicMeshNames = GetDynamicMeshNames();
                ExportUtility.UploadDynamicObjects(dynamicMeshNames, showPopups);
            }
        }

        /// <summary>
        /// Helper method to upload manifest for dynamic objects
        /// </summary>
        private static void UploadManifestForDynamics(System.Action onComplete)
        {
            AggregationManifest manifest = new AggregationManifest();
            manifest.AddOrReplaceDynamic(GetDynamicObjectsInScene());
            EditorCore.UploadManifest(manifest, onComplete, onComplete);
        }

        /// <summary>
        /// Extract dynamic mesh names from scene
        /// </summary>
        private static List<string> GetDynamicMeshNames()
        {
            List<string> dynamicMeshNames = new List<string>();
            var dynamicObjectsInScene = GetDynamicObjectsInScene();
            foreach (var dyn in dynamicObjectsInScene)
            {
                dynamicMeshNames.Add(dyn.MeshName);
            }
            return dynamicMeshNames;
        }

        /// <summary>
        /// Show upload confirmation dialog based on scene state
        /// </summary>
        private static bool ShowUploadConfirmationDialog(Cognitive3D_Preferences.SceneSettings current)
        {
            bool hasBeenUploaded = !string.IsNullOrEmpty(current.SceneId) && !string.IsNullOrEmpty(current.backendSceneId);
            if (!hasBeenUploaded)
            {
                // NEW SCENE
                return EditorUtility.DisplayDialog("Upload New Scene", "Upload " + current.SceneName + " to " + EditorCore.DisplayValue(DisplayKey.ViewerName) + "?", "Ok", "Cancel");
            }
            else
            {
                // NEW SCENE VERSION
                return EditorUtility.DisplayDialog("Upload New Version", "Upload a new version of this existing scene? Will archive previous version", "Ok", "Cancel");
            }
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
            var dynamicsInScene = GetDynamicObjectsInScene();
            ExportUtility.ExportDynamicObjects(dynamicsInScene);
        }

        internal static void ExportAndUploadAllDynamicsInScene()
        {
            ExportAllDynamicsInScene();
            UploadDynamics(true, true);
        }

        internal static void UploadDynamics(bool uploadExportedDynamics, bool exportAndUploadDynamicsFromScene, bool showPopups = false)
        {
            void OnManifestUploadComplete()
            {
                HandleDynamicsUpload(uploadExportedDynamics, exportAndUploadDynamicsFromScene, showPopups);
            }

            void OnSceneVersionRefreshed()
            {
                if (uploadExportedDynamics || exportAndUploadDynamicsFromScene)
                {
                    UploadManifestForDynamics(OnManifestUploadComplete);
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

        /// <summary>
        /// Optimized upload method that splits scene upload into two phases:
        /// Phase 1: Core files (gltf, bin, settings.json) + screenshot as multipart form
        /// Phase 2: Auxiliary files (textures, debug.log) as gzip compressed archive
        /// This reduces payload size and allows backend to process geometry immediately while textures upload
        /// </summary>
        public static void UploadDecimatedSceneOptimized(Cognitive3D_Preferences.SceneSettings settings, System.Action<int> uploadComplete, System.Action<float> progressCallback)
        {
            if (settings == null)
            {
                uploadComplete?.Invoke(0); // Notify caller upload was skipped
                return;
            }

            UploadSceneSettingsOptimized = settings;
            UploadCompleteOptimized = uploadComplete;
            UploadProgressCallbackOptimized = progressCallback;

            bool hasExistingSceneId = settings != null && !string.IsNullOrEmpty(settings.SceneId);
            bool uploadConfirmed = false;
            string sceneName = settings.SceneName;
            string[] filePaths = new string[] { };

            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

            if (SceneExportDirExists)
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar);
            }

            // Confirm upload dialog
            if (!SceneExportDirExists || filePaths.Length <= 1)
            {
                if (EditorUtility.DisplayDialog("Upload Scene", "Scene " + settings.SceneName + " has no exported geometry. Upload anyway?", "Yes", "No"))
                {
                    uploadConfirmed = true;
                    // Create settings.json file
                    string objPath = EditorCore.GetSubDirectoryPath(sceneName);
                    Directory.CreateDirectory(objPath);

                    string escapedSceneName = settings.SceneName.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                    string jsonSettingsContents = "{ \"scale\":1, \"sceneName\":\"" + escapedSceneName + "\",\"sdkVersion\":\"" + Cognitive3D_Manager.SDK_VERSION + "\"}";
                    File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

                    string debugContent = DebugInformationWindow.GetDebugContents();
                    File.WriteAllText(objPath + "debug.log", debugContent);
                }
            }
            else
            {
                uploadConfirmed = true;
            }

            if (!uploadConfirmed)
            {
                UploadCompleteOptimized?.Invoke(0); // Notify caller upload was cancelled
                CleanupOptimizedUploadState();
                return;
            }

            // Classify files into core and auxiliary
            List<string> coreFiles;
            List<string> auxiliaryFiles;
            string screenshotFile;
            ClassifyFiles(sceneName, sceneExportDirectory, out coreFiles, out auxiliaryFiles, out screenshotFile);

            // Store auxiliary files for Phase 2
            AuxiliaryFilesForPhase2 = auxiliaryFiles.ToArray();

            // Build list of files with field names for streaming multipart
            var filesWithFieldNames = new List<(string filePath, string fieldName, string fileName)>();

            foreach (var f in coreFiles)
            {
                if (f.ToLower().EndsWith(".ds_store"))
                {
                    continue;
                }
                filesWithFieldNames.Add((f, "file", null));
            }

            // Add screenshot with custom field name
            if (!string.IsNullOrEmpty(screenshotFile) && File.Exists(screenshotFile))
            {
                filesWithFieldNames.Add((screenshotFile, "screenshot", "screenshot.png"));
                Util.logDebug("Added screenshot to Phase 1");
            }
            else
            {
                Util.logWarning("SceneExportWindow Upload can't find screenshot file");
            }

            string boundary;
            string tempMultipartPath = CreateStreamingMultipartFile(filesWithFieldNames, out boundary, "Preparing core files (Phase 1)");
            if (string.IsNullOrEmpty(tempMultipartPath))
            {
                Debug.LogError("Failed to create multipart file for Phase 1");
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
                return;
            }
            TempMultipartFilePath = tempMultipartPath;

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);

            string uploadMessage = hasExistingSceneId ? "Uploading core files (Phase 1)" : "Uploading new scene (Phase 1)";
            var url = (hasExistingSceneId && settings.VersionId > 0) ?
                CognitiveStatics.PostUpdateScene(settings.SceneId):
                CognitiveStatics.PostNewScene(settings.SceneId);
            try
            {
                EditorNetwork.PostFile(url, tempMultipartPath, PostSceneUploadResponsePhase1, headers, true, "Upload", uploadMessage, WrapProgressCallback(0.0f, 0.5f));
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to start scene upload Phase 1: " + ex.Message);
                CleanupTempFile();
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
            }
        }

        /// <summary>
        /// Force update method that always uses PUT with force parameter for both phases
        /// Used when updating an existing scene version without creating a new version
        /// </summary>
        public static void UpdateDecimatedSceneOptimized(Cognitive3D_Preferences.SceneSettings settings, System.Action<int> uploadComplete, System.Action<float> progressCallback)
        {
            if (settings == null)
            {
                uploadComplete?.Invoke(0);
                return;
            }

            if (string.IsNullOrEmpty(settings.SceneId) || settings.VersionNumber <= 0)
            {
                Debug.LogError("Cannot force update: Scene must have existing SceneId and VersionNumber");
                uploadComplete?.Invoke(0);
                return;
            }

            UploadSceneSettingsOptimized = settings;
            UploadCompleteOptimized = uploadComplete;
            UploadProgressCallbackOptimized = progressCallback;

            string sceneName = settings.SceneName;
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

            if (!SceneExportDirExists || Directory.GetFiles(sceneExportDirectory).Length <= 1)
            {
                if (!EditorUtility.DisplayDialog("Force Update Scene", "Scene " + settings.SceneName + " has no exported geometry. Update anyway?", "Yes", "No"))
                {
                    UploadCompleteOptimized?.Invoke(0);
                    CleanupOptimizedUploadState();
                    return;
                }

                // Create settings.json file
                string objPath = EditorCore.GetSubDirectoryPath(sceneName);
                Directory.CreateDirectory(objPath);

                string escapedSceneName = settings.SceneName.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                string jsonSettingsContents = "{ \"scale\":1, \"sceneName\":\"" + escapedSceneName + "\",\"sdkVersion\":\"" + Cognitive3D_Manager.SDK_VERSION + "\"}";
                File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

                string debugContent = DebugInformationWindow.GetDebugContents();
                File.WriteAllText(objPath + "debug.log", debugContent);
            }

            // Classify files
            List<string> coreFiles;
            List<string> auxiliaryFiles;
            string screenshotFile;
            ClassifyFiles(sceneName, sceneExportDirectory, out coreFiles, out auxiliaryFiles, out screenshotFile);

            AuxiliaryFilesForPhase2 = auxiliaryFiles.ToArray();

            // Build multipart file list
            var filesWithFieldNames = new List<(string filePath, string fieldName, string fileName)>();

            foreach (var f in coreFiles)
            {
                if (f.ToLower().EndsWith(".ds_store"))
                    continue;
                filesWithFieldNames.Add((f, "file", null));
            }

            // Add screenshot
            if (!string.IsNullOrEmpty(screenshotFile) && File.Exists(screenshotFile))
            {
                filesWithFieldNames.Add((screenshotFile, "screenshot", "screenshot.png"));
                Util.logDebug("Added screenshot to Phase 1");
            }
            else
            {
                Util.logWarning("SceneExportWindow Upload can't find screenshot file");
            }

            string boundary;
            string tempMultipartPath = CreateStreamingMultipartFile(filesWithFieldNames, out boundary, "Preparing core files for update (Phase 1)");
            if (string.IsNullOrEmpty(tempMultipartPath))
            {
                Debug.LogError("Failed to create multipart file for Phase 1");
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
                return;
            }
            TempMultipartFilePath = tempMultipartPath;

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);

            // Always use PUT with force parameter for Phase 1
            string url = CognitiveStatics.PostUpdateSceneForce(settings.SceneId, settings.VersionNumber);
            try
            {
                EditorNetwork.PutFile(url, tempMultipartPath, PostSceneUpdateResponsePhase1, headers, true, "Update", "Force updating scene (Phase 1)", WrapProgressCallback(0.0f, 0.5f));
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to start force update Phase 1: " + ex.Message);
                CleanupTempFile();
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
            }
        }

        /// <summary>
        /// Callback from UpdateDecimatedSceneOptimized Phase 1
        /// </summary>
        static void PostSceneUpdateResponsePhase1(int responseCode, string error, string text)
        {
            CleanupTempFile();

            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogError("Scene Update Phase 1 Error: " + error);
                SegmentAnalytics.TrackEvent("UpdatingSceneError" + responseCode + "_Phase1", "SceneSetupSceneUpdatePage");
                if (responseCode != 100)
                    EditorUtility.DisplayDialog("Error Updating Scene", "Phase 1 failed with code " + responseCode + ".\n\nSee Console for details", "Ok");
                UploadCompleteOptimized?.Invoke(responseCode);
                CleanupOptimizedUploadState();
                return;
            }

            if (!string.IsNullOrEmpty(text) && (text.Contains("Internal Server Error") || text.Contains("Bad Request")))
            {
                Debug.LogError("Scene Update Phase 1 Error:" + text);
                EditorUtility.DisplayDialog("Error Updating Scene", "There was an internal error updating the scene (Phase 1). \n\nSee Console for more details", "Ok");
                UploadCompleteOptimized?.Invoke(responseCode);
                CleanupOptimizedUploadState();
                return;
            }

            UploadSceneSettingsOptimized.LastRevision = System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture);
            GUI.FocusControl("NULL");
            EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
            AssetDatabase.SaveAssets();

            SegmentAnalytics.TrackEvent("UpdatingSceneComplete_Phase1", "SceneSetupSceneUpdatePage");
            StartPhase2Update();
        }

        /// <summary>
        /// Initiates Phase 2 for forced update (auxiliary files)
        /// </summary>
        static void StartPhase2Update()
        {
            if (AuxiliaryFilesForPhase2 == null || AuxiliaryFilesForPhase2.Length == 0)
            {
                Util.logDebug("No auxiliary files to update. Skipping Phase 2.");
                UploadCompleteOptimized?.Invoke(200);

                Debug.Log("<color=green>Scene Update Complete!</color>");
                SegmentAnalytics.SegmentProperties props = new SegmentAnalytics.SegmentProperties();
                props.buttonName = "SceneSetupSceneUpdatePage";
                props.SetProperty("sceneVersion", UploadSceneSettingsOptimized.VersionNumber);
                SegmentAnalytics.TrackEvent("UpdatingSceneComplete_Optimized", props);

                CleanupOptimizedUploadState();
                return;
            }

            var filesWithFieldNames = new List<(string filePath, string fieldName, string fileName)>();
            foreach (var filePath in AuxiliaryFilesForPhase2)
            {
                filesWithFieldNames.Add((filePath, "file", null));
            }

            string boundary;
            string tempMultipartPath = CreateStreamingMultipartFile(filesWithFieldNames, out boundary, "Preparing auxiliary files for update (Phase 2)");
            if (string.IsNullOrEmpty(tempMultipartPath))
            {
                Debug.LogError("Failed to create multipart file for Phase 2");
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
                return;
            }
            TempMultipartFilePath = tempMultipartPath;

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);

            // Always use PUT with force parameter for Phase 2
            string url = CognitiveStatics.PostUpdateSceneForce(UploadSceneSettingsOptimized.SceneId, UploadSceneSettingsOptimized.VersionNumber);
            try
            {
                EditorNetwork.PutFile(url, tempMultipartPath, PostSceneUpdateResponsePhase2, headers, true, "Update", "Force updating auxiliary files (Phase 2)", WrapProgressCallback(0.5f, 1.0f));
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to start force update Phase 2: " + ex.Message);
                CleanupTempFile();
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
            }
        }

        /// <summary>
        /// Callback from UpdateDecimatedSceneOptimized Phase 2
        /// </summary>
        static void PostSceneUpdateResponsePhase2(int responseCode, string error, string text)
        {
            CleanupTempFile();

            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogWarning("Phase 2 failed (textures may be missing), but core scene updated successfully. Error: " + error);
                SegmentAnalytics.TrackEvent("UpdatingSceneError" + responseCode + "_Phase2", "SceneSetupSceneUpdatePage");
            }
            else
            {
                SegmentAnalytics.TrackEvent("UpdatingSceneComplete_Phase2", "SceneSetupSceneUpdatePage");

                // Update scene name after successful update
                UpdateSceneName(UploadSceneSettingsOptimized.SceneId, UploadSceneSettingsOptimized.SceneName);
            }

            UploadCompleteOptimized?.Invoke(200);

            Debug.Log("<color=green>Scene Update Complete!</color>");
            SegmentAnalytics.SegmentProperties props = new SegmentAnalytics.SegmentProperties();
            props.buttonName = "SceneSetupSceneUpdatePage";
            props.SetProperty("sceneVersion", UploadSceneSettingsOptimized.VersionNumber);
            SegmentAnalytics.TrackEvent("UpdatingSceneComplete_Optimized", props);

            CleanupOptimizedUploadState();
        }

        /// <summary>
        /// Callback from UploadDecimatedSceneOptimized Phase 1 (core files upload)
        /// On success, proceeds to Phase 2 (auxiliary files upload)
        /// </summary>
        static void PostSceneUploadResponsePhase1(int responseCode, string error, string text)
        {
            CleanupTempFile();

            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogError("Scene Upload Phase 1 Error: " + error);
                SegmentAnalytics.TrackEvent("UploadingSceneError" + responseCode + "_Phase1", "SceneSetupSceneUploadPage");
                if (responseCode != 100) // User cancelled
                    EditorUtility.DisplayDialog("Error Uploading Scene", "Phase 1 failed with code " + responseCode + ".\n\nSee Console for details", "Ok");
                UploadCompleteOptimized?.Invoke(responseCode);
                CleanupOptimizedUploadState();
                return;
            }

            // Check for internal server error
            if (!string.IsNullOrEmpty(text) && (text.Contains("Internal Server Error") || text.Contains("Bad Request")))
            {
                Debug.LogError("Scene Upload Phase 1 Error:" + text);
                EditorUtility.DisplayDialog("Error Uploading Scene", "There was an internal error uploading the scene (Phase 1). \n\nSee Console for more details", "Ok");
                UploadCompleteOptimized?.Invoke(responseCode);
                CleanupOptimizedUploadState();
                return;
            }

            UploadSceneSettingsOptimized.LastRevision = System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture);
            GUI.FocusControl("NULL");
            EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
            AssetDatabase.SaveAssets();

            SegmentAnalytics.TrackEvent("UploadingSceneComplete_Phase1", "SceneSetupSceneUploadPage");
            StartPhase2Upload();
        }

        /// <summary>
        /// Initiates Phase 2 upload for auxiliary files using streaming multipart.
        /// Builds multipart body on disk by streaming files in small chunks (4KB buffer).
        /// This approach keeps memory usage minimal (~4KB) while maintaining backend compatibility.
        /// Skips upload if there are no auxiliary files.
        /// </summary>
        static void StartPhase2Upload()
        {
            // Skip Phase 2 if there are no auxiliary files
            if (AuxiliaryFilesForPhase2 == null || AuxiliaryFilesForPhase2.Length == 0)
            {
                Util.logDebug("No auxiliary files to upload. Skipping Phase 2.");
                UploadCompleteOptimized?.Invoke(200);

                Debug.Log("<color=green>Scene Upload Complete!</color>");
                SegmentAnalytics.SegmentProperties props = new SegmentAnalytics.SegmentProperties();
                props.buttonName = "SceneSetupSceneUploadPage";
                props.SetProperty("sceneVersion", UploadSceneSettingsOptimized.VersionNumber + 1);
                SegmentAnalytics.TrackEvent("UploadingSceneComplete_Optimized", props);

                CleanupOptimizedUploadState();
                return;
            }

            // Convert auxiliary files to tuple format with default "file" field name
            var filesWithFieldNames = new List<(string filePath, string fieldName, string fileName)>();
            foreach (var filePath in AuxiliaryFilesForPhase2)
            {
                filesWithFieldNames.Add((filePath, "file", null));
            }

            string boundary;
            string tempMultipartPath = CreateStreamingMultipartFile(filesWithFieldNames, out boundary, "Preparing auxiliary files (Phase 2)");
            if (string.IsNullOrEmpty(tempMultipartPath))
            {
                Debug.LogError("Failed to create multipart file for Phase 2");
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
                return;
            }
            TempMultipartFilePath = tempMultipartPath;

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            headers.Add("Content-Type", "multipart/form-data; boundary=" + boundary);

            string url = CognitiveStatics.PostUpdateScene(UploadSceneSettingsOptimized.SceneId);
            try
            {
                EditorNetwork.PutFile(url, tempMultipartPath, PostSceneUploadResponsePhase2, headers, true, "Upload", "Uploading auxiliary files (Phase 2)", WrapProgressCallback(0.5f, 1.0f));
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Failed to start scene upload Phase 2: " + ex.Message);
                CleanupTempFile();
                UploadCompleteOptimized?.Invoke(500);
                CleanupOptimizedUploadState();
            }
        }

        /// <summary>
        /// Creates a multipart form data file on disk by streaming source files in small chunks.
        /// Memory usage is limited to the buffer size (~4KB) regardless of total file sizes.
        /// </summary>
        /// <param name="filesWithFieldNames">List of (filePath, fieldName, overrideFileName) tuples. If overrideFileName is null, uses actual filename.</param>
        /// <param name="boundary">Output: the boundary string used in the multipart format</param>
        /// <param name="progressMessage">Optional message to display in progress bar</param>
        /// <returns>Path to the created multipart file, or null if creation failed</returns>
        private static string CreateStreamingMultipartFile(List<(string filePath, string fieldName, string fileName)> filesWithFieldNames, out string boundary, string progressMessage = "Preparing upload files")
        {
            boundary = MultipartBoundaryPrefix + System.DateTime.Now.Ticks.ToString("x");
            string outputPath = Path.Combine(Path.GetTempPath(), "cognitive3d_multipart_" + System.DateTime.Now.Ticks + ".bin");
            byte[] buffer = new byte[StreamingBufferSize];

            // Calculate total size for progress reporting
            long totalSize = 0;
            foreach (var (filePath, _, _) in filesWithFieldNames)
            {
                if (File.Exists(filePath))
                {
                    FileInfo fi = new FileInfo(filePath);
                    totalSize += fi.Length;
                }
            }

            try
            {
                long processedSize = 0;
                int fileIndex = 0;

                using (FileStream outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, StreamingBufferSize))
                {
                    byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "\r\n");
                    byte[] crlf = System.Text.Encoding.UTF8.GetBytes("\r\n");

                    foreach (var (filePath, fieldName, overrideFileName) in filesWithFieldNames)
                    {
                        if (!File.Exists(filePath))
                        {
                            Debug.LogWarning("Skipping missing file: " + filePath);
                            continue;
                        }

                        string name = overrideFileName ?? Path.GetFileName(filePath);
                        if (name.ToLower().EndsWith(".ds_store"))
                            continue;

                        fileIndex++;
                        float progress = totalSize > 0 ? (float)processedSize / totalSize : (float)fileIndex / filesWithFieldNames.Count;
                        EditorUtility.DisplayProgressBar("Preparing Upload", progressMessage + $" ({fileIndex}/{filesWithFieldNames.Count})", progress);

                        outputStream.Write(boundaryBytes, 0, boundaryBytes.Length);

                        string header = "Content-Disposition: form-data; name=\"" + fieldName + "\"; filename=\"" + name + "\"\r\n" +
                                        "Content-Type: application/octet-stream\r\n\r\n";
                        byte[] headerBytes = System.Text.Encoding.UTF8.GetBytes(header);
                        outputStream.Write(headerBytes, 0, headerBytes.Length);

                        using (FileStream sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamingBufferSize))
                        {
                            int bytesRead;
                            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outputStream.Write(buffer, 0, bytesRead);
                                processedSize += bytesRead;

                                // Update progress bar periodically
                                if (totalSize > 0 && processedSize % (StreamingBufferSize * 10) == 0)
                                {
                                    progress = (float)processedSize / totalSize;
                                    EditorUtility.DisplayProgressBar("Preparing Upload", progressMessage + $" ({fileIndex}/{filesWithFieldNames.Count})", progress);
                                }
                            }
                        }

                        outputStream.Write(crlf, 0, crlf.Length);
                    }

                    byte[] finalBoundary = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");
                    outputStream.Write(finalBoundary, 0, finalBoundary.Length);
                }

                EditorUtility.ClearProgressBar();
                return outputPath;
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("Failed to create streaming multipart file: " + ex.Message);
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                return null;
            }
        }

        // Constants for streaming multipart upload
        private const int StreamingBufferSize = 4096; // 4KB buffer
        private const string MultipartBoundaryPrefix = "----Cognitive3DBoundary";

        /// <summary>
        /// Path to the current temporary multipart file (reused for both phases)
        /// </summary>
        static string TempMultipartFilePath;

        /// <summary>
        /// Callback from Phase 2 (auxiliary files upload)
        /// Phase 1 already succeeded, so report success even if Phase 2 fails
        /// </summary>
        static void PostSceneUploadResponsePhase2(int responseCode, string error, string text)
        {
            CleanupTempFile();

            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogWarning("Phase 2 failed (textures may be missing), but core scene uploaded successfully. Error: " + error);
                SegmentAnalytics.TrackEvent("UploadingSceneError" + responseCode + "_Phase2", "SceneSetupSceneUploadPage");
            }
            else
            {
                SegmentAnalytics.TrackEvent("UploadingSceneComplete_Phase2", "SceneSetupSceneUploadPage");

                // Update scene name after successful upload
                UpdateSceneName(UploadSceneSettingsOptimized.SceneId, UploadSceneSettingsOptimized.SceneName);
            }

            UploadCompleteOptimized?.Invoke(200);

            Debug.Log("<color=green>Scene Upload Complete!</color>");
            SegmentAnalytics.SegmentProperties props = new SegmentAnalytics.SegmentProperties();
            props.buttonName = "SceneSetupSceneUploadPage";
            props.SetProperty("sceneVersion", UploadSceneSettingsOptimized.VersionNumber + 1);
            SegmentAnalytics.TrackEvent("UploadingSceneComplete_Optimized", props);

            CleanupOptimizedUploadState();
        }

        /// <summary>
        /// Cleans up the temporary multipart file used for streaming upload
        /// </summary>
        private static void CleanupTempFile()
        {
            if (!string.IsNullOrEmpty(TempMultipartFilePath) && File.Exists(TempMultipartFilePath))
            {
                try
                {
                    File.Delete(TempMultipartFilePath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("Failed to delete temporary file: " + ex.Message);
                }
            }
            TempMultipartFilePath = null;
        }

        /// <summary>
        /// Updates the scene name on the server after scene upload or update completes
        /// </summary>
        private static void UpdateSceneName(string sceneId, string sceneName)
        {
            if (string.IsNullOrEmpty(sceneId) || string.IsNullOrEmpty(sceneName))
            {
                Util.logDebug("UpdateSceneName: Missing sceneId or sceneName, skipping update");
                return;
            }

            string url = CognitiveStatics.PostUpdateScene(sceneId);
            string jsonBody = "{\"sceneName\":\"" + sceneName + "\"}";

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
            {
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
            }
            headers.Add("Content-Type", "application/json");

            EditorNetwork.Patch(url, jsonBody, UpdateSceneNameResponse, headers, false, "Patch", "Updating scene name");
        }

        /// <summary>
        /// Callback from UpdateSceneName PATCH request
        /// </summary>
        static void UpdateSceneNameResponse(int responseCode, string error, string text)
        {
            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogWarning("Failed to update scene name. Error: " + error);
            }
            else
            {
                Util.logDebug("Scene name updated successfully");
            }
        }
    
        // State variables for optimized two-phase upload
        static Cognitive3D_Preferences.SceneSettings UploadSceneSettingsOptimized;
        static System.Action<int> UploadCompleteOptimized;
        static System.Action<float> UploadProgressCallbackOptimized;
        static string[] AuxiliaryFilesForPhase2;

        /// <summary>
        /// Classifies export files into core scene files (gltf, bin, settings.json) and auxiliary files (textures, debug.log)
        /// </summary>
        private static void ClassifyFiles(
            string sceneName,
            string sceneExportDirectory,
            out List<string> coreFiles,
            out List<string> auxiliaryFiles,
            out string screenshotFile)
        {
            coreFiles = new List<string>();
            auxiliaryFiles = new List<string>();
            screenshotFile = null;

            if (!Directory.Exists(sceneExportDirectory))
            {
                return;
            }

            string[] allFiles = Directory.GetFiles(sceneExportDirectory);

            foreach (var filePath in allFiles)
            {
                string fileName = Path.GetFileName(filePath);
                string lowerFileName = fileName.ToLower();

                // Skip system files
                if (lowerFileName.EndsWith(".ds_store") || lowerFileName.EndsWith(".meta"))
                    continue;

                // Core files: scene.gltf, scene.bin, settings.json
                if (lowerFileName == "scene.gltf" || lowerFileName == sceneName.ToLower() + ".gltf" ||
                    lowerFileName == "scene.bin" || lowerFileName == sceneName.ToLower() + ".bin" ||
                    lowerFileName == "settings.json")
                {
                    coreFiles.Add(filePath);
                }
                // Auxiliary files: textures and debug log
                else if (lowerFileName.EndsWith(".png") || lowerFileName == "debug.log")
                {
                    auxiliaryFiles.Add(filePath);
                }
            }

            // Get screenshot from screenshot subdirectory
            string screenshotDir = sceneExportDirectory + "screenshot";
            if (Directory.Exists(screenshotDir))
            {
                string[] screenshots = Directory.GetFiles(screenshotDir);
                if (screenshots.Length > 0)
                {
                    screenshotFile = screenshots[0];
                }
            }
        }

        /// <summary>
        /// Wraps a progress callback to remap progress from 0-1 to a specific range (min to max)
        /// </summary>
        private static System.Action<float> WrapProgressCallback(float min, float max)
        {
            if (UploadProgressCallbackOptimized == null)
                return null;

            return (progress) =>
            {
                float mappedProgress = min + (progress * (max - min));
                UploadProgressCallbackOptimized?.Invoke(mappedProgress);
            };
        }

        /// <summary>
        /// Cleans up static state variables used during optimized upload
        /// </summary>
        private static void CleanupOptimizedUploadState()
        {
            UploadSceneSettingsOptimized = null;
            UploadCompleteOptimized = null;
            UploadProgressCallbackOptimized = null;
            AuxiliaryFilesForPhase2 = null;

            // Safety net cleanup for temp streaming file
            CleanupTempFile();
        }

        #region (Old) Upload Scene

        static System.Action<int> UploadComplete;
        //displays popup window confirming upload, then uploads the files

        /// <summary>
        /// displays confirmation popup
        /// reads files from export directory and sends POST request to backend
        /// invokes uploadComplete if upload actually starts and PostSceneUploadResponse callback gets 200/201 responsecode
        /// </summary>
        public static void UploadDecimatedScene(Cognitive3D_Preferences.SceneSettings settings, System.Action<int> uploadComplete, System.Action<float> progressCallback)
        {
            //if uploadNewScene POST
            //else PUT to sceneexplorer/sceneid

            if (settings == null) { UploadSceneSettings = null; return; }

            UploadSceneSettings = settings;

            bool hasExistingSceneId = settings != null && !string.IsNullOrEmpty(settings.SceneId);

            bool uploadConfirmed = false;
            string sceneName = settings.SceneName;
            string[] filePaths = new string[] { };

            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + settings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

            if (SceneExportDirExists)
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar);
            }

            //custom confirm upload popup windows
            if ((!SceneExportDirExists || filePaths.Length <= 1))
            {
                if (EditorUtility.DisplayDialog("Upload Scene", "Scene " + settings.SceneName + " has no exported geometry. Upload anyway?", "Yes", "No"))
                {
                    uploadConfirmed = true;
                    //create a json.settings file in the directory
                    string objPath = EditorCore.GetSubDirectoryPath(sceneName);

                    Directory.CreateDirectory(objPath);

                    string escapedSceneName = settings.SceneName.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                    string jsonSettingsContents = "{ \"scale\":1, \"sceneName\":\"" + escapedSceneName + "\",\"sdkVersion\":\"" + Cognitive3D_Manager.SDK_VERSION + "\"}";
                    File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

                    string debugContent = DebugInformationWindow.GetDebugContents();
                    File.WriteAllText(objPath + "debug.log", debugContent);
                }
            }
            else
            {
                uploadConfirmed = true;
            }

            if (!uploadConfirmed)
            {
                UploadSceneSettings = null;
                return; //just exit now
            }

            //after confirmation because uploading an empty scene creates a settings.json file
            if (Directory.Exists(sceneExportDirectory))
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar);
            }

            string[] screenshotPath = new string[0];
            if (Directory.Exists(sceneExportDirectory + "screenshot"))
            {
                screenshotPath = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "Cognitive3D_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");
            }
            else
            {
                Util.logDebug("SceneExportWindow Upload can't find directory to screenshot");
            }

            System.Text.StringBuilder fileList = new System.Text.StringBuilder("Upload Files:\n");
            WWWForm wwwForm = new WWWForm();
            foreach (var f in filePaths)
            {
                if (f.ToLower().EndsWith(".ds_store"))
                {
                    Util.logDebug("skip file " + f);
                    continue;
                }

                fileList.Append(f);
                fileList.Append('\n');

                var data = File.ReadAllBytes(f);
                wwwForm.AddBinaryData("file", data, Path.GetFileName(f));
            }

            Util.logDebug(fileList.ToString());

            if (screenshotPath.Length == 0)
            {
                Util.logDebug("SceneExportWindow Upload can't find files in screenshot directory");
            }
            else
            {
                wwwForm.AddBinaryData("screenshot", File.ReadAllBytes(screenshotPath[0]), "screenshot.png");
            }

            if (hasExistingSceneId) //upload new verison of existing scene
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (EditorCore.IsDeveloperKeyValid)
                {
                    headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                    foreach (var v in wwwForm.headers)
                    {
                        headers[v.Key] = v.Value;
                    }
                }
                EditorNetwork.Post(CognitiveStatics.PostUpdateScene(settings.SceneId), wwwForm.data, PostSceneUploadResponse, headers, true, "Upload", "Uploading new version of scene", progressCallback);//AUTH
            }
            else //upload as new scene
            {
                Dictionary<string, string> headers = new Dictionary<string, string>();
                if (EditorCore.IsDeveloperKeyValid)
                {
                    headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);
                    foreach (var v in wwwForm.headers)
                    {
                        headers[v.Key] = v.Value;
                    }
                }
                EditorNetwork.Post(CognitiveStatics.PostNewScene(), wwwForm.data, PostSceneUploadResponse, headers, true, "Upload", "Uploading new scene", progressCallback);//AUTH
            }

            UploadComplete = uploadComplete;
        }

        /// <summary>
        /// callback from UploadDecimatedScene
        /// </summary>
        static void PostSceneUploadResponse(int responseCode, string error, string text)
        {
            Util.logDebug("UploadScene Response. [RESPONSE CODE] " + responseCode
                + (!string.IsNullOrEmpty(error) ? " [ERROR] " + error : "")
                + (!string.IsNullOrEmpty(text) ? " [TEXT] " + text : ""));

            if (responseCode != 200 && responseCode != 201)
            {
                Debug.LogError("Scene Upload Error " + error);
                SegmentAnalytics.TrackEvent("UploadingSceneError" + responseCode + "_SceneUploadPage", "SceneSetupSceneUploadPage");
                if (responseCode != 100) //ie user cancelled upload
                {
                    EditorUtility.DisplayDialog("Error Uploading Scene", "There was an error uploading the scene. Response code was " + responseCode + ".\n\nSee Console for more details", "Ok");
                }
                UploadComplete.Invoke(responseCode);
                UploadSceneSettings = null;
                UploadComplete = null;
                return;
            }

            if (!string.IsNullOrEmpty(text) && (text.Contains("Internal Server Error") || text.Contains("Bad Request")))
            {
                Debug.LogError("Scene Upload Error:" + text);
                EditorUtility.DisplayDialog("Error Uploading Scene", "There was an internal error uploading the scene. \n\nSee Console for more details", "Ok");
                UploadComplete.Invoke(responseCode);
                UploadSceneSettings = null;
                UploadComplete = null;
                return;
            }

            string responseText = text.Replace("\"", "");
            if (!string.IsNullOrEmpty(responseText)) //uploading a new version returns empty. uploading a new scene returns sceneid
            {
                EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
                UploadSceneSettings.SceneId = responseText;
                AssetDatabase.SaveAssets();
            }

            UploadSceneSettings.LastRevision = System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture);
            GUI.FocusControl("NULL");
            EditorUtility.SetDirty(Cognitive3D_Preferences.Instance);
            AssetDatabase.SaveAssets();

            if (UploadComplete != null)
            {
                UploadComplete.Invoke(responseCode);
            }
            UploadComplete = null;

            Debug.Log("<color=green>Scene Upload Complete!</color>");
            SegmentAnalytics.SegmentProperties props = new SegmentAnalytics.SegmentProperties();
            props.buttonName = "SceneSetupSceneUploadPage";
            props.SetProperty("sceneVersion", UploadSceneSettings.VersionNumber+1);
            SegmentAnalytics.TrackEvent("UploadingSceneComplete_SceneUploadPage", props);
        }

        static Cognitive3D_Preferences.SceneSettings UploadSceneSettings;
        /// <summary>
        /// SceneSettings for the currently uploading scene
        /// </summary>
        public static void ClearUploadSceneSettings() //sometimes not set to null when init window quits
        {
            UploadSceneSettings = null;
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
    #endregion
    }
}
