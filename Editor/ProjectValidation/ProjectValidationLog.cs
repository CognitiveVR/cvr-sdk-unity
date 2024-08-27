using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Cognitive3D
{
    internal class ProjectValidationLog : MonoBehaviour
    {
        internal static readonly string FILEPATH = System.IO.Path.GetDirectoryName(Application.dataPath) + "/Cognitive3D_ProjectValidation.json";

        /// <summary>
        /// Stores and writes essential data for project validation into JSON file
        /// </summary>
        public static void UpdateLog()
        {
            ProjectValidationJSON pvj = new ProjectValidationJSON();
            pvj.applicationVersion = Application.version;

            string json = JsonUtility.ToJson(pvj);

            File.WriteAllText(FILEPATH, json);
        }

        public static void SetBuildProcessPopup(bool showPopup)
        {
            //file doesn't exist, create a new file
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                pvj = new ProjectValidationJSON();
            }
            pvj.showBuildPopup = showPopup;

            //write to file path
            File.WriteAllText(FILEPATH, JsonUtility.ToJson(pvj));
        }

        public static bool GetBuildProcessPopup()
        {
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                return true;
            }
            return pvj.showBuildPopup;
        }

        public static void AddIgnoreItem(string itemId)
        {
            //file doesn't exist, create a new file
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                pvj = new ProjectValidationJSON();
            }

            //item already ignored
            if (pvj.ignoredItems.Contains(itemId)) { return; }

            //add item to ignored list
            pvj.ignoredItems.Add(itemId);

            //write to file path
            File.WriteAllText(FILEPATH, JsonUtility.ToJson(pvj));
        }

        public static void RemoveIgnoreItem(string itemId)
        {
            //file doesn't exist, create a new file
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                pvj = new ProjectValidationJSON();
            }

            //item doesn't exist
            if (!pvj.ignoredItems.Contains(itemId)) { return; }

            //remove item from ignored list
            pvj.ignoredItems.Remove(itemId);

            //write to file path
            File.WriteAllText(FILEPATH, JsonUtility.ToJson(pvj));
        }

        static bool ReadLog(out ProjectValidationJSON projectValidationLog)
        {
            if (!File.Exists(FILEPATH))
            {
                projectValidationLog = null;
                return false;
            }
            projectValidationLog = JsonUtility.FromJson<ProjectValidationJSON>(File.ReadAllText(FILEPATH));
            return true;
        }

        public static List<string> GetLogIgnoreItems()
        {
            //file doesn't exist, return empty list
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                return new List<string>();
            }
            return pvj.ignoredItems;
        }

        public static bool ContainsIgnoreItem(string message)
        {
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                return false;
            }
            return pvj.ignoredItems.Contains(message);
        }

        public static void ClearIgnoreItems()
        {
            ProjectValidationJSON pvj;
            if (!ReadLog(out pvj))
            {
                return;
            }
            pvj.ignoredItems.Clear();

            //write to file path
            File.WriteAllText(FILEPATH, JsonUtility.ToJson(pvj));
        }
    }

    [System.Serializable]
    internal class ProjectValidationJSON
    {
        public bool showBuildPopup = true;
        public string applicationVersion;
        public List<string> ignoredItems = new List<string>();
    }
}
