using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using UnityEditor.Build;
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

        // Adding scenes that has unfixed items 
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

        static void AddOrUpdateSceneValidationStatus(Scene scene)
        {
            if (ProjectValidation.hasNotFixedItems())
            {
                AddOrUpdateDictionary(scenesNeedFix, GetSceneName(scene.path), scene.path);
            }
        }

        internal static bool VerifyCurrentSceneValidationItems()
        {
            if (ProjectValidation.hasNotFixedItems())
            {
                bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Cognitive3D project validation has identified unresolved issues that may result in inaccurate data recording", "Fix", "Ignore");
                if (result)
                {
                    ProjectValidationSettingsProvider.OpenSettingsWindow();
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return true;
        }

        /// <summary>
        /// iterates through all build scenes in the project to identify outstanding project validation items
        /// </summary>
        internal static void VerifyBuildScenesValidationItems()
        {
            if (scenesNeedFix != null && scenesNeedFix.Count != 0)
            {
                // Concatenate scene names into a single string
                string sceneList = string.Join(", ", scenesNeedFix.Keys);

                Debug.LogError(LOG_TAG + "Found unresolved issues in the following scenes: "  + sceneList);
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
                    throwExecption = false;
                    Clear();
                    return;
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
            bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Would you like to perform Cognitive3D project validation by verifying all build scenes?", "Yes", "No");
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
                    await Task.Delay(2000);

                    // // Update progress bar one more time after the delay
                    // float nextProgress = (float)(i + 1) / EditorBuildSettings.scenes.Length;
                    // EditorUtility.DisplayProgressBar(LOG_TAG + "Project Validation", "Verifying scenes...", nextProgress);

                    // Check if project has not fixed items
                    if (ProjectValidation.hasNotFixedItems())
                    {
                        AddOrUpdateDictionary(scenesNeedFix, GetSceneName(EditorBuildSettings.scenes[i].path), EditorBuildSettings.scenes[i].path);
                    }
                }

                // Clear progress bar
                EditorUtility.ClearProgressBar();

                VerifyBuildScenesValidationItems();
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
    }
}
