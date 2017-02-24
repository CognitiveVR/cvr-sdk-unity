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

        //[MenuItem("Window/cognitiveVR/Scene Export Window", priority = 3)]
        public static void Init()
        {
            CognitiveVR_SceneExportWindow window = (CognitiveVR_SceneExportWindow)GetWindow(typeof(CognitiveVR_SceneExportWindow), true, "cognitiveVR Scene Export");
            window.minSize = new Vector2(500, 500);
            window.Show();
        }

        bool exportOptionsFoldout = false;
        static CognitiveVR_Preferences.SceneSettings currentSceneSettings;

        string searchString = "";

        int sceneWidth = 140;
        int keyWidth = 400;


        bool loadedScenes = false;
        List<string> sceneNames = new List<string>();

        void AddAllScenes()
        {
            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                sceneNames.Add(name);
            }
        }

        

        void OnGUI()
        {
            if (!loadedScenes)
            {
                UpdateSceneNames();
                AddAllScenes();
                loadedScenes = true;
            }

            GUI.skin.label.richText = true;
            GUI.skin.box.richText = true;
            GUI.skin.textField.richText = true;
            

            prefs = CognitiveVR_Settings.GetPreferences();

            //=========================
            //scene select
            //=========================

            GUILayout.Label("Scene Export Manager", CognitiveVR_Settings.HeaderStyle);

            GUILayout.BeginHorizontal();

            searchString = CognitiveVR_Settings.GhostTextField("Search scenes", "", searchString);

            

            GUILayout.EndHorizontal();

            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });

            GUILayout.BeginHorizontal();

            GUILayout.Label("<size=8>Scene Name</size>", GUILayout.Width(sceneWidth));
            GUILayout.Label(new GUIContent("<size=8>Last Upload</size>", "The most recent date the scene was successfully uploaded"), GUILayout.Width(keyWidth));

            GUILayout.EndHorizontal();

            canvasPos = GUILayout.BeginScrollView(canvasPos, false,true,GUILayout.Height(140));
            GUI.Box(new Rect(-10, -10, position.width * 10, 10000), "");

            foreach (var v in prefs.sceneSettings)
            {
                if (!string.IsNullOrEmpty(searchString) && !v.SceneName.ToLower().Contains(searchString.ToLower())) { continue; }
                DisplaySceneSettings(v);
            }

            GUILayout.EndScrollView();


            //GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);


            currentSceneSettings = CognitiveVR_Preferences.Instance.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);

            if (currentSceneSettings == null)
            {
                currentSceneSettings = new CognitiveVR_Preferences.SceneSettings("Not Saved", "");
            }


            GUILayout.BeginHorizontal();

            bool sceneIsSaved = !string.IsNullOrEmpty(currentSceneSettings.ScenePath);


            
            GUI.skin.textField.alignment = TextAnchor.MiddleCenter;

            if (EditorGUIUtility.isProSkin)
            {
                if (sceneIsSaved)
                {
                    GUIStyle gs = new GUIStyle(EditorStyles.textField);
                    //GUI.Box(selectedRect, "", gs);

                    GUI.backgroundColor = CognitiveVR_Settings.GreenButton;
                    GUILayout.Box("<size=14><b>Current Scene: " + currentSceneSettings.SceneName + "</b></size>", gs);
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUIStyle gs = new GUIStyle(EditorStyles.textField);
                    //GUI.Box(selectedRect, "", gs);

                    GUI.backgroundColor = CognitiveVR_Settings.OrangeButtonPro;
                    GUILayout.Box("<size=14><b>Current Scene: " + currentSceneSettings.SceneName + "</b></size>", gs);
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                if (sceneIsSaved)
                {
                    GUIStyle gs = new GUIStyle(EditorStyles.textField);
                    GUI.backgroundColor = CognitiveVR_Settings.GreenButton;
                    GUILayout.Box("<size=14><b>Current Scene: " + currentSceneSettings.SceneName + "</b></size>", gs);
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    GUIStyle gs = new GUIStyle(EditorStyles.textField);
                    GUI.backgroundColor = CognitiveVR_Settings.OrangeButton;
                    GUILayout.Box("<size=14><b>Current Scene: " + currentSceneSettings.SceneName + "</b></size>", gs);
                    GUI.backgroundColor = Color.white;
                }
            }

            GUI.skin.textField.alignment = TextAnchor.MiddleLeft;

            //GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (!sceneIsSaved)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Refresh Scene List"))
                {
                    UpdateSceneNames();
                    AddAllScenes();
                }
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
            }

            EditorGUI.BeginDisabledGroup(!sceneIsSaved);

            //compression amount

            GUILayout.Space(10);

            if (!sceneIsSaved) { return; }

            GUILayout.Label("Scene Export Quality",CognitiveVR_Settings.HeaderStyle);

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

            EditorGUIUtility.labelWidth = 200;

            exportOptionsFoldout = EditorGUI.Foldout(EditorGUILayout.GetControlRect(), exportOptionsFoldout, "Advanced Options", true);

            EditorGUI.indentLevel++;
            if (exportOptionsFoldout)
            {
                prefs.ExportSettings.ExportStaticOnly = EditorGUILayout.Toggle(new GUIContent("Export Static Meshes Only", "Only export meshes marked as static. Dynamic objects (such as vehicles, doors, etc) will not be exported"), prefs.ExportSettings.ExportStaticOnly);
                prefs.ExportSettings.MinExportGeoSize = EditorGUILayout.FloatField(new GUIContent("Minimum export size", "Ignore exporting meshes that are below this size(pebbles, grass,etc)"), prefs.ExportSettings.MinExportGeoSize);
                prefs.ExportSettings.ExplorerMinimumFaceCount = EditorGUILayout.IntField(new GUIContent("Minimum Face Count", "Ignore decimating objects with fewer faces than this value"), prefs.ExportSettings.ExplorerMinimumFaceCount);
                prefs.ExportSettings.ExplorerMaximumFaceCount = EditorGUILayout.IntField(new GUIContent("Maximum Face Count", "Objects with this many faces will be decimated to 10% of their original face count"), prefs.ExportSettings.ExplorerMaximumFaceCount);

                GUIContent[] textureQualityNames = new GUIContent[] { new GUIContent("Full"), new GUIContent("Half"), new GUIContent("Quarter"), new GUIContent("Eighth"), new GUIContent("Sixteenth") };
                int[] textureQualities = new int[] { 1, 2, 4, 8, 16 };
                prefs.ExportSettings.TextureQuality = EditorGUILayout.IntPopup(new GUIContent("Texture Export Quality", "Reduce textures when uploading to scene explorer"), prefs.ExportSettings.TextureQuality, textureQualityNames, textureQualities);

                if (prefs.ExportSettings.MinExportGeoSize < 0) { prefs.ExportSettings.MinExportGeoSize = 0; }
                if (prefs.ExportSettings.ExplorerMinimumFaceCount < 0) { prefs.ExportSettings.ExplorerMinimumFaceCount = 0; }
                if (prefs.ExportSettings.ExplorerMaximumFaceCount < 1) { prefs.ExportSettings.ExplorerMaximumFaceCount = 1; }
                if (prefs.ExportSettings.ExplorerMinimumFaceCount > prefs.ExportSettings.ExplorerMaximumFaceCount) { prefs.ExportSettings.ExplorerMinimumFaceCount = prefs.ExportSettings.ExplorerMaximumFaceCount; }
            }
            EditorGUI.indentLevel--;

            GUILayout.Space(20);


            //Export Buttons

            if (string.IsNullOrEmpty(prefs.SavedBlenderPath))
            {
                FindBlender();
            }

            bool validBlenderPath = prefs.SavedBlenderPath.ToLower().EndsWith("blender.exe");

            GUILayout.BeginHorizontal();

            GUIContent blenderButtonContent = new GUIContent("Select Blender.exe");
            
            if (validBlenderPath)
                blenderButtonContent.tooltip = prefs.SavedBlenderPath;

            CognitiveVR_Settings.UserStartupBox("1", validBlenderPath);
            

            if (GUILayout.Button(blenderButtonContent))
            {
                prefs.SavedBlenderPath = EditorUtility.OpenFilePanel("Select Blender.exe", string.IsNullOrEmpty(prefs.SavedBlenderPath) ? "c:\\" : prefs.SavedBlenderPath, "");
            }
            GUILayout.EndHorizontal();

