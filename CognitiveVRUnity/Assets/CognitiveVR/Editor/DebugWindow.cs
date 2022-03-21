using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_2019_4_OR_NEWER
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
#endif

namespace CognitiveVR
{
    public class DebugInformationWindow : EditorWindow
    {
        public static void Init()
        {
            DebugInformationWindow window = (DebugInformationWindow)EditorWindow.GetWindow(typeof(DebugInformationWindow), true, "Debug Information");
            window.Show();
            Refresh();
        }

        static string DebugText;
        Vector2 view;

        public static string GetDebugContents()
        {
            Refresh();
            return DebugText;
        }

#if UNITY_2019_4_OR_NEWER
        static ListRequest Request;
        static List<string> PackageList = new List<string>();
        static string filePath;
        public static void WriteDebugToFile(string filepath)
        {
            filePath = filepath;
            Request = Client.List();    // List packages installed for the Project
            EditorApplication.update += Progress;
        }

        static void Progress()
        {
            if (Request.IsCompleted)
            {
                PackageList = new List<string>();

                if (Request.Status == StatusCode.Success)
                    foreach (var package in Request.Result)
                    {
                        PackageList.Add(package.name + " " + package.version);
                    }
                else if (Request.Status >= StatusCode.Failure)
                    Debug.Log(Request.Error.message);

                string debugContent = DebugInformationWindow.GetDebugContents();
                System.IO.File.WriteAllText(filePath, debugContent);

                EditorApplication.update -= Progress;
            }
        }
#else
        public static void WriteDebugToFile(string filepath)
        {
            string debugContent = GetDebugContents();
            System.IO.File.WriteAllText(filepath, debugContent);
        }
#endif

        static void Refresh()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
            CognitiveVR_Preferences p = CognitiveVR_Preferences.Instance;

            sb.AppendLine("*****************************");
            sb.AppendLine("***********SYSTEM************");
            sb.AppendLine("*****************************");
            sb.AppendLine("Unity Version:" + Application.unityVersion);
            sb.AppendLine("OS:" + SystemInfo.operatingSystem);
            sb.AppendLine("System Time: " + System.DateTime.Now.ToString());

#region Project Settings
            sb.AppendLine();
            sb.AppendLine("*****************************");
            sb.AppendLine("***********PROJECT***********");
            sb.AppendLine("*****************************");
            string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            sb.AppendLine("Scripting Define Symbols: " + s);

#if UNITY_2019_4_OR_NEWER
            sb.AppendLine("Packages:");
            foreach (var package in PackageList)
            {
                sb.AppendLine("  " + package);
            }
#endif

            sb.AppendLine("SDK Version: " + Core.SDK_VERSION);
            try
            {
                sb.AppendLine("Api Key: ****" + p.ApplicationKey.Substring(p.ApplicationKey.Length - 4));
            }
            catch
            {
                sb.AppendLine("Api Key: INVALID");
            }
            try
            {
                sb.AppendLine("Developer Key: ****" + EditorCore.DeveloperKey.Substring(EditorCore.DeveloperKey.Length - 4));
            }
            catch
            {
                sb.AppendLine("Developer Key: INVALID");
            }
            sb.AppendLine("Enable Logging: " + p.EnableLogging);
            sb.AppendLine("Enable Development Logging: " + p.EnableDevLogging);
            sb.AppendLine("Gaze Type: " + p.GazeType.ToString());
            sb.AppendLine("Snapshot Interval: " + p.SnapshotInterval);
            sb.AppendLine("Dynamic Object Search in Parent: " + p.DynamicObjectSearchInParent);
            sb.AppendLine("Dynamic Object Layer Mask: " + p.DynamicLayerMask);
            sb.AppendLine("Track GPS Location: " + p.TrackGPSLocation);
            sb.AppendLine("GPS Sync with Player Update: " + p.SyncGPSWithGaze);
            sb.AppendLine("GPS Update Interval: " + p.GPSInterval);
            sb.AppendLine("GPS Accuracy: " + p.GPSAccuracy);
            sb.AppendLine("Record Floor Position: " + p.RecordFloorPosition);
            sb.AppendLine("Gaze Snapshot Batch Size: " + p.GazeSnapshotCount);
            sb.AppendLine("Event Snapshot Batch Size: " + p.TransactionSnapshotCount);
            sb.AppendLine("Event Extreme Batch Size: " + p.TransactionExtremeSnapshotCount);
            sb.AppendLine("Event Minimum Timer: " + p.TransactionSnapshotMinTimer);
            sb.AppendLine("Event Automatic Send Timer: " + p.TransactionSnapshotMaxTimer);

            sb.AppendLine("Dynamic Snapshot Batch Size: " + p.DynamicSnapshotCount);
            sb.AppendLine("Dynamic Extreme Batch Size: " + p.DynamicExtremeSnapshotCount);
            sb.AppendLine("Dynamic Minimum Timer: " + p.DynamicSnapshotMinTimer);
            sb.AppendLine("Dynamic Automatic Send Timer: " + p.DynamicSnapshotMaxTimer);

