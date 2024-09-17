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
        
        string cachedAppVer;

        // Prompts a project validation popup if the project is not verified and the app version has changed
        public void OnPreprocessBuild(BuildReport report)
        {
            ProjectValidation.RegenerateItems();
            CheckCachedAppVersion();
            string currentAppVer = Application.version;

            if (currentAppVer != cachedAppVer)
            {
                ProjectValidationItemsStatus.VerifyAllBuildScenes();
            }

            if (ProjectValidationItemsStatus.throwBuildException)
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
            string filePath = ProjectValidationLog.FILEPATH;

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

        // Updates and refreshes project verification items after build
        public void OnPostprocessBuild(BuildReport report)
        {
            ProjectValidation.RegenerateItems();
        }
    }
}