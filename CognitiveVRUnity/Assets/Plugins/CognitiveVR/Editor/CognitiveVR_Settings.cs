using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class CognitiveVR_Settings : EditorWindow
{
    static CognitiveVR_Settings window;

    static CognitiveVR_Settings()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        bool show = true;

#if CVR_STEAMVR || CVR_OCULUSVR || CVR_CARDBOARDVR || CVR_NONE
        show = false;
#endif

        if (show)
        {
            window = GetWindow<CognitiveVR_Settings>(true);
            Vector2 size = new Vector2(300, 500);
            window.minSize = size;
            window.maxSize = size;
        }

        EditorApplication.update -= Update;
    }

    Color buttonSelect = new Color(0.6f, 0.3f, 0.9f);

    string GetSamplesResourcePath()
    {
        var ms = MonoScript.FromScriptableObject(this);
        var path = AssetDatabase.GetAssetPath(ms);
        path = System.IO.Path.GetDirectoryName(path);
        return path.Substring(0, path.Length - "Plugins/CognitiveVR/Editor".Length) + "";
    }

    string GetResourcePath()
    {
        var ms = MonoScript.FromScriptableObject(this);
        var path = AssetDatabase.GetAssetPath(ms);
        path = System.IO.Path.GetDirectoryName(path);
        return path.Substring(0, path.Length - "Editor".Length) + "";
    }

    string option= "CVR_STEAMVR";
    string id = "companyname1234-productname-test";
    bool addedBasicInitPrefab = false;
    public void OnGUI()
    {
        //title
        var resourcePath = GetResourcePath();
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcePath + "Textures/logo.png");
        var rect = GUILayoutUtility.GetRect(position.width, 150, GUI.skin.window);
        if (logo)
            GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);

        /*GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("A Cognitive morning to you!");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();*/

        //links
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.color = Color.blue;
        if (GUILayout.Button("Sign Up", EditorStyles.whiteLabel))
            Application.OpenURL("https://dashboard.cognitivevr.io/");
        GUI.color = Color.white;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.color = Color.blue;
        if (GUILayout.Button("Documentation", EditorStyles.whiteLabel))
            Application.OpenURL("https://github.com/CognitiveVR/cvr-sdk-unity/wiki");
        GUI.color = Color.white;
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

         
        GUILayout.Space(20);

        //account 
         
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("cognitiveVR Customer ID");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        bool validID = id != "companyname1234-productname-test" && id.Length > 0;
        string addInitButtonText = "Add Basic Init Prefab";
        if (addedBasicInitPrefab)
        {
            addInitButtonText = "Init Prefab Added!";
        }
        id = EditorGUILayout.TextField(id);
        if (validID)
        {
            if (GUILayout.Button(addInitButtonText))
            {
                if (!addedBasicInitPrefab)
                {
                    string sampleResourcePath = GetSamplesResourcePath();
                    Object basicInit = AssetDatabase.LoadAssetAtPath<Object>(sampleResourcePath + "CognitiveVR/_Sample/CognitiveVR_BasicInit.prefab");
                    if (basicInit)
                    {
                        GameObject prefab = (GameObject)PrefabUtility.InstantiatePrefab(basicInit);
                        addedBasicInitPrefab = true;
                    }
                    else
                    {
                        Debug.Log("Couldn't find CognitiveVR_BasicInit.prefab");
                    }
                }
            }
        }
        else
        {
            GUILayout.Button("Invalid Customer ID");
        }
        GUILayout.Space(20);


        //sdk
        GUILayout.Label("Please Select your VR SDK");

        if (option == "CVR_STEAMVR") { GUI.color = buttonSelect; }
        if (GUILayout.Button("SteamVR")) { option = "CVR_STEAMVR"; }
        GUI.color = Color.white;

        /*if (option == "CVR_OCULUSVR") { GUI.color = buttonSelect; }
        if (GUILayout.Button("OculusVR")) { option = "CVR_OCULUSVR"; }
        GUI.color = Color.white;

        if (option == "CVR_CARDBOARDVR") { GUI.color = buttonSelect; }
        if (GUILayout.Button("Cardboard")) { option = "CVR_CARDBOARDVR"; }
        GUI.color = Color.white;*/

        if (option == "CVR_NONE") { GUI.color = buttonSelect; }
        if (GUILayout.Button("None")) { option = "CVR_NONE"; }
        GUI.color = Color.white;
        GUILayout.Space(40);


        //save and close
        if (GUILayout.Button("Save"))
        {
            if (!string.IsNullOrEmpty(option))
            {
                string alldefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone) + ";" + option;
                PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, alldefines);
            }

            CognitiveVR.CognitiveVR_Preferences prefs = CognitiveVR.CognitiveVR_EditorPrefs.GetPreferences();
            prefs.CustomerID = id;

            Close();
        }
    }
}