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
        public List<FoldableList> foldableLists = new List<FoldableList>();

        internal void OnGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                EditorGUILayout.Space();

                GUILayout.Label("Project Validation", styles.IssuesTitleLabel);
                GUILayout.Label("The project validation simplifies Cognitive3D setup by providing a checklist of essential tasks and recommended best practices.", styles.SubtitleHelpText);

                EditorGUILayout.Space();

                GUILayout.Label("Checklist", styles.IssuesTitleLabel, GUILayout.Width(Styles.TitleLabelWidth));
            }

            // TODO: need to fix this! Generate items should not get called every frame!
            if (foldableLists.Count == 0)
            {
                GenerateItemLevelList(ProjectValidation.ItemLevel.Required);
                GenerateItemLevelList(ProjectValidation.ItemLevel.Recommended);
                GenerateCompletedItemList();
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
                            if (!item.isFixed)
                            {
                                DrawItem(item, item.message, true);
                            }
                            else if (list.listName == "Completed")
                            {
                                DrawItem(item, item.fixmessage, false);
                            }
                        }

                        GUILayout.EndVertical();
                    }
                }

                EditorGUILayout.Space();
            }
        }

        private void DrawItem(ProjectValidationItem item, string message, bool buttonEnabled)
        {
            using (var scope = new EditorGUILayout.HorizontalScope(styles.ListLabel))
            {
                GUILayout.Label(message);

                if (item.fixAction != null)
                {
                    if (buttonEnabled && GUILayout.Button("Fix", styles.FixButton))
                    {
                        ProjectValidation.FixItem(item);
                        GenerateCompletedItemList();
                    }
                }
            }
        }

        private void GenerateItemLevelList(ProjectValidation.ItemLevel level)
        {
            var items = ProjectValidation.GetItems(level).ToList();
            AddToFodableList(level.ToString(), items);
        }

        private void GenerateCompletedItemList()
        {
            var foldableList = foldableLists.FirstOrDefault(list => list.listName == "Completed");
            var items = ProjectValidation.GetFixedItems().ToList();

            if (foldableList == null)
            {
                AddToFodableList("Completed", items);
            }
            else
            {
                foldableList.items = items;
            }
        }

        private void AddToFodableList(string title, List<ProjectValidationItem> items)
        {
            FoldableList newLevelItemList = new FoldableList(title, items);
            foldableLists.Add(newLevelItemList);
        }
    }

    [Serializable]
    internal class FoldableList
    {
        public string listName;
        public bool showList = true;
        public List<ProjectValidationItem> items;

        public FoldableList(string name, List<ProjectValidationItem> items)
        {
            this.listName = name;
            this.items = items;
        }
    }
}
