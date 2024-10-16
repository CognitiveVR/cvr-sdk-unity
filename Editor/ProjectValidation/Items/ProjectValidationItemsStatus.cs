using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal static class ProjectValidationItemsStatus
    {
        private const string LOG_TAG = "[COGNITIVE3D] ";
        static Dictionary<string, List<ProjectValidation.ItemLevel>> scenesNeedFix = new Dictionary<string, List<ProjectValidation.ItemLevel>>();
        private static bool _throwBuildException;
        public static bool throwBuildException {
            get {
                return _throwBuildException;
            }
            internal set {
                _throwBuildException = value;
            }
        }

        static ProjectValidationItemsStatus()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        internal static string GetCurrentSceneName()
        {
            return EditorSceneManager.GetActiveScene().name;
        }

        // Update project validation items when a new scene opens
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ProjectValidation.RegenerateItems();
        }

        /// <summary>
        /// Displays a popup indicating scenes that require fixes
        /// </summary>
        internal static void DisplayScenesWithValidationItems()
        {
            if (scenesNeedFix != null)
            {
                if (scenesNeedFix.Count != 0)
                {
                    // Extract just the scene names from the dictionary keys
                    List<string> sceneNames = new List<string>();
                    foreach (string scenePath in scenesNeedFix.Keys)
                    {
                        string sceneName = GetSceneName(scenePath);
                        sceneNames.Add(sceneName);
                    }

                    // Join the scene names with commas
                    string allSceneNames = string.Join(", ", sceneNames);

                    Util.logError("Found unresolved issues in the following scenes: "  + allSceneNames);

                    List<string> sceneRequiredNames = new List<string>();
                    List<string> sceneRecommendedNames = new List<string>();
                    foreach (var scene in scenesNeedFix)
                    {
                        if (scene.Value.Contains(ProjectValidation.ItemLevel.Required))
                        {
                            sceneRequiredNames.Add(GetSceneName(scene.Key));
                        }

                        if (scene.Value.Contains(ProjectValidation.ItemLevel.Recommended))
                        {
                            sceneRecommendedNames.Add(GetSceneName(scene.Key));
                        }
                    }

                    string sceneList = "";

                    sceneList += sceneRequiredNames?.Count > 0 ? $"\nRequired tasks:\n{string.Join(", ", sceneRequiredNames)}\n" : "";
                    sceneList += sceneRecommendedNames?.Count > 0 ? $"\nRecommended tasks:\n{string.Join(", ", sceneRecommendedNames)}\n" : "";

                    // Popup
                    bool result = EditorUtility.DisplayDialog(LOG_TAG + "Project Validation Alert", "Cognitive3D project validation has been completed. Unresolved issues have been detected. \n" + sceneList, "More Details", "Ignore");
                    if (result)
                    {
                        // Opens up the first scene in the list that needs fix
                        EditorSceneManager.OpenScene(scenesNeedFix.Keys.First().ToString());
                        ProjectValidationSettingsProvider.OpenSettingsWindow();
                        throwBuildException = true;
                    }

                    Clear();
                    return;
                }
                else
                {
                    Util.logDebug("No issues were found in project");
                    OnProjectVerified();

                    EditorUtility.DisplayDialog(LOG_TAG + "Project Validation Alert", "Cognitive3D project validation has been successfully completed. \n \nNo issues were detected! You are now clear to build your app.", "OK");
                }
            }

            throwBuildException = false;
            Clear();
        }

        /// <summary>
        /// iterates through all build scenes in the project to identify outstanding project validation items
        /// </summary>
        internal static void VerifyAllBuildScenes()
        {
            if (ProjectValidationLog.GetBuildProcessPopup())
            {
                // Popup
                bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Would you like to verify that all build scenes meet the Cognitive3D configuration requirements? \n \n**Please note that if you choose to verify scenes, the build process will be stopped and will need to be restarted**", "Verify", "Skip");
                if (result)
                {
                    throwBuildException = true;
                    StartSceneVerificationProcess();
                    return;
                }
                else
                {
                    throwBuildException = false;
                    return;
                }
            }
        }

        // Extracts scene name from the full scene path
        private static string GetSceneName(string scenePath)
        {
            // Use Unity's AssetDatabase to extract the file name from the path
            string sceneNameWithExtension = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            // The scene name is the file name without the extension
            return sceneNameWithExtension;
        }

        private static void Clear()
        {
            scenesNeedFix.Clear();
        }

        private static void AddOrUpdateDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                // Update the value for the existing key
                dictionary[key] = value;
            }
            else
            {
                // Add a new key-value pair to the dictionary
                dictionary.Add(key, value);
            }
        }

        /// <summary>
        /// Starts process for verifying project validation items in each scene (in build settings)
        /// </summary>
        internal static async void StartSceneVerificationProcess()
        {
            int uploadedScenes = 0;

            // Checking if the scene has unsaved changes
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                if (TryGetScenesInBuildSettings(out var activeBuildScenes))
                {
                    // Checking if at least one build scene has been uploaded
                    for (int i = 0; i < activeBuildScenes.Count; i++)
                    {
                        if (Cognitive3D_Preferences.FindSceneByPath(activeBuildScenes[i].path) != null)
                        {
                            ++uploadedScenes;
                        }
                    }

                    if (uploadedScenes <= 0)
                    {
                        // Warn the user to upload at least one build scene if none have been uploaded
                        EditorUtility.DisplayDialog(LOG_TAG + "Project Validation Alert", "No build scenes have been uploaded. Please upload at least one scene.", "OK");
                    }
                    else
                    {
                        for (int i = 0; i < activeBuildScenes.Count; i++)
                        {
                            float progress = (float)i / activeBuildScenes.Count;
                            EditorUtility.DisplayProgressBar(LOG_TAG + "Project Validation", "Verifying scenes...", progress);

                            // Open scene
                            EditorSceneManager.OpenScene(activeBuildScenes[i].path);

                            // Time needed for updating project validation items in a scene
                            await Task.Delay(1000);

                            // Check if project has not-fixed items for scenes that has been uploaded and has scene ID
                            if (Cognitive3D_Preferences.FindSceneByPath(activeBuildScenes[i].path) != null && ProjectValidation.hasNotFixedItems())
                            {
                                var sceneLevelItems = ProjectValidation.GetLevelsOfItemsNotFixed().ToList();
                                AddOrUpdateDictionary(scenesNeedFix, activeBuildScenes[i].path, sceneLevelItems);
                            }
                        }

                        // Clear progress bar
                        EditorUtility.ClearProgressBar();

                        DisplayScenesWithValidationItems();
                    }
                }
            }
        }

        /// <summary>
        /// Writes into JSON if all items are verified (no issues)
        /// </summary>
        internal static void OnProjectVerified()
        {
            ProjectValidationLog.UpdateLog();
        }

        /// <summary>
        /// Iterate through all scenes in build settings to check if scene is active and enabled in build settings
        /// </summary>
        /// <param name="scenes">returns enabled scenes</param>
        /// <returns></returns>
        internal static bool TryGetScenesInBuildSettings(out List<EditorBuildSettingsScene> scenes)
        {
            scenes = new List<EditorBuildSettingsScene>();
            if (EditorBuildSettings.scenes.Length != 0)
            {
                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    // Checks if scene is enabled in build settings
                    if (scene.enabled)
                    {
                        scenes.Add(scene);
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
