using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


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

            internal readonly GUIStyle Wrap = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(0, 5, 1, 1)
            };

            internal readonly GUIStyle IssuesBackground = new GUIStyle("ScrollViewAlt")
            {
            };

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

            internal readonly GUIStyle GenerateReportButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
            };

            internal readonly GUIStyle FixButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = FixButtonWidth,
            };

            internal readonly GUIStyle FixAllButton = new GUIStyle(EditorStyles.miniButton)
            {
                margin = new RectOffset(0, 10, 2, 2),
                stretchWidth = false,
                fixedWidth = FixAllButtonWidth,
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

            internal readonly GUIStyle SubtitleHelpText = new GUIStyle(EditorStyles.miniLabel)
            {
                margin = new RectOffset(10, 0, 0, 0),
                wordWrap = true
            };

            internal readonly GUIStyle InternalHelpBox = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(5, 5, 5, 5)
            };

            internal readonly GUIStyle InternalHelpText = new GUIStyle(EditorStyles.miniLabel)
            {
                margin = new RectOffset(10, 0, 0, 0),
                wordWrap = true,
                fontStyle = FontStyle.Italic,
                normal =
                {
                    textColor = new Color(0.58f, 0.72f, 0.95f)
                }
            };

            internal readonly GUIStyle NormalStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(10, 0, 0, 0),
                wordWrap = true,
                stretchWidth = false
            };

            internal readonly GUIStyle BoldStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                stretchWidth = false,
                wordWrap = true,
                fontStyle = FontStyle.Bold
            };

            internal readonly GUIStyle MiniButton = new GUIStyle(EditorStyles.miniButton)
            {
                clipping = TextClipping.Overflow,
                fixedHeight = 18.0f,
                fixedWidth = 18.0f,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(2, 2, 2, 2)
            };

            internal readonly GUIStyle Foldout = new GUIStyle(EditorStyles.foldoutHeader)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(16, 5, 5, 5),
                fixedHeight = 26.0f
            };

            internal readonly GUIStyle FoldoutHorizontal = new GUIStyle(EditorStyles.label)
            {
                fixedHeight = 26.0f
            };

            internal readonly GUIStyle List = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(3, 3, 3, 3),
                padding = new RectOffset(3, 3, 3, 3)
            };
        }

        private static Styles styles = new Styles();

        internal void OnGUI()
        {
            EditorGUILayout.Space();

            GUILayout.Label("Project Validation", styles.IssuesTitleLabel);
            GUILayout.Label("The project validation simplifies Cognitive3D setup by providing a checklist of essential tasks and recommended best practices.", styles.SubtitleHelpText);

            EditorGUILayout.Space();

            GUILayout.Label("Checklist", styles.IssuesTitleLabel, GUILayout.Width(Styles.TitleLabelWidth));

            GenerateItemLevelList(ProjectValidation.ItemLevel.Required, "Required");
            GUILayout.FlexibleSpace();
        }

        private void GenerateItemLevelList(ProjectValidation.ItemLevel level, string title)
        {
            var items = new List<ProjectValidationItem>(ProjectValidation.GetItems(level));

            // Debug.Log("@@@ number of items in " + level.ToString() + " level is " + items.Count);

            // GUILayout.Label("This is just to see if it's working!", styles.IssuesTitleLabel);

            using (var scope = new EditorGUILayout.VerticalScope(styles.List))
            {
                var rect = scope.rect;

                // Foldout
                title = $"{title} ({items.Count})";

                var foldout = FoldoutWithAdditionalAction(title, rect, () =>
                {

                });
            }
        }

        private bool FoldoutWithAdditionalAction(string label, Rect rect, Action inlineAdditionalAction)
        {
            var previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = rect.width - 8;

            bool foldout;
            using (new EditorGUILayout.HorizontalScope(styles.FoldoutHorizontal))
            {
                // foldout = Foldout(key, label);
                foldout = true;
                inlineAdditionalAction?.Invoke();
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
            return foldout;
        }
    }
}