#if UNITY_EDITOR_OSX
            EditorGUILayout.HelpBox("Exporting scenes has not been tested on Mac!", MessageType.Warning);
#endif

            //this line can be simplified. editor disable groups stack nicely
            EditorGUI.BeginDisabledGroup(!validBlenderPath || string.IsNullOrEmpty(prefs.CustomerID) || string.IsNullOrEmpty(currentSceneSettings.ScenePath));

            string exportButtonText = "Bake Scene \"" + currentSceneSettings.SceneName +"\"";

            GUIContent exportContent = new GUIContent(exportButtonText, "Exports the scene to Blender and reduces polygons. This also exports required textures at a reduced resolution");

            if (string.IsNullOrEmpty(prefs.CustomerID))
            {
                exportContent.tooltip = "You must have a valid CustomerID to export a scene. Please register at cogntivevr.co and follow the setup instructions at docs.cognitivevr.io";
            }


            var uploadButtonContent = new GUIContent("Upload baked \"" + currentSceneSettings.SceneName + "\" scene files to Dashboard");
            if (string.IsNullOrEmpty(prefs.CustomerID))
            {
                uploadButtonContent.tooltip = "You must have a valid CustomerID to upload a scene. Please register at cogntivevr.co and follow the setup instructions at docs.cognitivevr.io";
            }
            else
            {
                uploadButtonContent.tooltip = "Upload files in " + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            }

            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            var exists = Directory.Exists(sceneExportDirectory);



            

            GUILayout.BeginHorizontal();

            CognitiveVR_Settings.UserStartupBox("2", exists && Directory.GetFiles(sceneExportDirectory).Length > 0);

            if (GUILayout.Button(exportContent))
            {
                CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality,prefs.CompanyProductName);
            }
            GUILayout.EndHorizontal();
            
            if (!exists)
            {
                uploadButtonContent.tooltip = "Directory doesn't exist! " + sceneExportDirectory;
            }
            else if (Directory.GetFiles(sceneExportDirectory).Length <= 0)
            {
                uploadButtonContent.tooltip = "Directory doesn't contain any files " + sceneExportDirectory;
            }

            EditorGUI.BeginDisabledGroup(!exists || Directory.GetFiles(sceneExportDirectory).Length <= 0);

            System.DateTime revisionDate = System.DateTime.MinValue;
            revisionDate = DateTime.FromBinary(currentSceneSettings.LastRevision);

            GUILayout.BeginHorizontal();

            CognitiveVR_Settings.UserStartupBox("3", revisionDate.Year > 1000);

            if (GUILayout.Button(uploadButtonContent))
            {
                UploadDecimatedScene(true);
            }
            GUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();

            EditorGUI.EndDisabledGroup();

            GUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (revisionDate.Year > 1000)
            {
                GUI.color = CognitiveVR_Settings.GreenButton;
                if (GUILayout.Button("Save and Close", GUILayout.Height(40), GUILayout.MaxWidth(300)))
                {
                    Close();
                }
                GUI.color = Color.white;
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                if (GUILayout.Button("Save and Close", GUILayout.Height(40), GUILayout.MaxWidth(300)))
                {
                    Close();
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (GUI.changed)
            {
                EditorUtility.SetDirty(prefs);
            }
        }

        void DisplaySceneSettings(CognitiveVR_Preferences.SceneSettings settings)
        {
            bool isCurrentScene = false;
            if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path == settings.ScenePath)
            {
                isCurrentScene = true;
            }

            if (isCurrentScene)
            {
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.color = new Color(0, 10, 0,1);

                    GUIStyle gs = new GUIStyle(EditorStyles.textField);
                    GUI.Box(selectedRect, "",gs);
                }
                else
                {
                    GUI.color = CognitiveVR_Settings.GreenButton;
                    GUI.Box(selectedRect, "");
                }
            }
            GUI.color = Color.white;
            

            GUILayout.BeginHorizontal();

            if (EditorGUIUtility.isProSkin)
            {
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
            }
            else
            {
                if (isCurrentScene)
                {
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
                }
                else
                {
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
                }
            }

            EditorGUI.BeginDisabledGroup(!KeyIsValid(settings.SceneId));
            GUIContent sceneExplorerLink = new GUIContent("View on Dashboard");
            if (KeyIsValid(settings.SceneId))
            {
                sceneExplorerLink.tooltip = "https://sceneexplorer.com/scene/" + settings.SceneId;
            }

            if (GUILayout.Button(sceneExplorerLink))
            {
                Application.OpenURL("https://sceneexplorer.com/scene/" + settings.SceneId);
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Select"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(settings.ScenePath);
            }
            GUILayout.EndHorizontal();

            if (isCurrentScene)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    selectedRect = GUILayoutUtility.GetLastRect();
                    selectedRect.x -= 2;
                    selectedRect.y -= 1;
                    selectedRect.width += 4;
                    selectedRect.height += 4;

                    Repaint();
                }
            }
        }

        Rect selectedRect;

        public static void ExportScene(bool includeTextures, bool staticGeometry, float minSize, int textureDivisor, string customerID)
        {
            if (blenderProcess != null)
            {
                Debug.LogError("Currently decimating a scene. Please wait until this is finished!");
                return;
            }

            if (UploadSceneSettings != null)
            {
                Debug.LogError("Currently uploading a scene. Please wait until this is finished!");
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
            string jsonSettingsContents = "{ \"scale\":1, \"customerId\":\"" + customerID + "\",\"sceneName\":\""+ currentSceneSettings.SceneName+"\"}";
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
            UploadSceneSettings = currentSceneSettings;


            blenderStartTime = (float)EditorApplication.timeSinceStartup;
        }

        static float blenderStartTime = 0;
        static float currentBlenderTime = 0;
        static float maxBlenderTime = 240;

        static bool BlenderRequest;
        static bool HasOpenedBlender;
        static Process blenderProcess;

        static void UpdateProcess()
        {
            currentBlenderTime = (float)(EditorApplication.timeSinceStartup - blenderStartTime);
            EditorUtility.DisplayProgressBar("Blender Decimate", "Reducing the polygons and scene complexity using Blender", currentBlenderTime / maxBlenderTime);

            //could probably clean up some of this
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

        static void UploadDecimatedScene(bool fetchNewCurrentScene = false)
        {
            if (fetchNewCurrentScene)
            {
                UploadSceneSettings = CognitiveVR_Preferences.Instance.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            }
            if (UploadSceneSettings == null) { return; }

            if (sceneUploadWWW != null)
            {
                Debug.LogError("scene upload WWW is not null. please wait until your scene has finished uploading before uploading another!");
            }

            if (EditorUtility.DisplayDialog("Upload Scene","Do you want to upload \""+ UploadSceneSettings.SceneName+"\" to your Dashboard?", "Yes", "No"))
            {
                string sceneName = UploadSceneSettings.SceneName;

                string fullName = sceneName + appendName;

                var filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName + Path.DirectorySeparatorChar);

                string fileList = "Upload Files:\n";

                WWWForm wwwForm = new WWWForm();
                foreach (var f in filePaths)
                {
                    if (f.Contains(".obj") && !f.Contains("_decimated.obj")){ continue; }
                    if (f.Contains(".mtl") && !f.Contains("_decimated.mtl")){ continue; }

                    fileList += f + "\n";

                    var data = File.ReadAllBytes(f);
                    wwwForm.AddBinaryData("file", data, Path.GetFileName(f));
                }
                Debug.Log(fileList);
                sceneUploadWWW = new WWW("https://sceneexplorer.com/api/scenes/", wwwForm);

                EditorApplication.update += UpdateUploadData;
            }
            else
            {
                UploadSceneSettings = null;
            }
        }

        static CognitiveVR_Preferences.SceneSettings UploadSceneSettings;

        static WWW sceneUploadWWW;

        static void UpdateUploadData()
        {
            if (sceneUploadWWW == null)
            {
                EditorApplication.update -= UpdateUploadData;
                EditorUtility.ClearProgressBar();
                return;
            }

            if (EditorUtility.DisplayCancelableProgressBar("Uploading","Uploading scene data to sceneExplorer.com",sceneUploadWWW.uploadProgress))
            {
                EditorApplication.update -= UpdateUploadData;
                EditorUtility.ClearProgressBar();
                sceneUploadWWW.Dispose();
                sceneUploadWWW = null;
                UploadSceneSettings = null;
                Debug.Log("Upload canceled!");
                return;
            }

            if (!sceneUploadWWW.isDone) { return; }

            EditorUtility.ClearProgressBar();
            EditorApplication.update -= UpdateUploadData;

            //response
            if (!string.IsNullOrEmpty(sceneUploadWWW.error))
            {
                Debug.LogError("Scene Upload Error:" + sceneUploadWWW.error);

                sceneUploadWWW.Dispose();
                sceneUploadWWW = null;
                UploadSceneSettings = null;
                return;
            }

            string responseText = sceneUploadWWW.text;
            UploadSceneSettings.SceneId = responseText.Replace("\"","");

            UploadSceneSettings.LastRevision = System.DateTime.UtcNow.ToBinary();
            sceneUploadWWW.Dispose();
            sceneUploadWWW = null;

            GUI.FocusControl("NULL");

            AssetDatabase.SaveAssets();

            if (EditorUtility.DisplayDialog("Upload Complete", UploadSceneSettings.SceneName + " was successfully uploaded! Do you want to open your scene in SceneExplorer?","Open on SceneExplorer","Close"))
            {
                Application.OpenURL("https://sceneexplorer.com/scene/" + UploadSceneSettings.SceneId);
            }

            UploadSceneSettings = null;
        }

        static int uploadRequests = 0;
        static WWW dynamicUploadWWW;


        public static void UploadDynamicObjects()
        {
            string fileList = "Upload Files:\n";

            var settings = CognitiveVR_Preferences.Instance.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            if (settings == null)
            {
                Debug.Log("settings are null " + UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
                string s = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
                if (string.IsNullOrEmpty(s))
                {
                    s = "Unknown Scene";
                }
                EditorUtility.DisplayDialog("Upload Failed", "Could not find the Scene Settings for \"" + s + "\". Are you sure you've saved, exported and uploaded this scene to SceneExplorer?", "Ok");
                return;
            }

            string sceneid = settings.SceneId;

            if (string.IsNullOrEmpty(sceneid))
            {
                EditorUtility.DisplayDialog("Upload Failed", "Could not find the SceneId for \"" + settings.SceneName + "\". Are you sure you've exported and uploaded this scene to SceneExplorer?","Ok");
                return;
            }

            string uploadUrl = "https://sceneexplorer.com/api/objects/" + sceneid + "/";

            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";
            var subdirectories = Directory.GetDirectories(path);

            int option = EditorUtility.DisplayDialogComplex("Upload Dynamic Objects", "Do you want to upload " + subdirectories.Length + " Objects to \"" + settings.SceneName + "\" (" + settings.SceneId + ")?", "Ok", "Cancel","Open Directory");
            if (option == 0) { } //ok
            else if (option == 1){return; } //cancel
            else
            {
                //open directory
                System.Diagnostics.Process.Start("explorer.exe", path);
                return;
            }

            foreach (var subdir in subdirectories)
            {
                var filePaths = Directory.GetFiles(subdir);

                WWWForm wwwForm = new WWWForm();
                foreach (var f in filePaths)
                {
                    fileList += f + "\n";

                    var data = File.ReadAllBytes(f);
                    wwwForm.AddBinaryData("file", data, Path.GetFileName(f));
                }

                var dirname = new DirectoryInfo(subdir).Name;

                Debug.Log("upload dynamic object: " + dirname);

                dynamicUploadWWW = new WWW(uploadUrl + dirname, wwwForm);
                uploadRequests++;
            }

            if (uploadRequests > 0)
            {
                EditorApplication.update += UpdateUploadDynamics;
            }
        }

        static void UpdateUploadDynamics()
        {
            if (dynamicUploadWWW == null)
            {
                EditorApplication.update -= UpdateUploadDynamics;
                return;
            }

            if (!dynamicUploadWWW.isDone) { return; }

            if (!string.IsNullOrEmpty(dynamicUploadWWW.error))
            {
                Debug.LogError(dynamicUploadWWW.error);
            }
            if (!string.IsNullOrEmpty(dynamicUploadWWW.text))
            {
                Debug.Log("dynamic object upload reponse text " + dynamicUploadWWW.text);
            }
            else
            {
                Debug.Log("dynamic object upload complete");
            }

            EditorApplication.update -= UpdateUploadDynamics;
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

        private static void UpdateSceneNames()
        {
            //save these to a temp list
            List<CognitiveVR_Preferences.SceneSettings> oldSettings = new List<CognitiveVR_Preferences.SceneSettings>();
            foreach (var v in CognitiveVR_Preferences.Instance.sceneSettings)
            {
                oldSettings.Add(v);
            }


            //clear then rebuild the list in preferences
            CognitiveVR_Preferences.Instance.sceneSettings.Clear();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                CognitiveVR_Preferences.Instance.sceneSettings.Add(new CognitiveVR_Preferences.SceneSettings(name, path));
            }

            //match up dictionary keys from temp list
            foreach (var oldSetting in oldSettings)
            {
                foreach (var newSetting in CognitiveVR_Preferences.Instance.sceneSettings)
                {
                    if (newSetting.SceneName == oldSetting.SceneName)
                    {
                        newSetting.SceneId = oldSetting.SceneId;
                        newSetting.LastRevision = oldSetting.LastRevision;
                        newSetting.SceneName = oldSetting.SceneName;
                        newSetting.ScenePath = oldSetting.ScenePath;
                    }
                }
            }
        }

        #endregion
    }
}