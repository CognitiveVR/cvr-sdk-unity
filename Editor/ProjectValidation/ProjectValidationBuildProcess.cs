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

        // Prompt project validation popup if app version changed
        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectValidationItems.UpdateProjectValidationItemStatus();
            CheckCachedAppVersion();
            currentAppVer = Application.version;

            if (currentAppVer != cachedAppVer)
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

        // Update app version
        public void OnPostprocessBuild(BuildReport report)
        {
            ProjectValidationLog.UpdateLog();
        }
    }
}