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
        internal static Dictionary<string, bool> sceneVaidationStatusDic = new Dictionary<string, bool>();
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
            AddOrUpdateDictionary(sceneVaidationStatusDic, scene.path, ProjectValidation.hasNotFixedItems());
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

        internal static void VerifyBuildScenesValidationItems()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                // Check if the scene path exists in the dictionary
                if (sceneVaidationStatusDic.ContainsKey(scene.path) && sceneVaidationStatusDic[scene.path] == true)
                {
                    AddOrUpdateDictionary(scenesNeedFix, GetSceneName(scene.path), scene.path);
                }
            }

            if (scenesNeedFix != null && scenesNeedFix.Count != 0)
            {
                // Concatenate scene names into a single string
                string sceneList = string.Join(", ", scenesNeedFix.Keys);

                bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Cognitive3D project validation has found unresolved issues in the following scenes: \n" + sceneList, "Fix", "Ignore");
                if (result)
                {
                    // Opens up the first scene in the list that needs fix
                    EditorSceneManager.OpenScene(scenesNeedFix.Values.First().ToString());
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
        }

        internal static async void VerifyAllBuildScenes()
        {
            bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Would you like to verify all build scenes for Cognitive3D project validation?", "Yes", "No");
            if (result)
            {
                throwExecption = true;

                foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
                {
                    EditorSceneManager.OpenScene(scene.path);

                    await Task.Delay(2000);

                    if (ProjectValidation.hasNotFixedItems())
                    {
                        AddOrUpdateDictionary(sceneVaidationStatusDic, scene.path, ProjectValidation.hasNotFixedItems());
                    }
                }

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
