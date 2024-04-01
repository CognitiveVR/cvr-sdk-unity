using UnityEngine;
using System.IO;

namespace Cognitive3D
{
    internal class ProjectValidationLog : MonoBehaviour
    {
        /// <summary>
        /// Stores and writes essential data for project validation into JSON file
        /// </summary>
        public static void UpdateLog()
        {
            ProjectValidationJSON pvj = new ProjectValidationJSON();
            pvj.applicationVersion = Application.version;

            string json = JsonUtility.ToJson(pvj);
            string filePath = EditorCore.GetBaseDirectoryPath() + "/projectvalidation.json";

            File.WriteAllText(filePath, json);
        }
    }

    [System.Serializable]
    internal class ProjectValidationJSON
    {
        public string applicationVersion;
    }
}
