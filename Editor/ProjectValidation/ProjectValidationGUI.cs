using System;
using System.Collections;
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
            private const float FixButtonWidth = 64.0f;
            private const float FixAllButtonWidth = 80.0f;
            internal const float GroupSelectionWidth = 244.0f;
            internal const float LabelWidth = 96f;
            internal const float TitleLabelWidth = 196f;

            internal readonly GUIStyle ListLabel = new GUIStyle("TV Selection")
            {
                border = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(5, 5, 5, 3),
                margin = new RectOffset(4, 4, 4, 5)
            };

            internal readonly GUIStyle IssuesTitleLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                wordWrap = false,
                stretchWidth = false,
                fontStyle = FontStyle.Bold,
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

            internal readonly GUIStyle FixButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = FixButtonWidth,
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

        private static Styles styles = new Styles();
        public static List<FoldableList> foldableLists = new List<FoldableList>();
        public static bool isInitialized;

        internal static void Init()
        {
            GenerateItemLevelList(EditorCore.Error, ProjectValidation.ItemLevel.Required);
            GenerateItemLevelList(EditorCore.Alert, ProjectValidation.ItemLevel.Recommended);
            GenerateCompletedItemList();

            isInitialized = true;
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

                GUILayout.Label("Project Validation", styles.IssuesTitleLabel);
                GUILayout.Label("The project validation simplifies Cognitive3D setup by providing a checklist of essential tasks and recommended best practices.", styles.SubtitleHelpText);

                EditorGUILayout.Space();

                GUILayout.Label("Checklist", styles.IssuesTitleLabel, GUILayout.Width(Styles.TitleLabelWidth));
            }

            DrawItemLists();

            GUILayout.FlexibleSpace();
        }

        private void DrawItemLists()
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
                            string buttonText = list.listName == "Required" ? "Fix" : "Apply";
                            if (!item.isFixed)
                            {
                                DrawItem(item, list.listItemIcon, item.message, true, buttonText);
                            }
                            
                            if (list.listName == "Completed")
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

        private void DrawItem(ProjectValidationItem item, Texture2D itemIcon, string message, bool buttonEnabled, string buttonText)
        {
            using (var scope = new EditorGUILayout.HorizontalScope(styles.ListLabel))
            {
                GUILayout.Label(itemIcon, styles.InlinedIconStyle);
                GUILayout.Label(message);

                if (item.fixAction != null)
                {
                    if (buttonEnabled && GUILayout.Button(buttonText, styles.FixButton))
                    {
                        ProjectValidation.FixItem(item);
                        GenerateCompletedItemList();
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
