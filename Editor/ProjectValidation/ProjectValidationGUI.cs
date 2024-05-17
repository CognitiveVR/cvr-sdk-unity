using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;


namespace Cognitive3D
{
    internal class ProjectValidationGUI
    {
        internal class Styles
        {
            private const float SmallIconSize = 16.0f;
            private const float MediumButtonWidth = 64.0f;
            private const float LargeButtonWidth = 90.0f;
            private const float IconButtonWidth = 30.0f;
            internal const float GroupSelectionWidth = 244.0f;
            internal const float LabelWidth = 96f;
            internal const float TitleLabelWidth = 196f;
            private const float IconSize = 16f;

            internal readonly GUIStyle ListLabel = new GUIStyle("TV Selection")
            {
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(5, 5, 5, 3),
                margin = new RectOffset(4, 4, 4, 5)
            };

            internal readonly GUIStyle IssuesTitleBoldLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                wordWrap = false,
                stretchWidth = false,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 2, 0, 0)
            };

            internal readonly GUIStyle IssuesTitleLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                wordWrap = false,
                stretchWidth = false,
                padding = new RectOffset(10, 10, 0, 0)
            };

            internal readonly GUIStyle InlinedIconStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = SmallIconSize,
                fixedHeight = SmallIconSize
            };

            internal readonly GUIStyle IconStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(5, 5, 4, 5),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = SmallIconSize,
                fixedHeight = SmallIconSize
            };

            internal readonly GUIStyle MediumButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = MediumButtonWidth,
            };

            internal readonly GUIStyle LargeButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = LargeButtonWidth,
            };

            internal readonly GUIStyle IconButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 0, 0),
                fixedWidth = IconButtonWidth,
                fixedHeight = 25
            };

            internal readonly GUIStyle InfoButton = new GUIStyle
            {
                padding = new RectOffset(0, 0, 5, 0)
            };

            internal readonly GUIStyle SubtitleHelpText = new GUIStyle(EditorStyles.miniLabel)
            {
                margin = new RectOffset(10, 0, 0, 0),
                wordWrap = true
            };

            internal readonly GUIStyle List = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(10, 10, 10, 10),
                padding = new RectOffset(5, 5, 5, 5),
            };

            internal readonly GUIStyle foldoutStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontStyle = FontStyle.Bold
            };

            internal readonly GUIStyle ItemDescription = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
            };
        }

        // Method to create a texture with a specified color
        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = color;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        internal static Color HexToColor(string hex)
        {
            hex = hex.Replace("#", string.Empty);
            byte r = (byte)(Convert.ToInt32(hex.Substring(0, 2), 16));
            byte g = (byte)(Convert.ToInt32(hex.Substring(2, 2), 16));
            byte b = (byte)(Convert.ToInt32(hex.Substring(4, 2), 16));
            byte a = 255;

            if (hex.Length == 8)
            {
                a = (byte)(Convert.ToInt32(hex.Substring(6, 2), 16));
            }

            return new Color32(r, g, b, a);
        }

        private static Styles _styles;
        // Delays instantiation of the Styles object until it is first accessed
        private static Styles styles
        {
            get
            {
                if (_styles == null)
                    _styles = new Styles();
                return _styles;
            }
        }
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
                ProjectValidationItems.UpdateProjectValidationItemStatus();
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

                GUILayout.Label("Project Validation", styles.IssuesTitleBoldLabel);

                float iconSize = EditorGUIUtility.singleLineHeight;
                if (GUILayout.Button(EditorCore.InfoGrey, styles.InfoButton, GUILayout.Width(iconSize),GUILayout.Height(iconSize)))
                {
                    Application.OpenURL("https://docs.cognitive3d.com/unity/project-validation/");
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUILayout.Label("The project validation simplifies Cognitive3D setup by providing a checklist of essential tasks and recommended best practices.", styles.SubtitleHelpText);

                EditorGUILayout.Space(20);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Checklist for " + ProjectValidationItemsStatus.GetCurrentSceneName(), styles.IssuesTitleBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent(EditorCore.RefreshIcon, "Refresh Checklists"), styles.IconButton))
                {
                    ProjectValidationItems.UpdateProjectValidationItemStatus();
                    Reset();
                }
                if (GUILayout.Button(new GUIContent(EditorCore.SettingsIcon2, "Additional Actions"), styles.IconButton))
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
                        ProjectValidationItems.UpdateProjectValidationItemStatus();
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
                    using (var scope = new EditorGUILayout.VerticalScope(styles.List))
                    {
                        list.showList = EditorGUILayout.Foldout(list.showList, list.listName, styles.foldoutStyle);

                        // Display the list if showList is true
                        if (list.showList)
                        {
                            GUILayout.BeginVertical();

                            // Iterate through each item in the list
                            foreach (var item in list.items)
                            {
                                string buttonText = item.actionType.ToString();
                                if ((list.listName != "Completed" || list.listName != "Ignored") && (!item.isFixed && !item.isIgnored))
                                {
                                    DrawItem(item, list.listItemIcon, item.message, true, buttonText);
                                }

                                if (list.listName == "Ignored" && item.isIgnored)
                                {
                                    DrawItem(item, null, item.message, true, buttonText);
                                }
                                
                                if (list.listName == "Completed" && item.isFixed)
                                {
                                    DrawItem(item, list.listItemIcon, item.fixmessage, false, "");
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
            using (var scope = new EditorGUILayout.HorizontalScope(styles.ListLabel))
            {
                if (itemIcon != null)
                {
                    GUILayout.Label(itemIcon, styles.InlinedIconStyle);
                }
                GUILayout.Label(message, styles.ItemDescription);

                if (item.fixAction != null && buttonEnabled)
                {
                    if (!item.isIgnored)
                    {
                        if (GUILayout.Button(buttonText, styles.MediumButton))
                        {
                            ProjectValidation.FixItem(item);
                            GenerateCompletedItemList();
                        }

                        if (GUILayout.Button("Ignore", styles.MediumButton))
                        {
                            ProjectValidation.IgnoreItem(item, true);
                            GenerateIgnoredItemList();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Don't Ignore", styles.LargeButton))
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
