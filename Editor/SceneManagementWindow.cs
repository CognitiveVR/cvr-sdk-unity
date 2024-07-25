using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Cognitive3D
{
    internal class SceneManagementWindow : EditorWindow
    {
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

        static List<SceneEntry> entries = new List<SceneEntry>();
        internal static void Init()
        {
            SceneManagementWindow window = (SceneManagementWindow)EditorWindow.GetWindow(typeof(SceneManagementWindow), true, "Scene Management (Version " + Cognitive3D_Manager.SDK_VERSION + ")");
            window.minSize = new Vector2(600, 550);
            window.maxSize = new Vector2(600, 550);
            window.Show();
            string[] guid = AssetDatabase.FindAssets("t:scene");
            List<string> paths = new List<string>();
            foreach (var id in guid)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(id));
            }
            entries.Clear();
            foreach (var path in paths)
            {
                entries.Add(new SceneEntry(path));
            }
        }


        Vector2 dynamicScrollPosition;
        bool exportDynamics = false;

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

            if (temp != string.Empty)
            {
                FilterList(searchBarString);
            }

            // Scroll area
            Rect innerScrollSize = new Rect(30, 80, 520, 1000);
            dynamicScrollPosition = GUI.BeginScrollView(new Rect(30, 80, 540, 360), dynamicScrollPosition, innerScrollSize, false, true, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar);

            for (int i = 0; i < entries.Count; i++)
            {
                Rect rect = new Rect(31, i * 40, 538, 35);
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

        private void FilterList(string filterQuery)
        {

        }

        private void DrawFooter()
        {
            GUI.color = EditorCore.BlueishGrey;
            GUI.DrawTexture(new Rect(0, 500, 600, 50), EditorGUIUtility.whiteTexture);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(85, 510, 220, 30), new GUIContent("Export and upload all scenes")))
            {

            }

            if (GUI.Button(new Rect(315, 510, 220, 30), new GUIContent("Export and upload selected scenes")))
            {

            }
        }
    }
}

