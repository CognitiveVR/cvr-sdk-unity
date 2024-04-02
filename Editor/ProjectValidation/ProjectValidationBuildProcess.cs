using UnityEngine;
using System.IO;
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

        private string currentAppVer;
        private string cachedAppVer;

        // Prompts a project validation popup if the project is not verified and the app version has changed
        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectValidationItems.UpdateProjectValidationItemStatus();
            CheckCachedAppVersion();
            currentAppVer = Application.version;

            if (currentAppVer != cachedAppVer && !ProjectValidationItemsStatus.isProjectVerified)
            {
                ProjectValidationItemsStatus.VerifyAllBuildScenes();
            }

            if (ProjectValidationItemsStatus.throwExecption)
            {
                throw new BuildFailedException(LOG_TAG + "Build process stopped");
            }
        }

        /// <summary>
        /// Checks and retrieves the app version stored in project validation JSON file
        /// </summary>
        internal void CheckCachedAppVersion()
        {
            cachedAppVer = "";
            string filePath = EditorCore.GetBaseDirectoryPath() + "/projectvalidation.json";

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                ProjectValidationJSON pvj = JsonUtility.FromJson<ProjectValidationJSON>(json);

                cachedAppVer = pvj.applicationVersion;
            }
        }
    }

    [InitializeOnLoad]
    internal class ProjectValidationPostBuildProcess : IPostprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        // Updates app version and resets project verification
        public void OnPostprocessBuild(BuildReport report)
        {
            ProjectValidationLog.UpdateLog();
            ProjectValidationItemsStatus.isProjectVerified = false;
        }
    }
}