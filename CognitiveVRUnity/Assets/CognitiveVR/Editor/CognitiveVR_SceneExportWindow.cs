using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using System;

namespace CognitiveVR
{
    public class CognitiveVR_SceneExportWindow : EditorWindow
    {
        static string appendName = "";

        static Vector2 canvasPos;
        static CognitiveVR_Preferences prefs;
        static bool remapHotkey;

        [MenuItem("cognitiveVR/Scene Export")]
        public static void Init()
        {
            // Get existing open window or if none, make a new one:
            CognitiveVR_SceneExportWindow window = (CognitiveVR_SceneExportWindow)GetWindow(typeof(CognitiveVR_SceneExportWindow),true, "cognitiveVR Scene Export");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        static int sceneIndex = 0;
        bool exportOptionsFoldout = false;
        bool hideNonBuildScenes = false;
        static CognitiveVR_Preferences.SceneKeySetting currentSceneSettings;

        bool loadedScenes = false;
        void OnGUI()
        {
            if (!loadedScenes)
            {
                ReadNames();
                loadedScenes = true;
            }

            GUI.skin.label.richText = true;

            prefs = CognitiveVR_Settings.GetPreferences();

            //=========================
            //scene select
            //=========================

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Scene Export Manager</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            List<string> sceneNames = new List<string>();
            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                if (hideNonBuildScenes)
                {
                    for (int i = 0; i < EditorBuildSettings.scenes.Length; i++)
                    {
                        if (EditorBuildSettings.scenes[i].path == path)
                        {
                            sceneNames.Add(name);
                        }
                    }
                }
                else
                {
                    sceneNames.Add(name);
                }
            }

            sceneIndex = EditorGUILayout.Popup(sceneIndex, sceneNames.ToArray());

            hideNonBuildScenes = GUILayout.Toggle(hideNonBuildScenes, "Hide non-building scenes");

            string selectedSceneName = "invalid scene name";
            if (sceneIndex < sceneNames.Count)
            {
                selectedSceneName = sceneNames[sceneIndex];
            }
            //GUILayout.Label(selectedSceneName);

            //when should scenes and keys get added to this?
            currentSceneSettings = CognitiveVR_Settings.GetPreferences().FindScene(selectedSceneName);

            System.DateTime revisionDate = System.DateTime.MinValue;

            if (currentSceneSettings != null)
            {
                revisionDate = currentSceneSettings.LastRevision;
            }

            //revision date

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Last Scene Revision:</b></size> " + revisionDate.ToShortDateString());
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);


            //compression amount

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Scene Export Quality</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            bool lowSettings = false;
            bool defaultSettings = false;
            bool highSettings = false;
            bool customSettings = false;

            if (ExportSettings.Match(CognitiveVR_Preferences.LowSettings, prefs.ExportSettings))
            {
                lowSettings = true;
            }
            else if (ExportSettings.Match(CognitiveVR_Preferences.DefaultSettings, prefs.ExportSettings))
            {
                defaultSettings = true;
            }
            else if (ExportSettings.Match(CognitiveVR_Preferences.HighSettings, prefs.ExportSettings))
            {
                highSettings = true;
            }
            else
            {
                customSettings = true;
            }

            if (GUILayout.Toggle(lowSettings, "Low", EditorStyles.radioButton))
            {
                prefs.ExportSettings = ExportSettings.Copy(CognitiveVR_Preferences.LowSettings);
                defaultSettings = false;
                highSettings = false;
            }
            if (GUILayout.Toggle(defaultSettings, "Default", EditorStyles.radioButton))
            {
                prefs.ExportSettings = ExportSettings.Copy(CognitiveVR_Preferences.DefaultSettings);
                lowSettings = false;
                highSettings = false;
            }
            if (GUILayout.Toggle(highSettings, "High", EditorStyles.radioButton))
            {
                prefs.ExportSettings = ExportSettings.Copy(CognitiveVR_Preferences.HighSettings);
                defaultSettings = false;
                highSettings = false;
            }
            if (GUILayout.Toggle(customSettings, "Custom", EditorStyles.radioButton))
            {
                exportOptionsFoldout = true;
            }

            GUILayout.EndHorizontal();

