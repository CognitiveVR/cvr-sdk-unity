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

        string searchString = "";

        int toggleWidth = 10;
        int sceneWidth = 140;
        int keyWidth = 400;

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

            GUILayout.BeginHorizontal();

            hideNonBuildScenes = GUILayout.Toggle(hideNonBuildScenes, "Only show scenes in Build Settings");
            //searchString = EditorGUILayout.TextField("Search", searchString);

            searchString = GhostTextField("Search scenes", "", searchString);



            GUILayout.EndHorizontal();

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            canvasPos = GUILayout.BeginScrollView(canvasPos, false,true,GUILayout.Height(140));

            foreach (var v in prefs.SceneKeySettings)
            {
                if (!string.IsNullOrEmpty(searchString) && !v.SceneName.ToLower().Contains(searchString.ToLower())) { continue; }
                DisplaySceneKeySettings(v);
            }

            GUILayout.EndScrollView();


            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);


            currentSceneSettings = CognitiveVR_Preferences.Instance.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<size=14><b>Current Scene: "+currentSceneSettings.SceneName + "</b></size>");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            

            /*string selectedSceneName = "invalid scene name";
            if (sceneIndex < sceneNames.Count)
            {
                selectedSceneName = sceneNames[sceneIndex];
            }*/
            //GUILayout.Label(selectedSceneName);

            //when should scenes and keys get added to this?
            //currentSceneSettings = CognitiveVR_Settings.GetPreferences().FindScene(selectedSceneName);

            if (currentSceneSettings == null)
            {
                //reload scenes if this one isn't currently found. likely was created while this window was open
                //ReadNames();
            }

            /*if (GUILayout.Button("Open Scene"))
            {
                //UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                //UnityEditor.SceneManagement.EditorSceneManager.OpenScene(currentSceneSettings.ScenePath);
                //var prefs = CognitiveVR_Settings.GetPreferences();
                //CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality);
            }*/

            System.DateTime revisionDate = System.DateTime.MinValue;

            if (currentSceneSettings != null)
            {
                revisionDate = DateTime.FromBinary(currentSceneSettings.LastRevision);
            }

            //revision date

            //GUILayout.Space(10);
            //GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            //GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (revisionDate.Year < 1000)
            {
                GUILayout.Label("Last Scene Revision: Never");
            }
            else
            {
                GUILayout.Label("Last Scene Revision: " + revisionDate.ToShortDateString());
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(currentSceneSettings.SceneKey))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                GUILayout.BeginHorizontal();
                EditorGUILayout.TextField("SceneID","a12345b6-78c9-01d2-3456-78e9f0ghi123", style);

                EditorGUI.BeginDisabledGroup(true);
                GUIContent sceneExplorerLink = new GUIContent("SceneExplorer...");
                if (GUILayout.Button(sceneExplorerLink))
                {
                    Application.OpenURL("https://sceneexplorer.com/scene/" + currentSceneSettings.SceneKey);
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                currentSceneSettings.SceneKey = EditorGUILayout.TextField("SceneID",currentSceneSettings.SceneKey, GUILayout.Width(keyWidth));

                //remove sceneexplorer.com/scene from sceneKey
                bool validKey = KeyIsValid(currentSceneSettings.SceneKey);

                if (!validKey)
                {
                    if (currentSceneSettings.SceneKey.Contains("http://sceneexplorer.com/scene/"))
                    {
                        currentSceneSettings.SceneKey = currentSceneSettings.SceneKey.Replace("http://sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                    if (currentSceneSettings.SceneKey.Contains("https://sceneexplorer.com/scene/"))
                    {
                        currentSceneSettings.SceneKey = currentSceneSettings.SceneKey.Replace("https://sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                    else if (currentSceneSettings.SceneKey.Contains("sceneexplorer.com/scene/"))
                    {
                        currentSceneSettings.SceneKey = currentSceneSettings.SceneKey.Replace("sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                }

                EditorGUI.BeginDisabledGroup(!validKey);

                GUIContent sceneExplorerLink = new GUIContent("SceneExplorer...");
                if (KeyIsValid(currentSceneSettings.SceneKey))
                {
                    sceneExplorerLink.tooltip = "https://sceneexplorer.com/scene/" + currentSceneSettings.SceneKey;
                }

                if (GUILayout.Button(sceneExplorerLink))
                {
                    Application.OpenURL("https://sceneexplorer.com/scene/" + currentSceneSettings.SceneKey);
                }
                EditorGUI.EndDisabledGroup();
                GUILayout.EndHorizontal();
            }


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

            //GUILayout.Space(10);
            //GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(20);

            if (string.IsNullOrEmpty(prefs.SavedBlenderPath))
            {
                FindBlender();
            }

            bool validBlenderPath = prefs.SavedBlenderPath.ToLower().EndsWith("blender.exe");

            if (GUILayout.Button("Select Blender.exe"))
            {
                prefs.SavedBlenderPath = EditorUtility.OpenFilePanel("Select Blender.exe", string.IsNullOrEmpty(prefs.SavedBlenderPath) ? "c:\\" : prefs.SavedBlenderPath, "");
            }

#if UNITY_EDITOR_OSX
            EditorGUILayout.HelpBox("Exporting scenes is not available on Mac at this time", MessageType.Warning);
            EditorGUI.BeginDisabledGroup(true);

#endif

            //appendName = EditorGUILayout.TextField(new GUIContent("Append to File Name", "This could be a level's number and version"), appendName);

            EditorGUI.BeginDisabledGroup(!validBlenderPath || string.IsNullOrEmpty(prefs.CustomerID));

            string exportButtonText = "Export Scene \"" + currentSceneSettings.SceneName +"\"";
            /*if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != currentSceneSettings.ScenePath)
            {
                exportButtonText = "Change Scene and Export";
            }*/

            GUIContent exportContent = new GUIContent(exportButtonText, "Exports the scene to Blender and reduces polygons. This also exports required textures at a reduced resolution");

            if (string.IsNullOrEmpty(prefs.CustomerID))
            {
                exportContent.tooltip = "You must have a valid CustomerID to export a scene. Please register at cogntivevr.co and follow the setup instructions at docs.cognitivevr.io";
            }

            if (GUILayout.Button(exportContent))
            {
                //ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize,prefs.ExportSettings.TextureQuality);

                //if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path != currentSceneSettings.ScenePath)
                //{
                    //UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                    //UnityEditor.SceneManagement.EditorSceneManager.OpenScene(currentSceneSettings.ScenePath);
                //}
                //var prefs = CognitiveVR_Settings.GetPreferences();
                CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality,prefs.CompanyProductName);

                //UnityEditor.SceneManagement.EditorSceneManager.OpenScene(currentSceneSettings.ScenePath);
                //CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality);
            }

            if (GUILayout.Button(new GUIContent("Upload Scene","Upload scene files from default scene folder")))
            {
                UploadDecimatedScene();
            }

            EditorGUI.EndDisabledGroup();

#if UNITY_EDITOR_OSX
            EditorGUI.EndDisabledGroup();
#endif

            /*if (GUILayout.Button(new GUIContent("Manage Scene IDs", "Open window to set which tracked player data is uploaded to your scenes")))
            {
                CognitiveVR_SceneKeyConfigurationWindow.Init();
            }*/

            if (GUI.changed)
            {
                EditorUtility.SetDirty(prefs);
            }
        }

        bool IsSceneInBuildSettings(string scenePath)
        {
            for (int i = 0; i< EditorBuildSettings.scenes.Length; i++)
            {
                if (EditorBuildSettings.scenes[i].path == scenePath){ return true; }
            }
            return false;
        }

        void DisplaySceneKeySettings(CognitiveVR_Preferences.SceneKeySetting settings)
        {
            bool inBuildSettings = IsSceneInBuildSettings(settings.ScenePath);
            if (hideNonBuildScenes && !inBuildSettings)
            {
                return;
            }

            GUILayout.BeginHorizontal();

            //settings.Track = GUILayout.Toggle(settings.Track, "", GUILayout.Width(toggleWidth));
            EditorGUI.BeginDisabledGroup(true);
            GUIContent buildScene = new GUIContent("");
            

            if (inBuildSettings)
            { buildScene.tooltip = "In Build Settings"; }
            else
            { buildScene.tooltip = "NOT in Build Settings"; }

            GUILayout.Toggle(inBuildSettings, buildScene, GUILayout.Width(toggleWidth));
            EditorGUI.EndDisabledGroup();

            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == settings.ScenePath)
            {
                GUI.color = Color.green;
            }

            GUILayout.Label(settings.SceneName, GUILayout.Width(sceneWidth));

            DateTime dt = DateTime.FromBinary(settings.LastRevision);
            if (dt.Year < 1000)
            {
                GUILayout.Label("Never", GUILayout.Width(sceneWidth));
            }
            else
            {
                string dtString = dt.ToShortDateString();
                GUILayout.Label(dtString, GUILayout.Width(sceneWidth));
            }

            GUI.color = Color.white;

            string startSceneName = settings.SceneKey;

            /*if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(settings.SceneKey))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                EditorGUILayout.TextField("a12345b6-78c9-01d2-3456-78e9f0ghi123", style);
            }
            else
            {
                settings.SceneKey = EditorGUILayout.TextField(settings.SceneKey, GUILayout.Width(keyWidth));
            }*/

            if (!string.IsNullOrEmpty(settings.SceneKey) && string.IsNullOrEmpty(startSceneName))
            {
                //new key!
                //settings.Track = true;
            }

            EditorGUI.BeginDisabledGroup(!KeyIsValid(settings.SceneKey));
            GUIContent sceneExplorerLink = new GUIContent("SceneExplorer...");
            if (KeyIsValid(settings.SceneKey))
            {
                sceneExplorerLink.tooltip = "https://sceneexplorer.com/scene/" + settings.SceneKey;
            }

            if (GUILayout.Button(sceneExplorerLink))
            {
                Application.OpenURL("https://sceneexplorer.com/scene/" + settings.SceneKey);
            }

            EditorGUI.EndDisabledGroup();
            
            /*if (settings.Track)
            {
                bool validKey = KeyIsValid(settings.SceneKey);

                if (!validKey)
                {
                    if (settings.SceneKey.Contains("http://sceneexplorer.com/scene/"))
                    {
                        settings.SceneKey = settings.SceneKey.Replace("http://sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                    if (settings.SceneKey.Contains("https://sceneexplorer.com/scene/"))
                    {
                        settings.SceneKey = settings.SceneKey.Replace("https://sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }
                    else if (settings.SceneKey.Contains("sceneexplorer.com/scene/"))
                    {
                        settings.SceneKey = settings.SceneKey.Replace("sceneexplorer.com/scene/", "");
                        GUI.FocusControl("NULL");
                    }

                    GUI.color = Color.red;
                    GUILayout.Button(new GUIContent("!", "ID is invalid! Should be format:\na12345b6-78c9-01d2-3456-78e9f0ghi123"), GUILayout.Width(14), GUILayout.Height(14));
                    GUI.color = Color.white;
                }
            }*/

            if (GUILayout.Button("Open Scene"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(settings.ScenePath);
            }

            GUILayout.EndHorizontal();
        }

        public static void ExportScene(bool includeTextures, bool staticGeometry, float minSize, int textureDivisor, string customerID)
        {
            if (blenderProcess != null)
            {
                Debug.LogError("Currently decimating a scene. Please wait until this is finished!");
                return;
            }

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

            //write json settings file
            string jsonSettingsContents = "{ \"scale\":1, \"customerid\":\"" + customerID + "\"}";
            File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

            //System.Diagnostics.Process.Start("http://google.com/search?q=" + "cat pictures");

            decimateScriptPath = decimateScriptPath.Replace(" ", "\" \"");
            objPath = objPath.Replace(" ", "\" \"");
            fullName = fullName.Replace(" ", "\" \"");

            EditorUtility.ClearProgressBar();


            ProcessStartInfo processInfo;

            processInfo = new ProcessStartInfo(prefs.SavedBlenderPath);
            processInfo.UseShellExecute = true;
            processInfo.Arguments = "-P " + decimateScriptPath + " " + objPath + " " + prefs.ExportSettings.ExplorerMinimumFaceCount + " " + prefs.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;

            //changing scene while blender is decimating the level will break the file that will be automatically uploaded
            blenderProcess = Process.Start(processInfo);
            BlenderRequest = true;
            HasOpenedBlender = false;
            EditorApplication.update += UpdateProcess;

            EditorUtility.DisplayProgressBar("Blender Decimate", "Reducing the polygons and scene complexity using Blender", 0.5f);
            {
                /*blenderProcess.Kill();
                Debug.Log("KILL BLENDER PROCESS CANCEL BUTTON WOW");
                blenderProcess = null;
                EditorUtility.ClearProgressBar();*/
            }
            
            //if (EditorUtility.DisplayCancelableProgressBar("Scene Explorer Export", mf[i].name + " Terrain", 0.05f))
        }

        static bool BlenderRequest;
        static bool HasOpenedBlender;
        static Process blenderProcess;
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
                    blenderProcess = null;
                    EditorUtility.ClearProgressBar();
                    UploadDecimatedScene();
                }
            }
        }

        static void UploadDecimatedScene()
        {
            if (currentSceneSettings != null)
                currentSceneSettings.LastRevision = System.DateTime.UtcNow.ToBinary();

            if (EditorUtility.DisplayDialog("Upload Scene","Do you want to automatically upload your decimated scene to SceneExplorer.com?", "Yes", "No"))
            {
                string sceneName = currentSceneSettings.SceneName;

                string fullName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name + appendName;
                var filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + "\\CognitiveVR_SceneExplorerExport\\" + fullName + "\\");

                WWWForm wwwForm = new WWWForm();
                foreach (var f in filePaths)
                {
                    if (f.Contains(".obj") && !f.Contains("_decimated.obj")){ continue; }
                    if (f.Contains(".mtl") && !f.Contains("_decimated.mtl")){ continue; }
                    Debug.Log(f);

                    var data = File.ReadAllBytes(f);
                    wwwForm.AddBinaryData("fileUpload", data, Path.GetFileName(f));
                }
                sceneUploadWWW = new WWW("https://sceneexplorer.com/uploader", wwwForm);

                EditorApplication.update += UpdateUploadData;
                
                //use scenename to figure out which directory
                //get all files in teh directory
                //remove scenename.obj and scenename.mtl
                //http POST to sceneexplorer.com/upload
                //get sceneID back when upload complete
            }
            else //cancel
            {
                Debug.Log("You can manually upload your scene at SceneExplorer.com/upload");
            }
        }

        static WWW sceneUploadWWW;

        static void UpdateUploadData()
        {
            if (sceneUploadWWW == null)
            {
                EditorApplication.update -= UpdateUploadData;
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar("Uploading","Uploading scene data to sceneExplorer.com",sceneUploadWWW.uploadProgress))
            {
                EditorApplication.update -= UpdateUploadData;
                EditorUtility.ClearProgressBar();
                Debug.Log("Upload canceled!");
                return;
            }

            if (!sceneUploadWWW.isDone) { return; }

            Debug.Log("upload complete!");
            EditorUtility.ClearProgressBar();
            EditorApplication.update -= UpdateUploadData;

            //response
            if (!string.IsNullOrEmpty(sceneUploadWWW.error))
            {
                Debug.LogError(sceneUploadWWW.error);
                return;
            }

            string responseText = sceneUploadWWW.text;
            Debug.Log("upload scene response: " + responseText);

            currentSceneSettings.SceneKey = responseText;
        }

        #region Utility

        bool KeyIsValid(string key)
        {
            if (string.IsNullOrEmpty(key)) { return false; }

            //a12345b6-78c9-01d2-3456-78e9f0ghi123

            string pattern = @"[A-Za-z0-9\-+]{" + key.Length + "}";
            bool regexPass = System.Text.RegularExpressions.Regex.IsMatch(key, pattern);
            return regexPass;
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
                        //newSetting.Track = oldSetting.Track;
                        newSetting.LastRevision = oldSetting.LastRevision;
                        newSetting.SceneName = oldSetting.SceneName;
                        newSetting.ScenePath = oldSetting.ScenePath;
                    }
                }
            }
        }

        public static string GhostTextField(string ghostText, string label, string actualText)
        {
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(actualText))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                EditorGUILayout.TextField(label, ghostText, style);
                return "";
            }
            else
            {
                actualText = EditorGUILayout.TextField(label, actualText);//, GUILayout.Width(keyWidth));
            }
            return actualText;
        }

        public static string GhostPasswordField(string ghostText, string label, string actualText)
        {
            if (Event.current.type == EventType.Repaint && string.IsNullOrEmpty(actualText))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);

                EditorGUILayout.TextField(label, ghostText, style);
                return "";
            }
            else
            {
                actualText = EditorGUILayout.PasswordField(label, actualText);//, GUILayout.Width(keyWidth));
            }
            return actualText;
        }

        #endregion
    }
}