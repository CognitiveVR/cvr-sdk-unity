using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Cognitive3D
{
    [InitializeOnLoad]
    public static class ProjectValidationToolbar
    {
        static Button validateButton;
        static Image icon;
        static bool buttonAdded;

        static ProjectValidationToolbar()
        {
            EditorApplication.update += DelayedInit;
            ProjectValidation.OnProjectValidationUpdate += OnProjectValidationUpdate;
            EditorApplication.quitting += Cleanup;
        }

        private static void Cleanup()
        {
            ProjectValidation.OnProjectValidationUpdate -= OnProjectValidationUpdate;
            EditorApplication.quitting -= Cleanup;
        }

        private static void DelayedInit()
        {
            EditorApplication.update -= DelayedInit;
            TryCreateToolbarButton();
            OnProjectValidationUpdate(); // Set initial icon/text
        }

        private static void TryCreateToolbarButton()
        {
            if (buttonAdded) return;

            var toolbarType = typeof(Editor).Assembly.GetType("UnityEditor.Toolbar");
            if (toolbarType == null) return;

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars.Length == 0) return;

            var toolbar = toolbars[0];
            var mRootField = toolbarType.GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            if (mRootField == null) return;

            var mRoot = mRootField.GetValue(toolbar) as VisualElement;
            if (mRoot == null) return;

            var rightZone = mRoot.Q("ToolbarZoneLeftAlign");
            if (rightZone == null) return;

            validateButton = new Button(() =>
            {
                ProjectValidationSettingsProvider.OpenProjectSetupTool();
            })
            {
                text = string.Empty
            };

            validateButton.styleSheets.Clear();
            validateButton.AddToClassList("unity-toolbar-button");

            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            icon = new Image
            {
                image = EditorCore.LogoDone,
                scaleMode = ScaleMode.ScaleToFit,
                style =
                {
                    width = 16,
                    height = 16,
                    marginRight = 4
                }
            };

            var label = new Label("Cognitive3D Validation");

            container.Add(icon);
            container.Add(label);
            validateButton.Add(container);

            rightZone.Add(validateButton);
            buttonAdded = true;
        }

        static void OnProjectValidationUpdate()
        {
            // Ensure button exists
            if (!buttonAdded) TryCreateToolbarButton();
            if (icon == null || validateButton == null) return;

            // Disable the button during Play Mode
            validateButton.SetEnabled(!EditorApplication.isPlaying);
            
            var levels = ProjectValidation.GetLevelsOfItemsNotFixed()?.ToList();
            Texture2D newIcon;

            if (levels == null || levels.Count == 0)
            {
                newIcon = EditorCore.LogoDone;
            }
            else if (levels.Contains(ProjectValidation.ItemLevel.Required))
            {
                newIcon = EditorCore.LogoError;
            }
            else if (levels.Contains(ProjectValidation.ItemLevel.Recommended))
            {
                newIcon = EditorCore.LogoWarning;
            }
            else
            {
                newIcon = EditorCore.LogoDone;
            }

            // Prevent reassigning the same image
            if (icon.image != newIcon)
            {
                icon.image = newIcon;
            }
        }
    }
}
