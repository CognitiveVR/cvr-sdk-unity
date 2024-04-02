using UnityEngine;
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
        static Dictionary<string, string> scenesNeedFix = new Dictionary<string, string>();
        internal static bool throwExecption;
        internal static bool isProjectVerified;

        static ProjectValidationItemsStatus()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        internal static string GetCurrentSceneName()
        {
            return EditorSceneManager.GetActiveScene().name;
        }

        private static void OnSceneClosed(Scene scene)
        {
            AddOrUpdateSceneValidationStatus(scene);
        }

        private static void OnSceneSaved(Scene scene)
        {
            AddOrUpdateSceneValidationStatus(scene);
        }

        // Update project validation items when a new scene opens
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            ProjectValidation.Reset();
            ProjectValidationItems.WaitBeforeProjectValidation();
        }

        // This function adds or updates scenes in a dictionary if they exist in the build settings and require items to be fixed
        static void AddOrUpdateSceneValidationStatus(Scene scene)
        {
            if (ProjectValidation.hasNotFixedItems() && TryGetSceneInBuildSettings(scene))
            {
                AddOrUpdateDictionary(scenesNeedFix, GetSceneName(scene.path), scene.path);
            }
        }

        /// <summary>
        /// Displays a pop indicating current scene that require fixes
        /// </summary>
        /// <returns></returns>
        internal static void VerifyCurrentSceneValidationItems()
        {
            if (ProjectValidation.hasNotFixedItems())
            {
                bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Cognitive3D project validation has identified unresolved issues that may result in inaccurate data recording", "Fix", "Ignore");
                if (result)
                {
                    ProjectValidationSettingsProvider.OpenSettingsWindow();
                    throwExecption = true;
                    return;
                }
                else
                {
                    throwExecption = false;
                    return;
                }
            }

            throwExecption = false;
            return;
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
                    // Concatenate scene names into a single string
                    string sceneList = string.Join(", ", scenesNeedFix.Keys);

                    Util.logError("Found unresolved issues in the following scenes: "  + sceneList);

                    // Popup
                    bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Cognitive3D project validation has found unresolved issues in the following scenes: \n" + sceneList, "Fix", "Ignore");
                    if (result)
                    {
                        // Opens up the first scene in the list that needs fix
                        EditorSceneManager.OpenScene(scenesNeedFix.Values.First().ToString());
                        ProjectValidationSettingsProvider.OpenSettingsWindow();
                        throwExecption = true;
                        Clear();
                        return;
                    }
                    else
                    {
                        isProjectVerified = false;
                        Clear();
                        return;
                    }
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
                        AddOrUpdateDictionary(scenesNeedFix, GetSceneName(EditorBuildSettings.scenes[i].path), EditorBuildSettings.scenes[i].path);
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
