using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;

namespace Cognitive3D
{
    [InitializeOnLoad]
    internal class ProjectValidationPreBuildProcess : IPreprocessBuildWithReport
    {
        private const string LOG_TAG = "[COGNITIVE3D] ";
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectValidationItems.UpdateProjectValidationItemStatus();

            ProjectValidationItemsStatus.VerifyAllBuildScenes();

            if (ProjectValidationItemsStatus.throwExecption)
            {
                throw new BuildFailedException(LOG_TAG + "Build process stopped");
            }
        }
    }
}