            sb.AppendLine("Sensor Snapshot Batch Size: " + p.SensorSnapshotCount);
            sb.AppendLine("Sensor Extreme Batch Size: " + p.SensorExtremeSnapshotCount);
            sb.AppendLine("Sensor Minimum Timer: " + p.SensorSnapshotMinTimer);
            sb.AppendLine("Sensor Automatic Send Timer: " + p.SensorSnapshotMaxTimer);

            sb.AppendLine("Fixation Snapshot Batch Size: " + p.FixationSnapshotCount);
            sb.AppendLine("Fixation Extreme Batch Size: " + p.FixationExtremeSnapshotCount);
            sb.AppendLine("Fixation Minimum Timer: " + p.FixationSnapshotMinTimer);
            sb.AppendLine("Fixation Automatic Send Timer: " + p.FixationSnapshotMaxTimer);

            sb.AppendLine("Save Data to Local Cache if no internet connection: " + p.LocalStorage);
            sb.AppendLine("Cache Size (bytes): " + p.LocalDataCacheSize);
            sb.AppendLine("Cache Size (mb): " + EditorUtility.FormatBytes(p.LocalDataCacheSize));
            sb.AppendLine("Custom Protocol: " + p.Protocol);
            sb.AppendLine("Custom Gateway: " + p.Gateway);
            sb.AppendLine("Custom Viewer: " + p.Viewer);
            sb.AppendLine("Custom Dashboard: " + p.Dashboard);

            sb.AppendLine("Send Data on HMD Remove: " + p.SendDataOnHMDRemove);
            sb.AppendLine("Send Data on Level Load: " + p.SendDataOnLevelLoad);
            sb.AppendLine("Send Data on Quit: " + p.SendDataOnQuit);
            sb.AppendLine("Send Data on Hotkey: " + p.SendDataOnHotkey);
            sb.AppendLine("Send Data Primary Hotkey: " + p.SendDataHotkey);
            sb.AppendLine("Send Data Hotkey Modifiers: " + p.HotkeyShift + " " + p.HotkeyCtrl + " " + p.HotkeyAlt);

            sb.AppendLine("Texture Export Quality: " + p.TextureResize);
            sb.AppendLine("Export Lowest LOD from LODGroup Components: " + p.ExportSceneLODLowest);
            sb.AppendLine("Export AO Maps: " + p.ExportAOMaps);

            sb.AppendLine("Scene Settings:");

            for(int i = 0; i< p.sceneSettings.Count;i++)
            {
                var scene = p.sceneSettings[i];
                if (i != p.sceneSettings.Count -1)
                {
                    sb.AppendLine("  ├─" + scene.SceneName);
                    sb.AppendLine("  │  ├─Scene Id: " + scene.SceneId);
                    sb.AppendLine("  │  ├─Scene Path: " + scene.ScenePath);
                    sb.AppendLine("  │  ├─Last Revision: " + scene.LastRevision);
                    sb.AppendLine("  │  ├─Version Number: " + scene.VersionNumber);
                    sb.AppendLine("  │  └─Version Id: " + scene.VersionId);
                }
                else
                {
                    sb.AppendLine("  └─" + scene.SceneName);
                    sb.AppendLine("     ├─Scene Id: " + scene.SceneId);
                    sb.AppendLine("     ├─Scene Path: " + scene.ScenePath);
                    sb.AppendLine("     ├─Last Revision: " + scene.LastRevision);
                    sb.AppendLine("     ├─Version Number: " + scene.VersionNumber);
                    sb.AppendLine("     └─Version Id: " + scene.VersionId);
                }
            }
#endregion

#region Current Scene
            sb.AppendLine();
            sb.AppendLine("*****************************");
            sb.AppendLine("********CURRENT SCENE********");
            sb.AppendLine("*****************************");

            var currentScene = CognitiveVR_Preferences.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            if (currentScene != null)
            {
                sb.AppendLine("Scene Name: " + currentScene.SceneName);
                sb.AppendLine("Scene Id: " + currentScene.SceneId);
                sb.AppendLine("Scene Path: " + currentScene.ScenePath);
                sb.AppendLine("Last Revision: " + currentScene.LastRevision);
                sb.AppendLine("Version Number: " + currentScene.VersionNumber);
                sb.AppendLine("Version Id: " + currentScene.VersionId);

                string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                string objPath = EditorCore.GetSubDirectoryPath(fullName);

                if (System.IO.Directory.Exists(objPath))
                {
                    var size = GetDirectorySize(objPath);
                    sb.AppendLine("Scene Size (mb): " + string.Format("{0:0.00}", (size / 1048576f)));
                }
                else
                {
                    sb.AppendLine("Scene Not Exported " + objPath);
                }
            }
            else
            {
                var currentEditorScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
                sb.AppendLine("Scene Name: " + currentEditorScene.name);
                sb.AppendLine("Scene Settings not included in Preferences");
            }

            var mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.gameObject != null)
            {
                sb.AppendLine("Main Camera GameObject: " + mainCamera.gameObject.name);
            }
            else
            {
                sb.AppendLine("No Main Camera in scene");
            }

