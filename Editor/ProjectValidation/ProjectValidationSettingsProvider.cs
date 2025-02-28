using UnityEngine;
using UnityEditor;
using Cognitive3D.Components;
using UnityEngine.UIElements;

namespace Cognitive3D
{
    internal class ProjectValidationSettingsProvider : SettingsProvider
    {
        private const string title = "Cognitive3D";
        public static readonly string SettingsPath = $"Project/{title}";
        private readonly ProjectValidationGUI projectValidationGUI = new ProjectValidationGUI();

        internal ProjectValidationSettingsProvider(string path, SettingsScope scopes) : base(path, scopes)
        {
        }

        public static void OpenProjectSetupTool()
        {
            SegmentAnalytics.TrackEvent("OpenProjectValidation", "MenuItems_ProjectValidation");
            OpenSettingsWindow();
        }

        public static void OpenSettingsWindow()
        {
            SegmentAnalytics.PageEvent("ProjectValidationWindow", "Opened");
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectValidationSettingsProvider()
        {
            return new ProjectValidationSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            ProjectValidation.RegenerateItems();
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            projectValidationGUI.OnGUI();
        }
    }
}
