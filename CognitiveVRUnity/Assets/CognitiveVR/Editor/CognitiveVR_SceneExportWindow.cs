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
            window.minSize = new Vector2(600, 600);
            window.Show();
            window.cachedScreenshot = null;
        }

        bool exportOptionsFoldout = false;
        static CognitiveVR_Preferences.SceneSettings currentSceneSettings;

        string searchString = "";

        int sceneWidth = 140;
        int keyWidth = 400;


        bool loadedScenes = false;

        List<string> AddAllScenes()
        {
            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");
            List<string> allSceneNames = new List<string>();

            //sceneNames.Clear();

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);
                allSceneNames.Add(name);
                //sceneNames.Add(name);
            }
            return allSceneNames;
        }

        Texture2D refreshTexture;

        void OnGUI()
        {
            if (!loadedScenes)
            {
                UpdateSceneNames();
                loadedScenes = true;
            }

            GUI.skin.label.richText = true;
            GUI.skin.box.richText = true;
            GUI.skin.textField.richText = true;

            //repaint if the cursor is in the scene selection area - make sure buttons and labels appear on the selected scene correctly
            if (Event.current.mousePosition.y<240 && Event.current.mousePosition.x > 0 && Event.current.mousePosition.x < position.width)
            {
                Repaint();
            }

            prefs = CognitiveVR_Settings.GetPreferences();

            //=========================
            //scene select
            //=========================

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Scene Export Manager", CognitiveVR_Settings.HeaderStyle);
            GUILayout.FlexibleSpace();
            
            if (refreshTexture == null)
            {
                refreshTexture = EditorGUIUtility.FindTexture("d_RotateTool On");
            }

            if (GUILayout.Button(refreshTexture, GUILayout.Width(32)))
            {
                UpdateSceneNames();
                currentSceneSettings = null;
                cachedScreenshot = null;
            }
            GUILayout.EndHorizontal();

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


            currentSceneSettings = prefs.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);

            if (currentSceneSettings == null)
            {
                currentSceneSettings = new CognitiveVR_Preferences.SceneSettings("Not Saved", "");
            }


            if (!prefs.IsCustomerIDValid)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("No CustomerID set. Have you logged in?");
                if (GUILayout.Button("Open Account Settings Window"))
                {
                    CognitiveVR_Settings.Init();
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(10);
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
                return;
            }

            EditorGUI.BeginDisabledGroup(!sceneIsSaved);


            //===========================
            //Scene Export Settings
            //===========================

            EditorGUI.BeginDisabledGroup(!prefs.IsCustomerIDValid);

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(position.width - 286));

            GUILayout.Space(10);

            GUILayout.Label("Scene Export Quality",CognitiveVR_Settings.HeaderStyle);




            //export settings
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
                prefs.ExportSettings.DiffuseTextureName = EditorGUILayout.TextField(new GUIContent("Diffuse Texture Name", "The name of the main diffuse texture to export. Generally _MainTex, but possibly something else if you are using a custom shader"), prefs.ExportSettings.DiffuseTextureName);

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


           


            //find blender
            GUILayout.BeginHorizontal();

            if (string.IsNullOrEmpty(prefs.SavedBlenderPath))
            {
                FindBlender();
            }

#if UNITY_EDITOR_WIN
            GUIContent blenderButtonContent = new GUIContent("Select Blender.exe");
#elif UNITY_EDITOR_OSX
            GUIContent blenderButtonContent = new GUIContent("Select Blender.app");
