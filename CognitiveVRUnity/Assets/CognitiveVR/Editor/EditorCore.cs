using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using System;
using CognitiveVR;

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
                _checkmark = EditorGUIUtility.FindTexture("Collab");
            }
            return _checkmark;
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
        
    }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        
    }
    #endregion
}
