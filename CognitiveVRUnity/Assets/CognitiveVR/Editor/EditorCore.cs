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

    /// <summary>
    /// Gets the cognitivevr_preferences or creates and returns new default preferences
    /// </summary>
    /// <returns>Preferences</returns>
    public static CognitiveVR_Preferences GetPreferences()
    {
        CognitiveVR_Preferences asset = Resources.Load<CognitiveVR_Preferences>("CognitiveVR_Preferences");
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
            AssetDatabase.CreateAsset(asset, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            AssetDatabase.Refresh();
        }
        return asset;
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

    Texture2D cachedScreenshot;

    bool HasSavedScreenshot(string sceneName)
    {
        if (!Directory.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot")) { return false; }
        if (!File.Exists("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png")) { return false; }
        return true;
    }

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
                tex.LoadImage(File.ReadAllBytes("CognitiveVR_SceneExplorerExport" + Path.DirectorySeparatorChar + sceneName + Path.DirectorySeparatorChar + "screenshot" + Path.DirectorySeparatorChar + "screenshot.png"));
                returnTexture = tex;
                cachedScreenshot = returnTexture;
                return true;
            }
        }
        returnTexture = null;
        return false;
    }

#endregion

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

    #endregion

    public static bool HasExportedCurrentScene()
    {
        return false;
    }

    public static List<string> GetExportedDynamicObjectNames()
    {
        //read folder
        return new List<string>();
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
        else if (Directory.Exists(@"C:/Program Files (x86)"))
        {
            if (Directory.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64"))
            {
                if (Directory.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64"))
                {
                    if (File.Exists(@"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe"))
                    {
                        return @"C:/Program Files (x86)/blender-2.77a-windows64/blender-2.77a-windows64/blender.exe";
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
    }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        
    }
    #endregion
}