#endif

            if (IsBlenderPathValid())
                blenderButtonContent.tooltip = prefs.SavedBlenderPath;

            CognitiveVR_Settings.UserStartupBox("1", IsBlenderPathValid());
            if (GUILayout.Button(blenderButtonContent))
            {
#if UNITY_EDITOR_WIN
                prefs.SavedBlenderPath = EditorUtility.OpenFilePanel(blenderButtonContent.text, string.IsNullOrEmpty(prefs.SavedBlenderPath) ? "c:\\" : prefs.SavedBlenderPath, "");
#elif UNITY_EDITOR_OSX
                prefs.SavedBlenderPath = EditorUtility.OpenFilePanel(blenderButtonContent.text, string.IsNullOrEmpty(prefs.SavedBlenderPath) ? "/Applications/" : prefs.SavedBlenderPath, "");
#endif
            }
            GUILayout.EndHorizontal();
            







            //export scene
            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(currentSceneSettings.ScenePath)); //you don't need blender to export empty 360 video scenes
            string exportButtonText = "Export Scene \"" + currentSceneSettings.SceneName + "\"";

            GUIContent exportContent = new GUIContent(exportButtonText, "Exports the scene to Blender and reduces polygons. This also exports required textures at a reduced resolution");
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);
            CognitiveVR_Settings.UserStartupBox("2", SceneExportDirExists && Directory.GetFiles(sceneExportDirectory).Length > 0);

            if (GUILayout.Button(exportContent))
            {
                CognitiveVR.CognitiveVR_SceneExportWindow.ExportScene(true, prefs.ExportSettings.ExportStaticOnly, prefs.ExportSettings.MinExportGeoSize, prefs.ExportSettings.TextureQuality,prefs.CompanyProduct,prefs.ExportSettings.DiffuseTextureName);
            }
            GUILayout.EndHorizontal();





            //upload
            GUILayout.BeginHorizontal();
            var uploadButtonContent = new GUIContent("Upload baked \"" + currentSceneSettings.SceneName + "\" scene files to Dashboard");
            if (!prefs.IsCustomerIDValid)
            {
                uploadButtonContent.tooltip = "You must have a valid CustomerID to upload a scene. Please register at cogntivevr.co and follow the setup instructions at docs.cognitivevr.io";
            }
            else
            {
                uploadButtonContent.tooltip = "Upload files in " + Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            }
            if (!SceneExportDirExists)
            {
                uploadButtonContent.tooltip = "Directory doesn't exist! " + sceneExportDirectory;
            }
            else if (Directory.GetFiles(sceneExportDirectory).Length <= 0)
            {
                uploadButtonContent.tooltip = "Directory doesn't contain any files " + sceneExportDirectory;
            }
            System.DateTime revisionDate = System.DateTime.MinValue;
            revisionDate = DateTime.FromBinary(currentSceneSettings.LastRevision);
            CognitiveVR_Settings.UserStartupBox("3", revisionDate.Year > 1000);

            if (GUILayout.Button(uploadButtonContent))
            {
                UploadDecimatedScene(true);
            }
            GUILayout.EndHorizontal();


            GUILayout.EndVertical();

            EditorGUI.EndDisabledGroup(); //scene path is valid
            EditorGUI.EndDisabledGroup(); //valid customer id

            //===========================
            //vertical space
            //===========================
            GUILayout.BeginVertical();
            GUILayout.Label("", new GUILayoutOption[] { GUILayout.ExpandHeight(true), GUILayout.Width(10) });
            GUILayout.EndVertical();

            //===========================
            //scene snapshot
            //===========================
            GUILayout.BeginVertical();


            RenderTexture sceneRT = null;
            Texture2D tex;
            var loadedScreenshotFromFile = LoadScreenshot(currentSceneSettings.SceneName, out tex);

            string title = "";
            if (loadedScreenshotFromFile)
            {
                title = "Screenshot from file";
            }
            else
            {
                title = "Not Saved";

                if (SceneView.lastActiveSceneView != null)
                {
                    System.Reflection.FieldInfo[] fields = typeof(SceneView).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var v in fields)
                    {
                        if (v.Name == "m_SceneTargetTexture")
                        {
                            sceneRT = v.GetValue(SceneView.lastActiveSceneView) as RenderTexture;
                        }
                    }
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(title);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (loadedScreenshotFromFile)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(tex, GUILayout.Width(128), GUILayout.Height(128));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else if (sceneRT == null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box("No Scene View!", GUILayout.Width(128), GUILayout.Height(128));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Box(sceneRT, GUILayout.Width(128), GUILayout.Height(128));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            string saveButtonText = "Save Screenshot";
            if (loadedScreenshotFromFile)
                saveButtonText = "Replace Screenshot";

            //EditorGUI.BeginDisabledGroup(screenshot == null);
            EditorGUI.BeginDisabledGroup(SceneView.lastActiveSceneView == null);
            if (GUILayout.Button(saveButtonText))
            {
                if (sceneRT == null)
                {
                    System.Reflection.FieldInfo[] fields = typeof(SceneView).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var v in fields)
                    {
                        if (v.Name == "m_SceneTargetTexture")
                        {
                            sceneRT = v.GetValue(SceneView.lastActiveSceneView) as RenderTexture;
                        }
                    }
                }

                SaveScreenshot(currentSceneSettings.SceneName, ConvertRenderTexture(sceneRT));
                cachedScreenshot = null;
            }
            EditorGUI.EndDisabledGroup();

            //EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(currentSceneSettings.SceneId) || !HasSavedScreenshot(currentSceneSettings.SceneName));
            if (GUILayout.Button("Upload Screenshot"))
            {
                UploadScreenshot(currentSceneSettings.SceneId, currentSceneSettings.SceneName);
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

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

        Texture2D ConvertRenderTexture(RenderTexture rt)
        {
            //write rendertexture to png
            Texture2D tex = new Texture2D(rt.width, rt.height);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            return tex;
        }

        //returns true if savedblenderpath ends with blender.exe/app
        static bool IsBlenderPathValid()
        {
            if (string.IsNullOrEmpty(CognitiveVR_Settings.GetPreferences().SavedBlenderPath)) { return false; }
#if UNITY_EDITOR_WIN
            return CognitiveVR_Settings.GetPreferences().SavedBlenderPath.ToLower().EndsWith("blender.exe");
#elif UNITY_EDITOR_OSX
            return CognitiveVR_Settings.GetPreferences().SavedBlenderPath.ToLower().EndsWith("blender.app");
#else
            return false;
#endif
        }

        private void Update()
        {
            Repaint();
        }

        Texture2D cachedScreenshot;

        bool LoadScreenshot(string sceneName, out Texture2D returnTexture)
        {
            if (cachedScreenshot)
            {
                returnTexture = cachedScreenshot;
                return true;
            }
            //if file exists
            if (Directory.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot"))
            {
                if (File.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"))
                {
                    //load texture from file
                    Texture2D tex = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar  + "screenshot.png"));
                    returnTexture = tex;
                    cachedScreenshot = returnTexture;
                    return true;
                }
            }
            returnTexture = null;
            return false;
        }

        bool HasSavedScreenshot(string sceneName)
        {
            if (!Directory.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot")) { return false; }
            if (!File.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png")) { return false; }         
            return true;
        }

        void SaveScreenshot(string sceneName, Texture2D tex)
        {
            //create directory
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport");
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName);
            Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");

            //save file
            File.WriteAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png",tex.EncodeToPNG());
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
            GUIContent sceneExplorerLink = new GUIContent("View in Web Browser");
            if (KeyIsValid(settings.SceneId))
            {
                sceneExplorerLink.tooltip = Constants.SCENEEXPLORER_SCENE + settings.SceneId;
            }

            if (GUILayout.Button(sceneExplorerLink))
            {
                Application.OpenURL(Constants.SCENEEXPLORER_SCENE + settings.SceneId);
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Load Scene"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(settings.ScenePath);
                Repaint();
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

                    //if (!takeScreenshot)
                        //Repaint();
                }
            }
        }

        Rect selectedRect;

        public static void ExportScene(bool includeTextures, bool staticGeometry, float minSize, int textureDivisor, string customerID,string texturename)
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

            bool successfulExport = CognitiveVR_SceneExplorerExporter.ExportScene(fullName, includeTextures,staticGeometry,minSize,textureDivisor, texturename);

            if (!successfulExport)
            {
                Debug.LogError("Scene export canceled!");
                return;
            }

            if (!IsBlenderPathValid())
            {
                Debug.LogError("Blender is not found during scene export! Use Edit>Preferences...CognitivePreferences to locate Blender\nScene: "+ fullName+" exported to folder but not mesh decimated!");
                //return;
            }

            string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(fullName);
            string decimateScriptPath = Application.dataPath + "/CognitiveVR/Editor/decimateall.py";

            //write json settings file
            string jsonSettingsContents = "{ \"scale\":1, \"customerId\":\"" + customerID + "\",\"sceneName\":\""+ currentSceneSettings.SceneName+ "\",\"sdkVersion\":\"" + Core.SDK_Version + "\"}";
            File.WriteAllText(objPath + "settings.json", jsonSettingsContents);

            //System.Diagnostics.Process.Start("http://google.com/search?q=" + "cat pictures");

            decimateScriptPath = decimateScriptPath.Replace(" ", "\" \"");
            objPath = objPath.Replace(" ", "\" \"");
            fullName = fullName.Replace(" ", "\" \"");

            EditorUtility.ClearProgressBar();

            //use case for empty 360 video scenes
            if (String.IsNullOrEmpty(prefs.SavedBlenderPath))
            {
                Debug.LogError("Blender is not found during scene export! Scene is not being decimated");
            }

            ProcessStartInfo processInfo;
#if UNITY_EDITOR_WIN
            processInfo = new ProcessStartInfo(prefs.SavedBlenderPath);
            processInfo.UseShellExecute = true;
            processInfo.Arguments = "-P " + decimateScriptPath + " " + objPath + " " + prefs.ExportSettings.ExplorerMinimumFaceCount + " " + prefs.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;

#elif UNITY_EDITOR_OSX
            processInfo = new ProcessStartInfo("open");
            processInfo.Arguments = prefs.SavedBlenderPath + " --args -P " + decimateScriptPath + " " + objPath + " " + prefs.ExportSettings.ExplorerMinimumFaceCount + " " + prefs.ExportSettings.ExplorerMaximumFaceCount + " " + fullName;
            processInfo.UseShellExecute = false;
#endif

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

        static void UploadScreenshot(string sceneid, string sceneName)
        {
            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            string[] screenshotPath = new string[0];
            if (Directory.Exists(sceneExportDirectory + "screenshot"))
            {
                screenshotPath = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot");
            }
            else
            {
                Debug.Log("SceneExportWindow Upload can't find directory to screenshot");
                return;
            }
            
            if (screenshotPath.Length == 0)
            {
                Debug.Log("can't load data from screenshot directory");
                return;
            }

            int version = 1;
            string url = Constants.SCENEEXPLORERAPI_SCENES + sceneid + "/screenshot?version=" + version;


            WWWForm wwwForm = new WWWForm();
            wwwForm.AddBinaryData("screenshot", File.ReadAllBytes(screenshotPath[0]), "screenshot.png");
            new WWW(url, wwwForm);
        }

        static void UploadDecimatedScene(bool fetchNewCurrentScene = false)
        {
            if (fetchNewCurrentScene)
            {
                UploadSceneSettings = prefs.FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
            }
            if (UploadSceneSettings == null) { return; }

            if (sceneUploadWWW != null)
            {
                Debug.LogError("scene upload WWW is not null. please wait until your scene has finished uploading before uploading another!");
            }

            bool uploadConfirmed = false;
            string sceneName = UploadSceneSettings.SceneName;
            string fullName = sceneName + appendName;
            string[] filePaths = new string[] { };

            string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
            var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

            if (SceneExportDirExists)
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName + Path.DirectorySeparatorChar);
            }

            //custom confirm upload popup windows
            if ((!SceneExportDirExists || filePaths.Length <= 1))
            {
                if (EditorUtility.DisplayDialog("Upload Scene", "Scene " + UploadSceneSettings.SceneName + " has no exported geometry. Upload anyway?", "Yes", "No"))
                {
                    uploadConfirmed = true;
                    //create a json.settings file in the directory
                    string objPath = CognitiveVR_SceneExplorerExporter.GetDirectory(fullName);

                    Directory.CreateDirectory(objPath);

                    string jsonSettingsContents = "{ \"scale\":1, \"customerId\":\"" + prefs.CompanyProduct + "\",\"sceneName\":\"" + currentSceneSettings.SceneName + "\",\"sdkVersion\":\"" + Core.SDK_Version + "\"}";
                    File.WriteAllText(objPath + "settings.json", jsonSettingsContents);
                }
            }
            else
            {
                if (EditorUtility.DisplayDialog("Upload Scene", "Do you want to upload \"" + UploadSceneSettings.SceneName + "\" to your Dashboard?", "Yes", "No"))
                {
                    uploadConfirmed = true;
                }
            }

            if (!uploadConfirmed)
            {
                UploadSceneSettings = null;
                return;
                //just exit now
            }

            //after confirmation because uploading an empty scene creates a settings.json file
            if (Directory.Exists(sceneExportDirectory))
            {
                filePaths = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName + Path.DirectorySeparatorChar);
            }

            string[] screenshotPath = new string[0];
            if (Directory.Exists(sceneExportDirectory + "screenshot"))
            {
                screenshotPath = Directory.GetFiles(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + fullName + Path.DirectorySeparatorChar + "screenshot");
            }
            else
            {
                Debug.Log("SceneExportWindow Upload can't find directory to screenshot");
            }

            string fileList = "Upload Files:\n";

            string mtlFilepath = "";
            string objFilepath = "";

            WWWForm wwwForm = new WWWForm();
            foreach (var f in filePaths)
            {
                //set obj file. prefer decimated
                if (f.Contains(".obj"))
                {
                    if (f.Contains("_decimated.obj"))
                    {
                        objFilepath = f;
                    }
                    else if (string.IsNullOrEmpty(objFilepath))
                    {
                        objFilepath = f;
                    }
                    continue;
                }
                
                //set mtl file. prefer decimated
                if (f.Contains(".mtl"))
                {
                    if (f.Contains("_decimated.mtl"))
                    {
                        mtlFilepath = f;
                    }
                    else if (string.IsNullOrEmpty(mtlFilepath))
                    {
                        mtlFilepath = f;
                    }
                    continue;
                }

                fileList += f + "\n";

                var data = File.ReadAllBytes(f);
                wwwForm.AddBinaryData("file", data, Path.GetFileName(f));
            }

            if (!string.IsNullOrEmpty(objFilepath))
            {
                //add obj and mtl files
                wwwForm.AddBinaryData("file", File.ReadAllBytes(objFilepath), Path.GetFileName(objFilepath));
                fileList += objFilepath + "\n";
                wwwForm.AddBinaryData("file", File.ReadAllBytes(mtlFilepath), Path.GetFileName(mtlFilepath));
                fileList += mtlFilepath + "\n";
            }

            Debug.Log(fileList);

            if (screenshotPath.Length == 0)
            {
                Debug.Log("SceneExportWindow Upload can't find files in screenshot directory");
            }
            else
            {
                wwwForm.AddBinaryData("screenshot", File.ReadAllBytes(screenshotPath[0]), "screenshot.png");
            }

            sceneUploadWWW = new WWW(Constants.SCENEEXPLORERAPI_SCENES, wwwForm);

            EditorApplication.update += UpdateUploadData;
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
                Debug.LogError("Upload canceled!");
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

            //response can be <!DOCTYPE html><html lang=en><head><meta charset=utf-8><title>Error</title></head><body><pre>Internal Server Error</pre></body></html>
            if (sceneUploadWWW.text.Contains("Internal Server Error"))
            {
                Debug.LogError("Scene Upload Error:" + sceneUploadWWW.text);

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

            //TODO after scene upload response, hit another route to get the version of the scene

            GUI.FocusControl("NULL");

            AssetDatabase.SaveAssets();

            if (EditorUtility.DisplayDialog("Upload Complete", UploadSceneSettings.SceneName + " was successfully uploaded! Do you want to open your scene in SceneExplorer?","Open on SceneExplorer","Close"))
            {
                Application.OpenURL(Constants.SCENEEXPLORER_SCENE + UploadSceneSettings.SceneId);
            }

            Debug.Log("<color=green>Scene Upload Complete!</color>");

            UploadSceneSettings = null;
        }

        static List<DynamicObjectForm> dynamicObjectForms = new List<DynamicObjectForm>();

        public static void UploadDynamicObjects()
        {
            string fileList = "Upload Files:\n";

            var settings = CognitiveVR_Settings.GetPreferences().FindSceneByPath(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path);
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

            string uploadUrl = Constants.SCENEEXPLORERAPI_OBJECTS + sceneid + "/";

            string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";
            var subdirectories = Directory.GetDirectories(path);

            int option = EditorUtility.DisplayDialogComplex("Upload Dynamic Objects", "Do you want to upload " + subdirectories.Length + " Objects to \"" + settings.SceneName + "\" (" + settings.SceneId + ")?", "Ok", "Cancel","Open Directory");
            if (option == 0) { } //ok
            else if (option == 1){return; } //cancel
            else
            {
#if UNITY_EDITOR_WIN
                //open directory
                System.Diagnostics.Process.Start("explorer.exe", path);
                return;
#elif UNITY_EDITOR_OSX
                System.Diagnostics.Process.Start("open", path);
                return;
#endif
            }
            string objectNames="";
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

                objectNames += dirname + "\n";

                dynamicObjectForms.Add(new DynamicObjectForm(uploadUrl + dirname, wwwForm));
            }

            if (dynamicObjectForms.Count > 0)
            {
                Debug.Log("Upload dynamic objects: " + objectNames);
                EditorApplication.update += UpdateUploadDynamics;
            }
        }

        class DynamicObjectForm
        {
            public string Url;
            public WWWForm Form;

            public DynamicObjectForm(string url, WWWForm form)
            {
                Url = url;
                Form = form;
            }
        }

        static WWW dynamicUploadWWW;
        static void UpdateUploadDynamics()
        {
            if (dynamicUploadWWW == null)
            {
                //get the next dynamic object to upload from forms
                if (dynamicObjectForms.Count == 0)
                {
                    //DONE!
                    Debug.Log("<color=green>All dynamic object uploads complete!</color>");
                    EditorApplication.update -= UpdateUploadDynamics;
                    return;
                }
                else
                {
                    dynamicUploadWWW = new WWW(dynamicObjectForms[0].Url, dynamicObjectForms[0].Form);
                    dynamicObjectForms.RemoveAt(0);
                }
            }

            if (!dynamicUploadWWW.isDone) { return; }

            if (!string.IsNullOrEmpty(dynamicUploadWWW.error))
            {
                Debug.LogError(dynamicUploadWWW.error);
            }

            Debug.Log("Finished uploading dynamic object to " + dynamicUploadWWW.url);

            dynamicUploadWWW = null;
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

        static void FindBlender()
        {
#if UNITY_EDITOR_WIN
            if (Directory.Exists(@"C:/Program Files/"))
            {
                if (Directory.Exists(@"C:/Program Files/Blender Foundation/"))
                {
                    if (Directory.Exists(@"C:/Program Files/Blender Foundation/Blender"))
                    {
                        if (File.Exists(@"C:/Program Files/Blender Foundation/Blender/blender.exe"))
                        {
                            CognitiveVR_Settings.GetPreferences().SavedBlenderPath = @"C:/Program Files/Blender Foundation/Blender/blender.exe";
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
                            CognitiveVR_Settings.GetPreferences().SavedBlenderPath = @"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe";
                        }
                    }
                }
            }
#elif UNITY_EDITOR_OSX
            //check /Applications/Blender/blender.app
            if (Directory.Exists(@"/Applications/"))
            {
                if (Directory.Exists(@"/Applications/Blender/"))
                {
                    if (File.Exists(@"/Applications/Blender/blender.app"))
                    {
                        CognitiveVR_Settings.GetPreferences().SavedBlenderPath = @"/Applications/Blender/blender.app";
                    }
                }
            }
#endif
        }

        private static void UpdateSceneNames()
        {
            //save these to a temp list
            if (prefs == null)
            {
                prefs = CognitiveVR_Settings.GetPreferences();
            }

            List<CognitiveVR_Preferences.SceneSettings> oldSettings = new List<CognitiveVR_Preferences.SceneSettings>();
            foreach (var v in prefs.sceneSettings)
            {
                oldSettings.Add(v);
            }


            //clear then rebuild the list in preferences
            prefs.sceneSettings.Clear();

            //add all scenes
            string[] guidList = AssetDatabase.FindAssets("t:scene");

            foreach (var v in guidList)
            {
                string path = AssetDatabase.GUIDToAssetPath(v);
                string name = path.Substring(path.LastIndexOf('/') + 1);
                name = name.Substring(0, name.Length - 6);

                prefs.sceneSettings.Add(new CognitiveVR_Preferences.SceneSettings(name, path));
            }

            //match up dictionary keys from temp list
            foreach (var oldSetting in oldSettings)
            {
                foreach (var newSetting in prefs.sceneSettings)
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