            exportOptionsFoldout = EditorGUILayout.Foldout(exportOptionsFoldout, "Advanced Options");
            EditorGUI.indentLevel++;
            if (exportOptionsFoldout)
            {
                prefs.ExportSettings.ExportStaticOnly = EditorGUILayout.Toggle(new GUIContent("Export Static Geo Only", "Only export meshes marked as static. Dynamic objects (such as vehicles, doors, etc) will not be exported"), prefs.ExportSettings.ExportStaticOnly);
                prefs.ExportSettings.MinExportGeoSize = EditorGUILayout.FloatField(new GUIContent("Minimum export size", "Ignore exporting meshes that are below this size(pebbles, grass,etc)"), prefs.ExportSettings.MinExportGeoSize);
                prefs.ExportSettings.ExplorerMinimumFaceCount = EditorGUILayout.IntField(new GUIContent("Minimum Face Count", "Ignore decimating objects with fewer faces than this value"), prefs.ExportSettings.ExplorerMinimumFaceCount);
                prefs.ExportSettings.ExplorerMaximumFaceCount = EditorGUILayout.IntField(new GUIContent("Maximum Face Count", "Objects with this many faces will be decimated to 10% of their original face count"), prefs.ExportSettings.ExplorerMaximumFaceCount);

                GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter"), new GUIContent("Eighth"), new GUIContent("Sixteenth") };
                int[] textureQualities = new int[] { 1, 2, 4, 8, 16 };
                prefs.ExportSettings.TextureQuality = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), prefs.ExportSettings.TextureQuality, textureQualityNames, textureQualities);

