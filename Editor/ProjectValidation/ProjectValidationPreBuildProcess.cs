using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationPreBuildProcess : IPreprocessBuildWithReport
    {
        private const string LOG_TAG = "[COGNITIVE3D] ";
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            bool result = EditorUtility.DisplayDialog(LOG_TAG + "Build Paused", "Cognitive3D project validation has identified unresolved issues that may result in inaccurate data recording", "Fix", "Ignore");
            if (result)
            {
                ProjectValidationSettingsProvider.OpenSettingsWindow();
                throw new BuildFailedException(LOG_TAG + "Build process stopped");
            }
            else
            {
                return;
            }
        }
    }
}