            var manager = FindObjectOfType<CognitiveVR_Manager>();
            if (manager != null)
            {
                sb.AppendLine("Manager Initialize On Start: " + manager.InitializeOnStart);
                sb.AppendLine("Manager Startup Delay Time (s): " + manager.StartupDelayTime);
            }
            else
            {
                sb.AppendLine("No Manager in scene");
            }
#endregion

#region Scene Dynamics
            sb.AppendLine();
            sb.AppendLine("*****************************");
            sb.AppendLine("****CURRENT SCENE OBJECTS****");
            sb.AppendLine("*****************************");

            var sceneDynamics = FindObjectsOfType<DynamicObject>();
            sb.AppendLine("Dynamic Object Count: " + sceneDynamics.Length);

            for(int i = 0; i< sceneDynamics.Length;i++)
            {
                var dynamic = sceneDynamics[i];

                bool last = i == sceneDynamics.Length - 1;
                string headerLine =     "  ├─";
                string preLineMid =     "  │  ├─";
                string preLineLast =    "  │  └─";
                if (last)
                {
                    headerLine =        "  └─";
                    preLineMid =        "     ├─";
                    preLineLast =       "     └─";
                }

                sb.AppendLine(headerLine + dynamic.gameObject.name);
                sb.AppendLine(preLineMid+"Mesh Name: " + dynamic.MeshName);
                var mainCollider = dynamic.GetComponent<Collider>();
                if (mainCollider != null)
                {
                    sb.AppendLine(preLineMid+"Has Collider: true");
                    sb.AppendLine(preLineMid+"Collider Type: " + mainCollider.GetType().ToString());
                }
                else
                {
                    sb.AppendLine(preLineMid+"Has Collider: false");
                }

                if (dynamic.transform.childCount > 0)
                {
                    sb.AppendLine(preLineMid+"Has Children: true");
                    int expectedColliderCount = mainCollider != null ? 1 : 0;
                    if (dynamic.GetComponentsInChildren<Collider>().Length > expectedColliderCount)
                    {
                        sb.AppendLine(preLineLast + "Has Child Colliders: true");
                    }
                    else
                    {
                        sb.AppendLine(preLineLast+"Has Child Colliders: false");
                    }
                }
                else
                {
                    sb.AppendLine(preLineLast+"Has Children: false");
                }
            }
#endregion
            sb.AppendLine();
            sb.AppendLine("*****************************");
            sb.AppendLine("********EXPORT FOLDER********");
            sb.AppendLine("*****************************");

            string baseDirectory = EditorCore.GetBaseDirectoryPath();
            if (System.IO.Directory.Exists(baseDirectory))
            {
                System.IO.DirectoryInfo d = new System.IO.DirectoryInfo(baseDirectory);
                sb.AppendLine("/" + d.Name + " (" + string.Format("{0:0}", (GetDirectorySize(baseDirectory) / 1048576f)) + "mb)");
                AppendDirectory(sb, baseDirectory, 1);
            }

            DebugText = sb.ToString();
        }

        void OnGUI()
        {
            view = GUILayout.BeginScrollView(view);
            GUILayout.Label(DebugText);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh",GUILayout.Width(80)))
            {
                Refresh();
            }

            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = DebugText;
            }
            GUILayout.EndHorizontal();
        }

        //probably need to do a stack of directory/file counts
        static void AppendDirectory(System.Text.StringBuilder sb, string directoryPath, int depth)
        {
            int lastItemIndex = System.IO.Directory.GetFiles(directoryPath).Length + System.IO.Directory.GetDirectories(directoryPath).Length-1;
            
            for (int i = 0; i< System.IO.Directory.GetFiles(directoryPath).Length;i++)
            {
                var file = System.IO.Directory.GetFiles(directoryPath)[i];
                System.IO.FileInfo fi = new System.IO.FileInfo(file);
                string filename = System.IO.Path.GetFileName(file);

                for (int j = 0; j < depth; j++)
                {
                    sb.Append("   ");
                }
                sb.AppendLine(filename + " (" + string.Format("{0:0.00}", (fi.Length / 1048576f)) + "mb)");
            }

            for (int i = 0; i <= lastItemIndex - System.IO.Directory.GetFiles(directoryPath).Length; i++)
            {
                var dir = System.IO.Directory.GetDirectories(directoryPath)[i];
                System.IO.DirectoryInfo d = new System.IO.DirectoryInfo(dir);
                for (int j = 0; j < depth; j++)
                {
                    sb.Append("   ");
                }
                sb.AppendLine("/"+d.Name + " (" + string.Format("{0:0.00}", (GetDirectorySize(dir) / 1048576f)) + "mb)");
                AppendDirectory(sb, dir, depth + 1);
            }
        }

        static long GetDirectorySize(string p)
        {
            string[] a = System.IO.Directory.GetFiles(p, "*.*", System.IO.SearchOption.AllDirectories);
            long b = 0;
            foreach (string name in a)
            {
                System.IO.FileInfo info = new System.IO.FileInfo(name);
                b += info.Length;
            }
            return b;
        }
    }
}