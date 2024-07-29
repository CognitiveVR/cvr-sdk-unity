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

        /// <summary>
        /// Set to false until scene opened in FSM <br/>
        /// It is set back to false before the next iteration<br/>
        /// </summary>
        bool sceneOpened = false;

        /// <summary>
        /// Set to true once the controllers have dynamics attached to them <br/>
        /// It is set back to false before the next iteration
        /// </summary>
        bool gameObjectsSetupComplete = false;

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
                exportNow = true;

            }

            ////////////////////////
            /// EXPORT SELECTED  //
            ///////////////////////
            if (GUI.Button(new Rect(315, 510, 220, 30), new GUIContent("Export and upload selected scenes")))
            {

            }
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

            // Step 1: If export enabled, start
            if (exportNow)
            {
                // If scene not opened, open it and move to next frame
                if (!sceneOpened)
                {
                    SceneEntry sceneEntry = entries[sceneIndex];
                    EditorSceneManager.OpenScene(sceneEntry.path);
                    sceneOpened = true;
                    return;
                }

                // Step 2: If scene is open, set it up
                if (sceneOpened)
                {
                    // Instantiate and setup C3D_Manager if doesn't exist
                    if (!FindObjectOfType<Cognitive3D_Manager>())
                    {
                        SceneSetupWindow.PerformBasicSetup();
                        gameObjectsSetupComplete = false;
                        EditorSceneManager.SaveOpenScenes();
                        return;
                    }
                }

                // Step 3: In the next frame, assign dynamic objects to the controllers
                if (!gameObjectsSetupComplete)
                {
                    EditorSceneManager.SaveOpenScenes();
                    SceneSetupWindow.SetupControllers();
                    gameObjectsSetupComplete = true;
                    return;
                }    

                // Step 4: If export dynamics enabled, export dynamics from this scene
                if (exportDynamics)
                {
                    SceneSetupWindow.ExportAllDynamicsInScene();
                }

                // Step 5: Save, reset variables, exit
                EditorSceneManager.SaveOpenScenes();
                sceneOpened = false;
                sceneIndex++;
                return;
            }
        }
    }
}

