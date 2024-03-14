using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationPreBuildProcess : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            bool result = EditorUtility.DisplayDialog("Build Paused", "Outstanding Cognitive3D items are not fixed!", "Ignore", "Fix");
            if (result)
            {
                return;
            }
            else
            {
                ProjectValidationSettingsProvider.OpenSettingsWindow();
                throw new BuildFailedException("Build process stopped");
            }
        }
    }
}