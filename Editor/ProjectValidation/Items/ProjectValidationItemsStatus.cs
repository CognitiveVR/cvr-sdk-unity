using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItemsStatus
    {
        private const string LOG_TAG = "[COGNITIVE3D] ";
        static Dictionary<string, List<ProjectValidation.ItemLevel>> scenesNeedFix = new Dictionary<string, List<ProjectValidation.ItemLevel>>();
        internal static bool throwExecption;
        internal static bool isProjectVerified;

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
            ProjectValidation.Reset();
            ProjectValidationItems.WaitBeforeProjectValidation();
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
                    bool result = EditorUtility.DisplayDialog(LOG_TAG + "Project Validation Alert", "Cognitive3D project validation has detected unresolved issues! \n" + sceneList, "More Details", "Ignore");
                    if (result)
                    {
                        // Opens up the first scene in the list that needs fix
                        EditorSceneManager.OpenScene(scenesNeedFix.Keys.First().ToString());
                        ProjectValidationSettingsProvider.OpenSettingsWindow();
                        throwExecption = true;
                    }
                    else
                    {
                        isProjectVerified = false;
                    }

                    Clear();
                    return;
                }
                else
                {
                    Util.logDebug("No issues were found in project");
                    isProjectVerified = true;
                }
            }

            throwExecption = false;
            Clear();
        }

        /// <summary>
        /// iterates through all build scenes in the project to identify outstanding project validation items
        /// </summary>
        internal static async void VerifyAllBuildScenes()
        {
            // Popup
            bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Would you like to perform Cognitive3D project validation by verifying all build scenes? \n \nPress \"Yes\" to verify scenes or \"No\" to continue build process", "Yes", "No");
            if (result && EditorBuildSettings.scenes.Length != 0)
            {
                throwExecption = true;

                for (int i = 0; i < EditorBuildSettings.scenes.Length; i += 1)
                {
                    float progress = (float)i / EditorBuildSettings.scenes.Length;
                    EditorUtility.DisplayProgressBar(LOG_TAG + "Project Validation", "Verifying scenes...", progress);

                    // Open scene
                    EditorSceneManager.OpenScene(EditorBuildSettings.scenes[i].path);

                    // Time needed for updating project validation items in a scene
                    await Task.Delay(1000);

                    // Check if project has not fixed items
                    if (ProjectValidation.hasNotFixedItems())
                    {
                        var sceneLevelItems = ProjectValidation.GetLevelsOfItemsNotFixed().ToList();
                        AddOrUpdateDictionary(scenesNeedFix, EditorBuildSettings.scenes[i].path, sceneLevelItems);
                    }
                }

                // Clear progress bar
                EditorUtility.ClearProgressBar();

                DisplayScenesWithValidationItems();
            }
            else
            {
                throwExecption = false;
                return;
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
        /// Iterate through all scenes in build settings to check if target scene exists
        /// </summary>
        /// <param name="targetScene"></param>
        /// <returns></returns>
        internal static bool TryGetSceneInBuildSettings(Scene targetScene)
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path.Contains(targetScene.name))
                {
                    return true;
                }
            }
            return false;
        }
    }
}