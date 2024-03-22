using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationItemsStatus
    {
        internal static Dictionary<string, bool> sceneVaidationStatusDic = new Dictionary<string, bool>();

        static ProjectValidationItemsStatus()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneSaved += OnSceneSaved;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        private static void OnSceneClosed(Scene scene)
        {
            Debug.Log("@@@ Scene is closed!");
            AddOrUpdateSceneValidationStatus(scene);
        }

        private static void OnSceneSaved(Scene scene)
        {
            Debug.Log("@@@ Scene is saved!");
            AddOrUpdateSceneValidationStatus(scene);
        }

        // Update project validation items when a new scene opens
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            Debug.Log("@@@ Scene is opened!");
            ProjectValidation.Reset();
            ProjectValidationItems.WaitBeforeProjectValidation();
        }

        static void AddOrUpdateSceneValidationStatus(Scene scene)
        {
            if (!sceneVaidationStatusDic.ContainsKey(scene.name))
            {
                sceneVaidationStatusDic.Add(scene.name, ProjectValidation.hasNotFixedItems());
            }
            else
            {
                sceneVaidationStatusDic[scene.name] = ProjectValidation.hasNotFixedItems();
            }

            foreach (var status in sceneVaidationStatusDic)
            {
                Debug.Log("@@ Scene is " + status.Key + " and are not fixed items " + status.Value);
            }
        }
    }
}
