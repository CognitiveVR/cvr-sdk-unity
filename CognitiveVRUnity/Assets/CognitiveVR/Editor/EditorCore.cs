using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System;
using CognitiveVR;
using System.IO;

//contains functions for button/label styles
//references to editor prefs
//set editor define symbols
//check for sdk updates
//pre/post build inferfaces

[InitializeOnLoad]
public class EditorCore: IPreprocessBuild, IPostprocessBuild
{
    static EditorCore()
    {
        //Debug.Log("CognitiveVR EditorCore constructor");
        
        //check sdk versions
        CheckForUpdates();

        if (!EditorPrefs.HasKey("cognitive_init_popup"))
        {
            InitWizard.Init();
        }
        EditorPrefs.SetBool("cognitive_init_popup", true);
    }

    public static Color GreenButton = new Color(0.4f, 1f, 0.4f);

    static GUIStyle headerStyle;
    public static GUIStyle HeaderStyle
    {
        get
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.largeLabel);
                headerStyle.fontSize = 14;
                headerStyle.alignment = TextAnchor.UpperCenter;
                headerStyle.fontStyle = FontStyle.Bold;
                headerStyle.richText = true;
            }
            return headerStyle;
        }
    }

    private static Texture2D _logo;
    public static Texture2D LogoTexture
    {
        get
        {
            if (_logo == null)
            {
                _logo = Resources.Load<Texture2D>("cognitive3d-editorlogo");
            }
            return _logo;
        }
    }

    private static Texture2D _checkmark;
    public static Texture2D Checkmark
    {
        get
        {
            if (_checkmark == null)
            {
                _checkmark = Resources.Load<Texture2D>("nice_checkmark");
            }
            return _checkmark;
        }
    }

    private static Texture2D _alert;
    public static Texture2D Alert
    {
        get
        {
            if (_alert == null)
            {
                _alert = Resources.Load<Texture2D>("alert");
            }
            return _alert;
        }
    }

    private static Texture2D _emptycheckmark;
    public static Texture2D EmptyCheckmark
    {
        get
        {
            if (_emptycheckmark == null)
            {
                _emptycheckmark = Resources.Load<Texture2D>("grey_circle");
            }
            return _emptycheckmark;
        }
    }

    private static Texture2D _sceneHighlight;
    public static Texture2D SceneHighlight
    {
        get
        {
            if (_sceneHighlight == null)
            {
                _sceneHighlight = Resources.Load<Texture2D>("scene_blue");
            }
            return _sceneHighlight;
        }
    }

    private static Texture2D _sceneBackground;
    public static Texture2D SceneBackground
    {
        get
        {
            if (_sceneBackground == null)
            {
                _sceneBackground = Resources.Load<Texture2D>("scene_grey");
            }
            return _sceneBackground;
        }
    }

    private static Texture2D _objectsHighlight;
    public static Texture2D ObjectsHightlight
    {
        get
        {
            if (_objectsHighlight == null)
            {
                _objectsHighlight = Resources.Load<Texture2D>("objects_blue");
            }
            return _objectsHighlight;
        }
    }

    private static Texture2D _objectsBackground;
    public static Texture2D ObjectsBackground
    {
        get
        {
            if (_objectsBackground == null)
            {
                _objectsBackground = Resources.Load<Texture2D>("objects_grey");
            }
            return _objectsBackground;
        }
    }

    private static GUISkin _wizardGuiSkin;
    public static GUISkin WizardGUISkin
    {
        get
        {
            if (_wizardGuiSkin == null)
            {
                _wizardGuiSkin = Resources.Load<GUISkin>("WizardGUISkin");
            }
            return _wizardGuiSkin;
        }
    }

    private static string _blenderPath;
    public static string BlenderPath
    {
        get
        {
            if (string.IsNullOrEmpty(_blenderPath))
            {
                _blenderPath = FindBlender();
            }
            return _blenderPath;
        }
        set
        {
            _blenderPath = value;
        }
    }

    public static bool IsBlenderPathValid
    {
        get
        {
            if (string.IsNullOrEmpty(BlenderPath)) { Debug.Log("EditorCore BlenderPath is null or empty"); return false; }
#if UNITY_EDITOR_WIN
            return BlenderPath.ToLower().EndsWith("blender.exe");
#elif UNITY_EDITOR_OSX
            return BlenderPath.ToLower().EndsWith("blender.app");
#else
            return false;
#endif
        }
    }

    public static ExportSettings ExportSettings = ExportSettings.HighSettings;

    public static bool IsDeveloperKeyValid
    {
        get
        {
            return EditorPrefs.HasKey("developerkey") && !string.IsNullOrEmpty(EditorPrefs.GetString("developerkey"));
        }
    }

    public static string DeveloperKey
    {
        get
        {
            return EditorPrefs.GetString("developerkey");
        }
    }

    internal static bool HasDynamicExportFiles(string meshname)
    {
        string dynamicExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic" + Path.DirectorySeparatorChar + meshname;
        var SceneExportDirExists = Directory.Exists(dynamicExportDirectory);

        return SceneExportDirExists && Directory.GetFiles(dynamicExportDirectory).Length > 0;
    }


    //Color Blue = new Color32(0xFA, 0x4F, 0xF2, 0xFF); //set in guiskin
    //Color Black = new Color32(0x1A, 0x1A, 0x1A, 0xFF); //set in guiskin
    public static Color BlueishGrey = new Color32(0xE8, 0xEB, 0xFF, 0xFF); //E8EBEFFF


    public static void SetPlayerDefine(List<string> newDefines)
    {
        //get all scripting define symbols
        string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string[] symbols = s.Split(';');


        int cvrDefineCount = 0;
        int newDefineContains = 0;
        for (int i = 0; i < symbols.Length; i++)
        {
            if (symbols[i].StartsWith("CVR_"))
            {
                cvrDefineCount++;
            }
            if (newDefines.Contains(symbols[i]))
            {
                newDefineContains++;
            }
        }

        if (newDefineContains == cvrDefineCount && cvrDefineCount != 0)
        {
            //all defines already exist
            return;
        }

        //remove all CVR_ symbols
        for (int i = 0; i < symbols.Length; i++)
        {
            if (symbols[i].Contains("CVR_"))
            {
                symbols[i] = "";
            }
        }

        //rebuild symbols
        string alldefines = "";
        for (int i = 0; i < symbols.Length; i++)
        {
            if (!string.IsNullOrEmpty(symbols[i]))
            {
                alldefines += symbols[i] + ";";
            }
        }

        foreach (string define in newDefines)
        {
            alldefines += define + ";";
        }

        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, alldefines);

    }

    static CognitiveVR_Preferences _prefs;
    /// <summary>
    /// Gets the cognitivevr_preferences or creates and returns new default preferences
    /// </summary>
    /// <returns>Preferences</returns>
    public static CognitiveVR_Preferences GetPreferences()
    {
        if (_prefs == null)
        {
            _prefs = Resources.Load<CognitiveVR_Preferences>("CognitiveVR_Preferences");
            if (_prefs == null)
            {
                _prefs = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
                AssetDatabase.CreateAsset(_prefs, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
                AssetDatabase.Refresh();
            }
        }
        return _prefs;
    }

    #region Editor Screenshot

    static RenderTexture sceneRT = null;
    public static RenderTexture GetSceneRenderTexture()
    {
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
        return sceneRT;
    }

    static Texture2D cachedScreenshot;

    bool HasSavedScreenshot(string sceneName)
    {
        if (!Directory.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot")) { return false; }
        if (!File.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png")) { return false; }
        return true;
    }

    public static bool LoadScreenshot(string sceneName, out Texture2D returnTexture)
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
                tex.LoadImage(File.ReadAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"));
                returnTexture = tex;
                cachedScreenshot = returnTexture;
                return true;
            }
        }
        returnTexture = null;
        return false;
    }

    static List<Camera> tempDisabledCameras = new List<Camera>();
    //static RenderTexture saveRenderTexture;
    static string saveScreenshotSceneName;
    static int delay = 0;
    static System.Action SaveScreenshotComplete;

    public static void SaveCurrentScreenshot(string sceneName, System.Action saveScreenshotComplete)
    {
        delay = 0;
        saveScreenshotSceneName = sceneName;
        SaveScreenshotComplete = saveScreenshotComplete;
        EditorApplication.update += DelaySaveScreenshot;
    }

    static void DelaySaveScreenshot()
    {
        foreach (var c in UnityEngine.Object.FindObjectsOfType<Camera>())
        {
            if (c.enabled && c.gameObject.activeInHierarchy)
            {
                c.enabled = false;
                tempDisabledCameras.Add(c);
            }
        }
        if (delay < 1) { delay++; return; } //disable cameras for 2 frames - fixes issue with scene render texture and multiple cameras

        var saveRenderTexture = GetSceneRenderTexture();

        //write rendertexture to png
        Texture2D tex = new Texture2D(saveRenderTexture.width, saveRenderTexture.height);
        RenderTexture.active = saveRenderTexture;
        tex.ReadPixels(new Rect(0, 0, saveRenderTexture.width, saveRenderTexture.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        

        EditorApplication.update -= DelaySaveScreenshot;

        foreach (var c in tempDisabledCameras)
        {
            c.enabled = true;
        }
        tempDisabledCameras.Clear();

        //create directory
        Directory.CreateDirectory("CognitiveVR_SceneExplorerExport");
        Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + saveScreenshotSceneName);
        Directory.CreateDirectory("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + saveScreenshotSceneName + Path.DirectorySeparatorChar + "screenshot");

        //save file
        File.WriteAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + saveScreenshotSceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png", tex.EncodeToPNG());
        //use editor update to delay teh screenshot 1 frame?

        if (SaveScreenshotComplete != null)
            SaveScreenshotComplete.Invoke();
        SaveScreenshotComplete = null;
    }

    #endregion

    static System.Action RefreshSceneVersionComplete;
    /// <summary>
    /// get collection of versions of scene
    /// </summary>
    /// <param name="refreshSceneVersionComplete"></param>
    public static void RefreshSceneVersion(System.Action refreshSceneVersionComplete)
    {
        Debug.Log("refresh scene version");
        //gets the scene version from api and sets it to the current scene
        string currentSceneName = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().name;
        var currentSettings = CognitiveVR_Preferences.Instance.FindScene(currentSceneName);
        if (currentSettings != null)
        {
            if (!IsDeveloperKeyValid) { Debug.Log("Developer key invalid"); return; }

            if (currentSettings == null)
            {
                Debug.Log("SendSceneVersionRequest no scene settings!");
                return;
            }
            if (string.IsNullOrEmpty(currentSettings.SceneId))
            {
                Debug.Log("SendSceneVersionRequest Current scene doesn't have an id - cannot get scene versions");
                if (refreshSceneVersionComplete != null)
                    refreshSceneVersionComplete.Invoke();
                return;
            }

            RefreshSceneVersionComplete = refreshSceneVersionComplete;
            string url = Constants.GETSCENEVERSIONS(currentSettings.SceneId);

            Dictionary<string, string> headers = new Dictionary<string, string>();
            if (EditorCore.IsDeveloperKeyValid)
                headers.Add("Authorization", "APIKEY:DEVELOPER " + EditorCore.DeveloperKey);

            EditorNetwork.Get(url, GetSceneVersionResponse, headers, true, "Get Scene Version");//AUTH
        }
        else
        {
            Debug.Log("No scene versions for scene: " + currentSceneName);
        }
    }

    private static void GetSceneVersionResponse(int responsecode, string error, string text)
    {
        if (responsecode != 200)
        {
            RefreshSceneVersionComplete = null;
            //internal server error
            Util.logDebug("GetSettingsResponse [ERROR] " + responsecode);
            return;
        }
        var settings = CognitiveVR_Preferences.FindCurrentScene();
        if (settings == null)
        {
            //this should be impossible, but might happen if changing scenes at exact time
            RefreshSceneVersionComplete = null;
            Debug.LogWarning("Scene version request returned 200, but current scene cannot be found");
            return;
        }

        var collection = JsonUtility.FromJson<SceneVersionCollection>(text);
        if (collection != null)
        {
            settings.VersionId = collection.GetLatestVersion().id;
            settings.VersionNumber = collection.GetLatestVersion().versionNumber;
            EditorUtility.SetDirty(CognitiveVR_Preferences.Instance);
            AssetDatabase.SaveAssets();
        }

        if (RefreshSceneVersionComplete != null)
        {
            RefreshSceneVersionComplete.Invoke();
        }
    }

    #region GUI
    /// <summary>
    /// vibrant accept color for buttons
    /// </summary>
    static Color AcceptVibrant
    {
        get
        {
            if (EditorGUIUtility.isProSkin)
            {
                return Color.green;
            }
            return Color.green;
        }
    }

    /// <summary>
    /// vibrant decline color for buttons
    /// </summary>
    static Color DeclineVibrant
    {
        get
        {
            if (EditorGUIUtility.isProSkin)
            {
                return Color.red;
            }
            return Color.red;
        }
    }

    public static void GUIHorizontalLine(float size = 10)
    {
        //alternatively EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        GUILayout.Space(size);
        GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        GUILayout.Space(size);
    }

    public static bool AcceptButtonLarge(string text)
    {
        GUI.color = AcceptVibrant;

        if (GUILayout.Button(text, GUILayout.Height(40)))
        {
            return true;
        }
        GUI.color = Color.white;
        return false;
    }

    public static bool DeclineButtonLarge(string text)
    {
        GUI.color = DeclineVibrant;

        if (GUILayout.Button(text, GUILayout.Height(40)))
        {
            return true;
        }
        GUI.color = Color.white;
        return false;
    }

    public static void DisableButtonLarge(string text)
    {
        EditorGUI.BeginDisabledGroup(true);
        GUILayout.Button(text, GUILayout.Height(40));
        EditorGUI.EndDisabledGroup();
    }

    public static bool DisableableAcceptButtonLarget(string text, bool disable)
    {
        if (disable)
        {
            DisableButtonLarge(text);
            return false;
        }
        return AcceptButtonLarge(text);
    }

    //https://forum.unity.com/threads/how-to-copy-and-paste-in-a-custom-editor-textfield.261087/

    /// <summary>
    /// Add copy-paste functionality to any text field
    /// Returns changed text or NULL.
    /// Usage: text = HandleCopyPaste (controlID) ?? text;
    /// </summary>
    public static string HandleCopyPaste(int controlID)
    {
        if (controlID == GUIUtility.keyboardControl)
        {
            if (Event.current.type == EventType.KeyUp && (Event.current.modifiers == EventModifiers.Control || Event.current.modifiers == EventModifiers.Command))
            {
                if (Event.current.keyCode == KeyCode.C)
                {
                    Event.current.Use();
                    TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    editor.Copy();
                }
                else if (Event.current.keyCode == KeyCode.V)
                {
                    Event.current.Use();
                    TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                    editor.Paste();
                    return editor.text;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// TextField with copy-paste support
    /// </summary>
    public static string TextField(Rect rect, string value, int maxlength)
    {
        int textFieldID = GUIUtility.GetControlID("TextField".GetHashCode(), FocusType.Keyboard) + 1;
        if (textFieldID == 0)
            return value;

        // Handle custom copy-paste
        value = HandleCopyPaste(textFieldID) ?? value;

        return GUI.TextField(rect, value,maxlength);
    }

    #endregion

    public static List<string> ExportedDynamicObjects;
    /// <summary>
    /// returns list of exported dynamic objects. refreshes on init window focused
    /// </summary>
    /// <returns></returns>
    public static List<string> GetExportedDynamicObjectNames()
    {
        if (ExportedDynamicObjects != null)
        {
            return ExportedDynamicObjects;
        }
        //read folder
        string path = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + "Dynamic";

        Directory.CreateDirectory(path);

        var subdirectories = Directory.GetDirectories(path);

        List<string> ObjectNames = new List<string>();

        foreach (var subdir in subdirectories)
        {
            var dirname = new DirectoryInfo(subdir).Name;
            ObjectNames.Add(dirname);
        }
        ExportedDynamicObjects = ObjectNames;
        return ObjectNames;
    }

    static string FindBlender()
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
                        return @"C:/Program Files/Blender Foundation/Blender/blender.exe";
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
                        return @"/Applications/Blender/blender.app";
                    }
                }
            }
#endif
        return "";
    }

    #region Build Callbacks

    public int callbackOrder
    {
        get
        {
            return 0;
        }
    }

    public void OnPostprocessBuild(BuildTarget target, string path)
    {
        //TODO send dynamic object manifest from local json file
        /*if (EditorUtility.DisplayDialog("Object Manifest", "Upload all object ids to SceneExplorer for aggregation?", "Ok", "No"))
        {
            ManageDynamicObjects.UploadManifest();
        }*/
    }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        
    }
    #endregion


    #region Updates

    //data about the last sdk release on github
    public class ReleaseInfo
    {
        public string tag_name;
        public string body;
        public string created_at;
    }

    private static void SaveEditorVersion()
    {
        if (EditorPrefs.GetString("cvr_version") != CognitiveVR.Core.SDK_VERSION)
        {
            EditorPrefs.SetString("cvr_version", CognitiveVR.Core.SDK_VERSION);
            EditorPrefs.SetString("cvr_updateDate", System.DateTime.UtcNow.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    public static void ForceCheckUpdates()
    {
        EditorApplication.update -= UpdateCheckForUpdates;
        EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        SaveEditorVersion();

        checkForUpdatesRequest = new UnityEngine.WWW(Constants.GITHUB_SDKVERSION);
        EditorApplication.update += UpdateCheckForUpdates;
    }

    static WWW checkForUpdatesRequest;
    static void CheckForUpdates()
    {
        System.DateTime remindDate; //current date must be this or beyond to show popup window

        if (System.DateTime.TryParse(EditorPrefs.GetString("cvr_updateRemindDate", "1/1/1971 00:00:01"), out remindDate))
        {
            if (System.DateTime.UtcNow > remindDate)
            {
                EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                SaveEditorVersion();

                checkForUpdatesRequest = new UnityEngine.WWW(Constants.GITHUB_SDKVERSION);
                EditorApplication.update += UpdateCheckForUpdates;
            }
        }
        else
        {
            EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    static void UpdateCheckForUpdates()
    {
        if (!checkForUpdatesRequest.isDone)
        {
            //check for timeout
        }
        else
        {
            if (!string.IsNullOrEmpty(checkForUpdatesRequest.error))
            {
                Debug.Log("Check for cognitiveVR SDK version update error: " + checkForUpdatesRequest.error);
            }

            if (!string.IsNullOrEmpty(checkForUpdatesRequest.text))
            {
                var info = JsonUtility.FromJson<ReleaseInfo>(checkForUpdatesRequest.text);

                var version = info.tag_name;
                string summary = info.body;

                if (!string.IsNullOrEmpty(version))
                {
                    if (version != CognitiveVR.Core.SDK_VERSION)
                    {
                        //new version
                        CognitiveVR_UpdateSDKWindow.Init(version, summary);
                    }
                    else if (EditorPrefs.GetString("cvr_skipVersion") == version)
                    {
                        //skip this version. limit this check to once a day
                        EditorPrefs.SetString("cvr_updateRemindDate", System.DateTime.UtcNow.AddDays(1).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        //up to date
                        Debug.Log("Version " + version + ". You are up to date");
                    }
                }
            }
            EditorApplication.update -= UpdateCheckForUpdates;
        }
    }

    #endregion

    #region Scene Export
    /// <summary>
    /// has folder and files for scene
    /// </summary>
    /// <param name="currentSceneSettings"></param>
    /// <returns></returns>
    public static bool HasSceneExportFiles(CognitiveVR_Preferences.SceneSettings currentSceneSettings)
    {
        if (currentSceneSettings == null) { return false; }
        string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
        var SceneExportDirExists = Directory.Exists(sceneExportDirectory);
        
        return SceneExportDirExists && Directory.GetFiles(sceneExportDirectory).Length > 0;
    }

    /// <summary>
    /// has folder for scene. can be empty
    /// </summary>
    /// <param name="currentSceneSettings"></param>
    /// <returns></returns>
    public static bool HasSceneExportFolder(CognitiveVR_Preferences.SceneSettings currentSceneSettings)
    {
        if (currentSceneSettings == null) { return false; }
        string sceneExportDirectory = Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + "CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + currentSceneSettings.SceneName + Path.DirectorySeparatorChar;
        var SceneExportDirExists = Directory.Exists(sceneExportDirectory);

        return SceneExportDirExists;
    }
    #endregion
}
