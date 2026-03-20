using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class ExitpollDetailGUI : IFeatureDetailGUI
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label("ExitPoll Survey", EditorCore.styles.FeatureTitle);

                float iconSize = EditorGUIUtility.singleLineHeight;
                Rect iconRect = GUILayoutUtility.GetRect(iconSize, iconSize, GUILayout.Width(iconSize), GUILayout.Height(iconSize));

                GUIContent buttonContent = new GUIContent(EditorCore.ExternalIcon, "Open ExitPoll Survey documentation");
                if (GUI.Button(iconRect, buttonContent, EditorCore.styles.InfoButton))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/exitpoll/");
                }

                GUILayout.FlexibleSpace(); // Push content to the left
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(
                "An ExitPoll survey is a feature to gather feedback from your users and aggregate results on the dashboard.",
                EditorStyles.wordWrappedLabel
            );

            EditorGUILayout.Space(10);

            GUILayout.Label("1. Create Hook", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Set up an ExitPoll hook in the Dashboard to trigger surveys.", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Open Dashboard Hook Manager", GUILayout.Height(30)) && FeatureLibrary.projectID > 0)
            {
                Application.OpenURL(CognitiveStatics.GetExitPollSettingsUrl(FeatureLibrary.projectID) + "/managehooks");
            }

            EditorGUILayout.Space(10);

            GUILayout.Label("2. Create Questions", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Build your question set that will be shown in the survey.", EditorStyles.wordWrappedLabel);

            if (GUILayout.Button("Open Dashboard ExitPoll Manager", GUILayout.Height(30)) && FeatureLibrary.projectID > 0)
            {
                Application.OpenURL(CognitiveStatics.GetExitPollSettingsUrl(FeatureLibrary.projectID));
            }

            EditorGUILayout.Space(10);

            GUILayout.Label("3. Import & Configure", EditorCore.styles.FeatureTitle);
            GUILayout.Label("Import the ExitPoll sample and optionally assign your hook ID.", EditorStyles.wordWrappedLabel);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import ExitPoll Customization Sample", GUILayout.Height(30)) && FeatureLibrary.projectID > 0)
            {
                ImportExitPollSampleWithOptionalPrefab(false);
            }
            if (GUILayout.Button("Add ExitpollHolder prefab", GUILayout.Height(30)) && FeatureLibrary.projectID > 0)
            {
                ImportExitPollSampleWithOptionalPrefab(true);
            }
            GUILayout.EndHorizontal();
        }

        internal static void ImportExitPollSampleWithOptionalPrefab(bool addPrefab)
        {
            var packageName = "com.cognitive3d.c3d-sdk";
            var sampleName = "Exitpoll Customization";

            var samples = UnityEditor.PackageManager.UI.Sample.FindByPackage(packageName, null);

            foreach (var sample in samples)
            {
                if (sample.displayName == sampleName)
                {
                    if (!sample.isImported)
                    {
                        sample.Import();
                        Util.logDebug($"Imported sample: {sample.displayName}");
                    }
                    else
                    {
                        Util.logWarning($"{sample.displayName} sample already imported. Path: {sample.importPath}");
                    }

                    if (addPrefab && GameObject.FindAnyObjectByType<ExitPollHolder>() == null)
                    {
                        string fullPath = sample.importPath + "/ExitPollHolderPrefab.prefab";

                        // Normalize slashes
                        fullPath = fullPath.Replace("\\", "/");

                        // Find where "Assets" starts in the full path
                        int assetsIndex = fullPath.IndexOf("Assets/", System.StringComparison.OrdinalIgnoreCase);
                        if (assetsIndex == -1)
                        {
                            Debug.LogError("Could not find 'Assets/' in path: " + fullPath);
                            return;
                        }

                        string assetRelativePath = fullPath.Substring(assetsIndex);

                        // Load and instantiate
                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetRelativePath);
                        if (prefab == null)
                        {
                            Debug.LogError("Could not find prefab at: " + assetRelativePath);
                            return;
                        }

                        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        Undo.RegisterCreatedObjectUndo(instance, "Add ExitPoll Prefab");
                        Selection.activeObject = instance;
                    }

                    return;
                }
            }

            Debug.LogError("Exitpoll Customization sample not found!");
        }
    }
}
