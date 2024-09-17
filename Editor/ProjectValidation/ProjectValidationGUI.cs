using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;


namespace Cognitive3D
{
    internal class ProjectValidationGUI
    {
        private static List<FoldableList> foldableLists = new List<FoldableList>();
        private static bool isInitialized;

        internal static void Init()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            GenerateItemLevelList(EditorCore.Error, ProjectValidation.ItemLevel.Required);
            GenerateItemLevelList(EditorCore.Alert, ProjectValidation.ItemLevel.Recommended);
            GenerateCompletedItemList();
            GenerateIgnoredItemList();

            isInitialized = true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                ProjectValidation.SetIgnoredItemsFromLog();
                ProjectValidation.RegenerateItems();
                Reset();
            }
        }

        internal static void Reset()
        {
            foldableLists.Clear();
            isInitialized = false;
        }

        internal void OnGUI()
        {
            if (!isInitialized)
            {
                Init();
            }

            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space();

                GUILayout.BeginHorizontal();

                GUILayout.Label("Project Validation", EditorCore.styles.IssuesTitleBoldLabel);

                float iconSize = EditorGUIUtility.singleLineHeight;
                if (GUILayout.Button(EditorCore.InfoGrey, EditorCore.styles.InfoButton, GUILayout.Width(iconSize),GUILayout.Height(iconSize)))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/project-validation/");
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.Label("The project validation simplifies Cognitive3D setup by providing a checklist of essential tasks and recommended best practices.", EditorCore.styles.SubtitleHelpText);

                EditorGUILayout.Space(20);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Checklist for " + ProjectValidationItemsStatus.GetCurrentSceneName(), EditorCore.styles.IssuesTitleBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh Checklists"), EditorCore.styles.IconButton))
                {
                    ProjectValidation.RegenerateItems();
                    Reset();
                }
                if (GUILayout.Button(new GUIContent(EditorCore.SettingsIcon2, "Additional Actions"), EditorCore.styles.IconButton))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Verify all build scenes"), false, ProjectValidationItemsStatus.StartSceneVerificationProcess);
                    menu.AddItem(new GUIContent("Show project verification prompt before build"), ProjectValidationLog.GetBuildProcessPopup(), () =>
                    {
                        var showPopup = ProjectValidationLog.GetBuildProcessPopup();
                        ProjectValidationLog.SetBuildProcessPopup(!showPopup);
                    });
                    menu.AddItem(new GUIContent("Reset Ignored Items"), false, () =>
                    {
                        ProjectValidationLog.ClearIgnoreItems();
                        ProjectValidation.ResetIgnoredItems();
                        ProjectValidation.RegenerateItems();
                        Reset();
                    });

                    menu.ShowAsContext();
                }
                GUILayout.EndHorizontal();
            }

            DrawItemLists();

            GUILayout.FlexibleSpace();
        }

        private void DrawItemLists()
        {
            var disableTasksList = EditorApplication.isPlaying;

            using (new EditorGUI.DisabledGroupScope(disableTasksList))
            {
                foreach (var list in foldableLists)
                {
                    using (var scope = new EditorGUILayout.VerticalScope(EditorCore.styles.List))
                    {
                        list.showList = EditorGUILayout.Foldout(list.showList, list.listName, EditorCore.styles.foldoutStyle);

                        // Display the list if showList is true
                        if (list.showList)
                        {
                            GUILayout.BeginVertical();

                            // Iterate through each item in the list
                            foreach (var item in list.items)
                            {
                                string buttonText = item.actionType.ToString();

                                if (list.listName == "Ignored" && item.isIgnored)
                                {
                                    DrawItem(item, null, item.message, true, buttonText);
                                } 
                                else if (list.listName == "Completed" && item.isFixed)
                                {
                                    DrawItem(item, list.listItemIcon, item.fixmessage, false, "");
                                }
                                else if (list.listName != "Completed" && !item.isFixed && !item.isIgnored)
                                {
                                    DrawItem(item, list.listItemIcon, item.message, true, buttonText);
                                }
                            }

                            GUILayout.EndVertical();
                        }
                    }

                    EditorGUILayout.Space();
                }
            }
        }

        private void DrawItem(ProjectValidationItem item, Texture2D itemIcon, string message, bool buttonEnabled, string buttonText)
        {
            using (var scope = new EditorGUILayout.HorizontalScope(EditorCore.styles.ListLabel))
            {
                if (itemIcon != null)
                {
                    GUILayout.Label(itemIcon, EditorCore.styles.InlinedIconStyle);
                }
                GUILayout.Label(message, EditorCore.styles.ItemDescription);

                if (item.fixAction != null && buttonEnabled)
                {
                    if (!item.isIgnored)
                    {
                        if (buttonText != "None")
                        {
                            if (GUILayout.Button(buttonText, EditorCore.styles.MediumButton))
                            {
                                ProjectValidation.FixItem(item);
                                GenerateCompletedItemList();
                            }
                        }

                        if (GUILayout.Button("Ignore", EditorCore.styles.MediumButton))
                        {
                            ProjectValidation.IgnoreItem(item, true);
                            GenerateIgnoredItemList();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Revert", EditorCore.styles.MediumButton))
                        {
                            ProjectValidation.IgnoreItem(item, false);
                            GenerateIgnoredItemList();
                        }
                    }
                }
            }
        }

        private static void GenerateItemLevelList(Texture2D icon, ProjectValidation.ItemLevel level)
        {
            var items = ProjectValidation.GetItems(level).ToList();
            AddToFodableList(level.ToString(), icon, items);
        }

        private static void GenerateCompletedItemList()
        {
            var foldableList = foldableLists.FirstOrDefault(list => list.listName == "Completed");
            var items = ProjectValidation.GetFixedItems().ToList();

            if (foldableList == null)
            {
                AddToFodableList("Completed", EditorCore.CircleCheckmark, items);
            }
            else
            {
                foldableList.items = items;
            }
        }

        private static void GenerateIgnoredItemList()
        {
            var foldableList = foldableLists.FirstOrDefault(list => list.listName == "Ignored");
            var items = ProjectValidation.GetIgnoredItems(true).ToList();

            if (foldableList == null)
            {
                // Item icon should be used!
                AddToFodableList("Ignored", EditorCore.CircleCheckmark, items);
            }
            else
            {
                foldableList.items = items;
            }
        }

        private static void AddToFodableList(string title, Texture2D icon, List<ProjectValidationItem> items)
        {
            FoldableList newLevelItemList = new FoldableList(title, icon, items);
            foldableLists.Add(newLevelItemList);
        }
    }

    [Serializable]
    internal class FoldableList
    {
        public string listName;
        public bool showList = true;
        public Texture2D listItemIcon;
        public List<ProjectValidationItem> items;

        public FoldableList(string name, Texture2D icon, List<ProjectValidationItem> items)
        {
            this.listName = name;
            this.listItemIcon = icon;
            this.items = items;
        }
    }
}
