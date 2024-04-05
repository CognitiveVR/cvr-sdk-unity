using UnityEngine;
using System.IO;

namespace Cognitive3D
{
    internal class ProjectValidationLog : MonoBehaviour
    {
        internal static string filePath = System.IO.Path.GetDirectoryName(Application.dataPath) + "/Cognitive3D_ProjectValidation.json";

        /// <summary>
        /// Stores and writes essential data for project validation into JSON file
        /// </summary>
        public static void UpdateLog()
        {
            ProjectValidationJSON pvj = new ProjectValidationJSON();
            pvj.applicationVersion = Application.version;

            string json = JsonUtility.ToJson(pvj);

            File.WriteAllText(filePath, json);
        }
    }

    [System.Serializable]
    internal class ProjectValidationJSON
    {
        public string applicationVersion;
    }
}
