using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Cognitive3D
{
    internal class SceneManagementWindow : EditorWindow
    {

        #region SCENE_ENTRY_CLASS

        internal class SceneEntry
        {
            internal string path;
            internal bool selected;

            internal SceneEntry(string pathToScene)
            {
                path = pathToScene;
                selected = false;
            }
        }

        #endregion

        static List<SceneEntry> entries = new List<SceneEntry>();
        Vector2 dynamicScrollPosition;
        
        /// <summary>
        /// If true, export dynamics from the scenes too <br/>
        /// Otherwise, just export the scenes
        /// </summary>
        bool exportDynamics = false;

        /// <summary>
        /// If true, export only the selected scenes <br/>
        /// Otherwise, export all
        /// </summary>
        bool selectedOnly;

        /// <summary>
        /// Set to false until user clicks export button <br/>
        /// User's click sets it to true <br/>
        /// Once export complete, this variable is set back to false
        /// </summary>
        bool exportNow = false;

        /// <summary>
        /// Used to track the current scene in the Update FSM
        /// </summary>
        int sceneIndex = 0;

        internal enum SceneManagementUploadState
        {
            /// <summary>
            /// We start here <br/>
            /// If target scene is open, we move to scene setup <br/>
            /// Otherwise, we open scene, and then move to scene setup
            /// </summary>
            Init,
   
            SceneSetup,
            GameObjectSetup,
            Export,
            StartUpload,
            Uploading,
            Complete
        };

        internal SceneManagementUploadState sceneUploadState = SceneManagementUploadState.Init;

        internal static void Init()
        {
            // Only search "Assets/" - don't search Packages/
            string[] foldersToSearch = { "Assets/" };
            SceneManagementWindow window = (SceneManagementWindow)EditorWindow.GetWindow(typeof(SceneManagementWindow), true, "Scene Management (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(600, 550);
            window.maxSize = new Vector2(600, 550);
            window.Show();
            string[] guid = AssetDatabase.FindAssets("t:scene", foldersToSearch);
            entries.Clear();
            foreach (var id in guid)
            {
                entries.Add(new SceneEntry(AssetDatabase.GUIDToAssetPath(id)));
            }
        }

        private void OnGUI()
        {
            // Basic GUI skin
            GUI.skin = EditorCore.WizardGUISkin;
            GUI.DrawTexture(new Rect(0, 0, 600, 550), EditorGUIUtility.whiteTexture);
            
            // Title
            Rect steptitlerect = new Rect(0, 0, 600, 30);
            GUI.Label(steptitlerect, "SCENES FOUND IN PROJECT: " + entries.Count, "image_centered");

            // Search bar
            string searchBarString  = string.Empty;
            Rect searchBarRect = new Rect(100, 40, 400, 20);
            string temp = GUI.TextField(searchBarRect, searchBarString, 64);

            // If search string exists, filter
            if (temp != string.Empty)
            {
                // FilterList(temp);
            }

            // Scroll area
            Rect innerScrollSize = new Rect(30, 80, 520, 1000);
            dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 80, 540, 360), dynamicScrollPosition, innerScrollSize, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);

            for (int i = 0; i < entries.Count; i++)
            {
                Rect rect = new Rect(31, i * 40 + 80, 538, 35);
                DrawSceneEntry(entries[i], rect, i % 2 == 0);
            }

            GUI.EndScrollView();
            GUI.Box(new Rect(30, 80, 540, 360), "", "box_sharp_alpha");

            // Checkbox for exporting dynamics
            var dynamicsExportCheckbox = exportDynamics ? EditorCore.BoxCheckmark : EditorCore.BoxEmpty;
            if (GUI.Button(new Rect(205, 450, 30, 30), dynamicsExportCheckbox, EditorCore.WizardGUISkin.GetStyle("image_centered")))
            {
                exportDynamics = !exportDynamics;
            }
            GUI.Label(new Rect(245, 450, 250, 30), "Export dynamics with scene", "dynamiclabel");

            DrawFooter();
            Repaint();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="rect"></param>
        /// <param name="dark"></param>
        private void DrawSceneEntry(SceneEntry scene, Rect rect, bool dark)
        {
            var toggleIcon = scene.selected ? EditorCore.BoxCheckmark : EditorCore.BoxEmpty;

            // Alternate dark/light background
            if (dark)
            {
                GUI.Box(rect, "", EditorCore.WizardGUISkin.GetStyle("dynamicentry_even"));
            }
            else
            {
                GUI.Box(rect, "", EditorCore.WizardGUISkin.GetStyle("dynamicentry_odd"));
            }

            if (GUI.Button(new Rect(rect.x, rect.y + 2, 30, 30), toggleIcon, EditorCore.WizardGUISkin.GetStyle("image_centered")))
            {
                scene.selected = !scene.selected;
            }

            GUI.Label(new Rect(rect.x + 32, rect.y, rect.width - 32, rect.height), scene.path, EditorCore.WizardGUISkin.GetStyle("dynamiclabel"));
            Repaint();
        }

        /// <summary>
        /// Filter list of shown scenes based on query
        /// </summary>
        /// <param name="filterQuery">A string of the parameters to filter on</param>
        private void FilterList(string filterQuery)
        {

        }

        /// <summary>
        /// Draws the footer box and buttons
        /// </summary>
        private void DrawFooter()
        {
            // Bottom border box
            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 600, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            ////////////////////////
            /// EXPORT ALL        //
            ////////////////////////
            if (GUI.Button(new Rect(85, 510, 220, 30), new GUIContent("Export and upload all scenes")))
            {
                selectedOnly = false;
                exportNow = true;
                sceneUploadState = SceneManagementUploadState.Init;
            }

            ////////////////////////
            /// EXPORT SELECTED  //
            ///////////////////////
            if (GUI.Button(new Rect(315, 510, 220, 30), new GUIContent("Export and upload selected scenes")))
            {
                selectedOnly = true;
                exportNow = true;
                sceneUploadState = SceneManagementUploadState.Init;
            }
        }

        internal bool IsSceneOpen(string scenePath)
        {
            return EditorSceneManager.GetActiveScene().path == scenePath;
        }


        /// <summary>
        /// This function defines a state machine
        /// We are unable to use coroutines here, so we rely on Update
        /// We need to wait a frame for certain actions to complete
        /// This will only be triggered when exportNow is set to true from the export button click
        /// </summary>
        private void Update()
        {
            // Exit condition: Stop once all scenes done
            if (sceneIndex > entries.Count - 1)
            {
                exportNow = false;
                return;
            }

            if (exportNow)
            {
                // Skip scene if we want to export selected only and this scene isn't selected
                if (selectedOnly && !entries[sceneIndex].selected)
                {
                    sceneIndex++;
                    sceneUploadState = SceneManagementUploadState.Init;
                    return;
                }

                switch (sceneUploadState)
                {
                    // If required scene isn't open, open it
                    case SceneManagementUploadState.Init:
                        if (EditorSceneManager.GetActiveScene().path == entries[sceneIndex].path)
                        {
                            sceneUploadState = SceneManagementUploadState.SceneSetup;
                        }
                        else
                        {
                            SceneEntry sceneEntry = entries[sceneIndex];
                            EditorSceneManager.OpenScene(sceneEntry.path);
                            sceneUploadState = SceneManagementUploadState.SceneSetup;
                        }
                        return;

                    // Instantiate and setup C3D_Manager if it doesn't exist
                    case SceneManagementUploadState.SceneSetup:
                        if (!FindObjectOfType<Cognitive3D_Manager>())
                        {
                            SceneSetupWindow.PerformBasicSetup();
                            EditorSceneManager.SaveOpenScenes();
                        }
                        sceneUploadState = SceneManagementUploadState.GameObjectSetup;
                        return;

                    // Assign dynamics to controllers
                    case SceneManagementUploadState.GameObjectSetup:
                        EditorSceneManager.SaveOpenScenes();
                        SceneSetupWindow.SetupControllers();
                        sceneUploadState = SceneManagementUploadState.Export;
                        return;

                    // Export the scene
                    case SceneManagementUploadState.Export:

                        string currentScenePath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                        var currentSettings = Cognitive3D_Preferences.FindSceneByPath(currentScenePath);

                        // If not in preferences, add it
                        if (currentSettings == null)
                        {
                            Cognitive3D_Preferences.AddSceneSettings(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                        }

                        ExportUtility.ExportGLTFScene();

                        string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                        string path = EditorCore.GetSubDirectoryPath(fullName);
                        ExportUtility.GenerateSettingsFile(path, fullName);
                        DebugInformationWindow.WriteDebugToFile(path + "debug.log");
                        EditorUtility.SetDirty(EditorCore.GetPreferences());
                        UnityEditor.AssetDatabase.SaveAssets();

                        sceneUploadState = SceneManagementUploadState.StartUpload;
                        return;

                    // 
                    case SceneManagementUploadState.StartUpload:
                        SceneSetupWindow.completedUpload = false;
                        SceneSetupWindow.UploadSceneAndDynamics(exportDynamics, exportDynamics, true, true, false);
                        sceneUploadState = SceneManagementUploadState.Uploading;
                        return;

                    case SceneManagementUploadState.Uploading:
                        if (SceneSetupWindow.completedUpload)
                        {
                            sceneUploadState = SceneManagementUploadState.Complete;
                        }
                        return;

                    // All done, clean up and move to next scene
                    case SceneManagementUploadState.Complete:
                        EditorSceneManager.SaveOpenScenes();
                        SceneSetupWindow.completedUpload = false;
                        sceneUploadState = SceneManagementUploadState.Init;
                        sceneIndex++;
                        return;
                }
            }
        }
    }
}