                if (prefs.ExportSettings.ExplorerMinimumFaceCount < 0) { prefs.ExportSettings.ExplorerMinimumFaceCount = 0; }
                if (prefs.ExportSettings.ExplorerMaximumFaceCount < 1) { prefs.ExportSettings.ExplorerMaximumFaceCount = 1; }
                if (prefs.ExportSettings.ExplorerMinimumFaceCount > prefs.ExportSettings.ExplorerMaximumFaceCount) { prefs.ExportSettings.ExplorerMinimumFaceCount = prefs.ExportSettings.ExplorerMaximumFaceCount; }
            }
            EditorGUI.indentLevel--;

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            if (string.IsNullOrEmpty(prefs.SavedBlenderPath))
            {
                FindBlender();
            }

            bool validBlenderPath = prefs.SavedBlenderPath.ToLower().EndsWith("blender.exe");

            if (GUILayout.Button("Select Blender.exe"))
            {
                prefs.SavedBlenderPath = EditorUtility.OpenFilePanel("Select Blender.exe", string.IsNullOrEmpty(prefs.SavedBlenderPath) ? "c:\\" : prefs.SavedBlenderPath, "");
            }

            //appendName = EditorGUILayout.TextField(new GUIContent("Append to File Name", "This could be a level's number and version"), appendName);

            EditorGUI.BeginDisabledGroup(!validBlenderPath);
            if (GUILayout.Button(new GUIContent("Export Scene", "Exports the scene to Blender and reduces polygons. This also exports required textures at a low resolution")))
            {
                ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize,prefs.ExportSettings.TextureQuality);   
            }

            EditorGUI.EndDisabledGroup();



            return;

            prefs = CognitiveVR_Settings.GetPreferences();


            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent("Customer ID", "The identifier for your company and product on the CognitiveVR Dashboard"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            prefs.CustomerID = EditorGUILayout.TextField(prefs.CustomerID);

            GUILayout.Space(10);


            /*if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(prefs);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR_ComponentSetup.Init();
            }*/

            canvasPos = GUILayout.BeginScrollView(canvasPos, false, false);

            GUILayout.Space(10);

            //prefs.SnapshotInterval = EditorGUILayout.FloatField(new GUIContent("Interval for Player Snapshots", "Delay interval for:\nHMD Height\nHMD Collsion\nArm Length\nController Collision"), prefs.SnapshotInterval);
            //prefs.SnapshotInterval = Mathf.Max(prefs.SnapshotInterval, 0.1f);

            //prefs.ComfortTrackingInterval = EditorGUILayout.Slider(new GUIContent("Comfort Tracking Send Rate", "Number of seconds used to average to determine comfort level. Lower means more smaller samples and more detail"), prefs.ComfortTrackingInterval, 3, 60);
            //prefs.OnlySendComfortOnLowFPS = EditorGUILayout.Toggle(new GUIContent("Only Send Low FPS", "Ignore sending Comfort at set intervals. Only send FPS events below the threshold"), prefs.OnlySendComfortOnLowFPS);
            //prefs.LowFramerateThreshold = EditorGUILayout.IntField(new GUIContent("Low Framerate Threshold", "Falling below and rising above this threshold will send events"), prefs.LowFramerateThreshold);
            //prefs.LowFramerateThreshold = Mathf.Max(prefs.LowFramerateThreshold, 1);

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent("Player Recorder Options", "Settings for how the Player Recorder collects and sends data to SceneExplorer.com"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //radio buttons

            GUIStyle selectedRadio = new GUIStyle(GUI.skin.label);
            selectedRadio.normal.textColor = new Color(0, 0.0f, 0, 1.0f);
            selectedRadio.fontStyle = FontStyle.Bold;

            //3D content
            GUILayout.BeginHorizontal();
            if (prefs.PlayerDataType == 0)
            {
                GUILayout.Label("3D Content (default)", selectedRadio, GUILayout.Width(195));
            }
            else
            {
                GUILayout.Label("3D Content (default)", GUILayout.Width(195));
            }
            bool o = prefs.PlayerDataType == 0;
            bool b = GUILayout.Toggle(prefs.PlayerDataType == 0, "",EditorStyles.radioButton);
            if (b != o)
            {
                prefs.PlayerDataType = 0;
            }
            GUILayout.EndHorizontal();

            //360 video content
            GUILayout.BeginHorizontal();
            if (prefs.PlayerDataType == 1)
            {
                GUILayout.Label(new GUIContent("360 Video Content", "Video content displayed in a sphere"), selectedRadio, GUILayout.Width(195));
            }
            else
            {
                GUILayout.Label(new GUIContent("360 Video Content", "Video content displayed in a sphere"), GUILayout.Width(195));
            }
            
            bool originalContentType = prefs.PlayerDataType == 1;
            bool selectedContentType = GUILayout.Toggle(prefs.PlayerDataType == 1, "", EditorStyles.radioButton);
            if (selectedContentType != originalContentType)
            {
                prefs.PlayerDataType = 1;
            }
            GUILayout.EndHorizontal();

            if (GUI.changed)
            {
                if (prefs.PlayerDataType == 0) //3d content
                {
                    prefs.TrackPosition = true;
                    prefs.TrackGazePoint = true;
                    prefs.TrackGazeDirection = false;
                    prefs.GazePointFromDirection = false;
                }
                else //video content
                {
                    prefs.TrackPosition = true;
                    prefs.TrackGazePoint = false;
                    prefs.TrackGazeDirection = false;
                    prefs.GazePointFromDirection = true;
                }
            }

            EditorGUI.BeginDisabledGroup(prefs.PlayerDataType != 1);
            prefs.GazeDirectionMultiplier = EditorGUILayout.FloatField(new GUIContent("Video Sphere Radius", "Multiplies the normalized GazeDirection"), prefs.GazeDirectionMultiplier);
            prefs.GazeDirectionMultiplier = Mathf.Max(0.1f, prefs.GazeDirectionMultiplier);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            //prefs.SendDataOnLevelLoad = EditorGUILayout.Toggle(new GUIContent("Send Data on Level Load", "Send all snapshots on Level Loaded"), prefs.SendDataOnLevelLoad);
            //prefs.SendDataOnQuit = EditorGUILayout.Toggle(new GUIContent("Send Data on Quit", "Sends all snapshots on Application OnQuit\nNot reliable on Mobile"), prefs.SendDataOnQuit);
            //prefs.DebugWriteToFile = EditorGUILayout.Toggle(new GUIContent("DEBUG - Write snapshots to file", "Write snapshots to file AND upload to SceneExplorer"), prefs.DebugWriteToFile);
            //prefs.SendDataOnHotkey = EditorGUILayout.Toggle(new GUIContent("DEBUG - Send Data on Hotkey", "Press a hotkey to send data"), prefs.SendDataOnHotkey);
            //prefs.SendDataOnHMDRemove = EditorGUILayout.Toggle(new GUIContent("Send data on HMD remove", "Send all snapshots on HMD remove event"), prefs.SendDataOnHMDRemove);

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scene Explorer Export Options");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();


#if UNITY_EDITOR_OSX
            EditorGUILayout.HelpBox("Exporting scenes is not available on Mac at this time", MessageType.Warning);
            EditorGUI.BeginDisabledGroup(true);

#endif

            prefs.ExportSettings.ExportStaticOnly = EditorGUILayout.Toggle(new GUIContent("Export Static Geo Only", "Only export meshes marked as static. Dynamic objects (such as vehicles, doors, etc) will not be exported"), prefs.ExportSettings.ExportStaticOnly);
            prefs.ExportSettings.MinExportGeoSize = EditorGUILayout.FloatField(new GUIContent("Minimum export size", "Ignore exporting meshes that are below this size(pebbles, grass,etc)"), prefs.ExportSettings.MinExportGeoSize);
            appendName = EditorGUILayout.TextField(new GUIContent("Append to File Name", "This could be a level's number and version"), appendName);
            prefs.ExportSettings.ExplorerMinimumFaceCount = EditorGUILayout.IntField(new GUIContent("Minimum Face Count", "Ignore decimating objects with fewer faces than this value"), prefs.ExportSettings.ExplorerMinimumFaceCount);
            prefs.ExportSettings.ExplorerMaximumFaceCount = EditorGUILayout.IntField(new GUIContent("Maximum Face Count", "Objects with this many faces will be decimated to 10% of their original face count"), prefs.ExportSettings.ExplorerMaximumFaceCount);

            if (prefs.ExportSettings.ExplorerMinimumFaceCount < 0) { prefs.ExportSettings.ExplorerMinimumFaceCount = 0; }
            if (prefs.ExportSettings.ExplorerMaximumFaceCount < 1) { prefs.ExportSettings.ExplorerMaximumFaceCount = 1; }
            if (prefs.ExportSettings.ExplorerMinimumFaceCount > prefs.ExportSettings.ExplorerMaximumFaceCount) { prefs.ExportSettings.ExplorerMinimumFaceCount = prefs.ExportSettings.ExplorerMaximumFaceCount; }

            if (string.IsNullOrEmpty(prefs.SavedBlenderPath))
            {
                FindBlender();
            }

            EditorGUILayout.LabelField("Path To Blender", prefs.SavedBlenderPath);
            if (GUILayout.Button("Select Blender.exe"))
            {
                prefs.SavedBlenderPath = EditorUtility.OpenFilePanel("Select Blender.exe", string.IsNullOrEmpty(prefs.SavedBlenderPath) ? "c:\\" : prefs.SavedBlenderPath, "");

                if (!string.IsNullOrEmpty(prefs.SavedBlenderPath))
                {
                    //prefs.SavedBlenderPath = prefs.SavedBlenderPath.Substring(0, prefs.SavedBlenderPath.Length - "blender.exe".Length) + "";
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Export Scene", "Exports the scene to Blender and reduces polygons. This also exports required textures at a low resolution")))
            {
                ExportScene(true,prefs.ExportSettings.ExportStaticOnly,prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality);
            }

            if (GUILayout.Button(new GUIContent("Export Scene SKIP TEXTURES", "Exports only the scene geometry to Blender and reduces polygons")))
            {
                ExportScene(false, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality);
            }
            GUILayout.EndHorizontal();

#if UNITY_EDITOR_OSX
            EditorGUI.EndDisabledGroup();
#endif

            if (GUILayout.Button(new GUIContent("Manage Scene IDs", "Open window to set which tracked player data is uploaded to your scenes")))
            {
                CognitiveVR_SceneKeyConfigurationWindow.Init();
            }

            GUILayout.EndScrollView();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(prefs);
            }
        }

        static bool BlenderRequest;
        static bool HasOpenedBlender;
        //TODO check for the specific blender that was opened. save var when process.start(thisblender)

        static void UpdateProcess()
        {
            Process[] blenders;
            if (BlenderRequest == true)
            {
                //Debug.Log("BLENDER - opening");
                blenders = Process.GetProcessesByName("blender");
                if (blenders.Length > 0)
                {
                    BlenderRequest = false;
                    HasOpenedBlender = true;
                }
            }
            if (HasOpenedBlender)
            {
                blenders = Process.GetProcessesByName("blender");
                if (blenders.Length > 0)
                {
                    //Debug.Log("BLENDER - do work");
                }
                else
                {
                    //Debug.Log("BLENDER - finished work");
                    EditorApplication.update -= UpdateProcess;
                    HasOpenedBlender = false;
                    UploadDecimatedScene();
                }
            }
        }

        static void UploadDecimatedScene()
        {
            if (currentSceneSettings != null)
                currentSceneSettings.LastRevision = System.DateTime.UtcNow;

            //TODO get this scene name
            //use that to figure out which directory
            //get all files in teh directory
            //remove scenename.obj and scenename.mtl
            //http POST to sceneexplorer.com/upload
            //get sceneID back when upload complete
        }

        public static void ExportScene(bool includeTextures, bool staticGeometry, float minSize, int textureDivisor)
        {
            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + appendName;

            bool successfulExport = CognitiveVR_SceneExplorerExporter.ExportWholeSelectionToSingle(fullName, includeTextures,staticGeometry,minSize,textureDivisor);

            if (!successfulExport)
            {
                Debug.LogError("Scene export canceled!");
                return;
            }

            if (string.IsNullOrEmpty(prefs.SavedBlenderPath) || !prefs.SavedBlenderPath.ToLower().EndsWith("blender.exe"))
            {
                Debug.LogError("Blender.exe is not found during scene export! Use Edit>Preferences...CognitivePreferences to locate Blender.exe\nScene: "+ fullName+" exported to folder but not mesh decimated!");
                return;
            }

            string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(fullName);
            string decimateScriptPath = Application.dataPath + "/CognitiveVR/Editor/decimateall.py";


            //System.Diagnostics.Process.Start("http://google.com/search?q=" + "cat pictures");

            decimateScriptPath = decimateScriptPath.Replace(" ", "\" \"");
            objPath = objPath.Replace(" ", "\" \"");
            fullName = fullName.Replace(" ", "\" \"");

            EditorUtility.ClearProgressBar();


            ProcessStartInfo ProcessInfo;

            ProcessInfo = new ProcessStartInfo(prefs.SavedBlenderPath);
            ProcessInfo.UseShellExecute = true;
            ProcessInfo.Arguments = "-P " + decimateScriptPath + " " + objPath + " " + prefs.ExportSettings.ExplorerMinimumFaceCount + " " + prefs.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;

            //changing scene while blender is decimating the level will break the file that will be automatically uploaded
            Process.Start(ProcessInfo);
            BlenderRequest = true;
            HasOpenedBlender = false;
            //EditorApplication.update += UpdateProcess;
        }

        public static bool FoldoutButton(string title, bool showing)
        {
            string fullTitle = showing ? "Hide ":"Show " ;
            fullTitle += title + " Options";
            GUILayout.Space(4);
            return GUILayout.Button(fullTitle, EditorStyles.toolbarButton);
        }

        static List<int> layerNumbers = new List<int>();

        static LayerMask LayerMaskField(GUIContent content, LayerMask layerMask)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;

            layerNumbers.Clear();

            for (int i = 0; i < layers.Length; i++)
                layerNumbers.Add(LayerMask.NameToLayer(layers[i]));

            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if (((1 << layerNumbers[i]) & layerMask.value) > 0)
                    maskWithoutEmpty |= (1 << i);
            }

            maskWithoutEmpty = UnityEditor.EditorGUILayout.MaskField(content, maskWithoutEmpty, layers);

            int mask = 0;
            for (int i = 0; i < layerNumbers.Count; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) > 0)
                    mask |= (1 << layerNumbers[i]);
            }
            layerMask.value = mask;

            return layerMask;
        }

        public static void ExecuteCommand(string Command)
        {
            System.Diagnostics.ProcessStartInfo ProcessInfo;

            ProcessInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/C " + Command);
            ProcessInfo.UseShellExecute = true;

            System.Diagnostics.Process.Start(ProcessInfo);
        }

        static void FindBlender()
        {
            if (Directory.Exists(@"C:/Program Files/"))
            {
                if (Directory.Exists(@"C:/Program Files/Blender Foundation/"))
                {
                    if (Directory.Exists(@"C:/Program Files/Blender Foundation/Blender"))
                    {
                        if (File.Exists(@"C:/Program Files/Blender Foundation/Blender/blender.exe"))
                        {
                            CognitiveVR_Preferences.Instance.SavedBlenderPath = @"C:/Program Files/Blender Foundation/Blender/blender.exe";
                        }
                    }
                }
            }
            else if (Directory.Exists(@"C:/Program Files (x86)"))
            {
                if (Directory.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64"))
                {
                    if (Directory.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64"))
                    {
                        if (File.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe"))
                        {
                            CognitiveVR_Preferences.Instance.SavedBlenderPath = @"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe";
                        }
                    }
                }
            }
        }

        private static void ReadNames()
        {
            //save these to a temp list
            List<CognitiveVR_Preferences.SceneKeySetting> oldSettings = new List<CognitiveVR_Preferences.SceneKeySetting>();
            foreach (var v in CognitiveVR_Preferences.Instance.SceneKeySettings)
            {
                oldSettings.Add(v);
            }


            //clear then rebuild the list in preferences
            CognitiveVR_Preferences.Instance.SceneKeySettings.Clear();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                CognitiveVR_Preferences.Instance.SceneKeySettings.Add(new CognitiveVR_Preferences.SceneKeySetting(name, path));
            }

            //match up dictionary keys from temp list
            foreach (var oldSetting in oldSettings)
            {
                foreach (var newSetting in CognitiveVR_Preferences.Instance.SceneKeySettings)
                {
                    if (newSetting.SceneName == oldSetting.SceneName)
                    {
                        newSetting.SceneKey = oldSetting.SceneKey;
                        newSetting.Track = oldSetting.Track;
                        newSetting.LastRevision = oldSetting.LastRevision;
                        newSetting.SceneName = oldSetting.SceneName;
                        newSetting.ScenePath = oldSetting.ScenePath;
                    }
                }
            }
        }
    }
}