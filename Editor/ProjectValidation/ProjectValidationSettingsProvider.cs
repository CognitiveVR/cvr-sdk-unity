using UnityEditor;


namespace Cognitive3D
{
    internal class ProjectValidationSettingsProvider : SettingsProvider
    {
        private const string title = "Cognitive3D";
        public static readonly string SettingsPath = $"Project/{title}";
        private ProjectValidationGUI projectValidationGUI = new ProjectValidationGUI();

        private ProjectValidationSettingsProvider(string path, SettingsScope scopes) : base(path, scopes)
        {
        }

        [MenuItem("Cognitive3D/Project Validation", false, 1)]
        static void OpenProjectSetupTool()
        {
            OpenSettingsWindow();
        }

        public static void OpenSettingsWindow()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            EditorUserBuildSettings.selectedBuildTargetGroup = buildTargetGroup;
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        [SettingsProvider]
        public static SettingsProvider CreateProjectValidationSettingsProvider()
        {
            return new ProjectValidationSettingsProvider(SettingsPath, SettingsScope.Project);
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            projectValidationGUI.OnGUI();
        }
    }
}
