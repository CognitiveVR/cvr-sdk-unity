using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace CognitiveVR
{
    public class CognitiveVR_EditorPrefs : EditorWindow
    {
        static string appendName = "";

        static Vector2 canvasPos;
        static CognitiveVR_Preferences prefs;
        static bool remapHotkey;

        [PreferenceItem("CognitiveVR")]
        public static void CustomPreferencesGUI()
        {
            prefs = GetPreferences();
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Version: " + Core.SDK_Version);

            GUI.color = Color.blue;
            if (GUILayout.Button("Documentation", EditorStyles.whiteLabel))
                Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/wiki");
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(new GUIContent("Customer ID", "The identifier for your company and product on the CognitiveVR Dashboard"));
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            prefs.CustomerID = EditorGUILayout.TextField(prefs.CustomerID);

            GUILayout.Space(10);


            if (GUILayout.Button("Save"))
            {
                EditorUtility.SetDirty(prefs);
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR_ComponentSetup.Init();
            }

            canvasPos = GUILayout.BeginScrollView(canvasPos, false, false);

            GUILayout.Space(10);

            prefs.SnapshotInterval = EditorGUILayout.FloatField(new GUIContent("Interval for Player Snapshots", "Delay interval for:\nHMD Height\nHMD Collsion\nArm Length\nController Collision"), prefs.SnapshotInterval);
            prefs.SnapshotInterval = Mathf.Max(prefs.SnapshotInterval, 0.1f);
            prefs.LowFramerateThreshold = EditorGUILayout.IntField(new GUIContent("Low Framerate Threshold", "Falling below and rising above this threshold will send events"), prefs.LowFramerateThreshold);
            prefs.LowFramerateThreshold = Mathf.Max(prefs.LowFramerateThreshold, 1);
            prefs.CollisionLayerMask = LayerMaskField(new GUIContent("Collision Layer", "LayerMask for HMD and Controller Collision events"), prefs.CollisionLayerMask);
            prefs.GazeObjectSendInterval = EditorGUILayout.FloatField(new GUIContent("Gaze Object Send Interval", "How many seconds of gaze data are batched together when reporting CognitiveVR_GazeObject look durations"), prefs.GazeObjectSendInterval);
            prefs.GazeObjectSendInterval = Mathf.Max(prefs.GazeObjectSendInterval, 1);

            prefs.TrackArmLengthSamples = EditorGUILayout.IntField(new GUIContent("Arm Length Samples", "Number of samples taken. The max is assumed to be maximum arm length"), prefs.TrackArmLengthSamples);
            prefs.TrackHMDHeightSamples = EditorGUILayout.IntField(new GUIContent("HMD Height Samples", "Number of samples taken. The average is assumed to be the player's eye height"), prefs.TrackHMDHeightSamples);
            prefs.TrackArmLengthSamples = Mathf.Clamp(prefs.TrackArmLengthSamples, 1, 100);
            prefs.TrackHMDHeightSamples = Mathf.Clamp(prefs.TrackHMDHeightSamples, 1, 100);




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
            
            bool o2 = prefs.PlayerDataType == 1;
            bool b2 = GUILayout.Toggle(prefs.PlayerDataType == 1, "", EditorStyles.radioButton);
            if (b2 != o2)
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

            prefs.SendDataOnLevelLoad = EditorGUILayout.Toggle(new GUIContent("Send Data on Level Load", "Send all snapshots on Level Loaded"), prefs.SendDataOnLevelLoad);
            prefs.SendDataOnQuit = EditorGUILayout.Toggle(new GUIContent("Send Data on Quit", "Sends all snapshots on Application OnQuit\nNot reliable on Mobile"), prefs.SendDataOnQuit);
            prefs.DebugWriteToFile = EditorGUILayout.Toggle(new GUIContent("DEBUG - Write snapshots to file", "Write snapshots to file AND upload to SceneExplorer"), prefs.DebugWriteToFile);
            prefs.SendDataOnHotkey = EditorGUILayout.Toggle(new GUIContent("DEBUG - Send Data on Hotkey", "Press a hotkey to send data"), prefs.SendDataOnHotkey);
            //prefs.SendDataOnHMDRemove = EditorGUILayout.Toggle(new GUIContent("Send data on HMD remove", "Send all snapshots on HMD remove event"), prefs.SendDataOnHMDRemove);

            EditorGUI.BeginDisabledGroup(!prefs.SendDataOnHotkey);

            if (remapHotkey)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                style.normal.textColor = new Color(0.5f, 1.0f, 0.5f, 1.0f);

                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Hotkey", "Shift, Ctrl and Alt modifier keys are not allowed"), GUILayout.Width(125));
                GUI.color = Color.green;
                GUILayout.Button("Any Key",GUILayout.Width(70));
                GUI.color = Color.white;

                string displayKey = (prefs.HotkeyCtrl ? "Ctrl + " : "") + (prefs.HotkeyShift ? "Shift + " : "") + (prefs.HotkeyAlt ? "Alt + " : "") + prefs.SendDataHotkey.ToString();
                GUILayout.Label(displayKey);
                GUILayout.EndHorizontal();
                Event e = Event.current;
                
                //shift, ctrl, alt
                if (e.type == EventType.keyDown && e.keyCode != KeyCode.None && e.keyCode != KeyCode.LeftShift && e.keyCode != KeyCode.RightShift && e.keyCode != KeyCode.LeftControl && e.keyCode != KeyCode.RightControl && e.keyCode != KeyCode.LeftAlt && e.keyCode != KeyCode.RightAlt)
                {
                    prefs.HotkeyAlt = e.alt;
                    prefs.HotkeyShift = e.shift;
                    prefs.HotkeyCtrl = e.control;
                    prefs.SendDataHotkey = e.keyCode;
                    remapHotkey = false;
                    //this is kind of a hack, but it works
                    GetWindow<CognitiveVR_EditorPrefs>().Repaint();
                    GetWindow<CognitiveVR_EditorPrefs>().Close();
                }
            }
            else
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.wordWrap = true;
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent("Hotkey", "Shift, Ctrl and Alt modifier keys are not allowed"), GUILayout.Width(125));
                if (GUILayout.Button("Remap", GUILayout.Width(70)))
                {
                    remapHotkey = true;
                }
                string displayKey = (prefs.HotkeyCtrl ? "Ctrl + " : "") + (prefs.HotkeyShift ? "Shift + " : "") + (prefs.HotkeyAlt ? "Alt + " : "") + prefs.SendDataHotkey.ToString();
                GUILayout.Label(displayKey);
                GUILayout.EndHorizontal();
            }

            EditorGUI.EndDisabledGroup();

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

            prefs.ExportStaticOnly = EditorGUILayout.Toggle(new GUIContent("Export Static Geo Only", "Only export meshes marked as static. Dynamic objects (such as vehicles, doors, etc) will not be exported"), prefs.ExportStaticOnly);
            prefs.MinExportGeoSize = EditorGUILayout.FloatField(new GUIContent("Minimum export size", "Ignore exporting meshes that are below this size(pebbles, grass,etc)"), prefs.MinExportGeoSize);
            appendName = EditorGUILayout.TextField(new GUIContent("Append to File Name", "This could be a level's number and version"), appendName);
            prefs.ExplorerMinimumFaceCount = EditorGUILayout.IntField(new GUIContent("Minimum Face Count", "Ignore decimating objects with fewer faces than this value"), prefs.ExplorerMinimumFaceCount);
            prefs.ExplorerMaximumFaceCount = EditorGUILayout.IntField(new GUIContent("Maximum Face Count", "Objects with this many faces will be decimated to 10% of their original face count"), prefs.ExplorerMaximumFaceCount);

            if (prefs.ExplorerMinimumFaceCount < 0) { prefs.ExplorerMinimumFaceCount = 0; }
            if (prefs.ExplorerMaximumFaceCount < 1) { prefs.ExplorerMaximumFaceCount = 1; }
            if (prefs.ExplorerMinimumFaceCount > prefs.ExplorerMaximumFaceCount) { prefs.ExplorerMinimumFaceCount = prefs.ExplorerMaximumFaceCount; }

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
                ExportScene(true);
            }

            if (GUILayout.Button(new GUIContent("Export Scene SKIP TEXTURES", "Exports only the scene geometry to Blender and reduces polygons")))
            {
                ExportScene(false);
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
            //TODO get this scene name
            //use that to figure out which directory
            //get all files in teh directory
            //remove scenename.obj and scenename.mtl
            //http POST to sceneexplorer.com/upload
            //get sceneID back when upload complete
        }

        public static void ExportScene(bool includeTextures)
        {
            string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + appendName;

            CognitiveVR_SceneExplorerExporter.ExportWholeSelectionToSingle(fullName, includeTextures);


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
            ProcessInfo.Arguments = "-P " + decimateScriptPath + " " + objPath + " " + prefs.ExplorerMinimumFaceCount + " " + prefs.ExplorerMaximumFaceCount + " " + fullName;

            //KNOWN BUG - changing scene while blender is decimating the level will break the file that should be uploaded
            Process.Start(ProcessInfo);
            BlenderRequest = true;
            HasOpenedBlender = false;
            EditorApplication.update += UpdateProcess;
        }

        public static bool FoldoutButton(string title, bool showing)
        {
            string fullTitle = showing ? "Hide ":"Show " ;
            fullTitle += title + " Options";
            GUILayout.Space(4);
            return GUILayout.Button(fullTitle, EditorStyles.toolbarButton);
        }

        public static CognitiveVR_Preferences GetPreferences()
        {
            CognitiveVR_Preferences asset = AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
                AssetDatabase.CreateAsset(asset, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            }
            return asset;
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
    }
}