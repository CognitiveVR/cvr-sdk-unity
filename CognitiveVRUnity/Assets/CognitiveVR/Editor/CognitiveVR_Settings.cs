using UnityEngine;
using UnityEditor;

namespace CognitiveVR
{
    [InitializeOnLoad]
    public class CognitiveVR_Settings : EditorWindow
    {
        static Color Green = new Color(0.6f, 1f, 0.6f);

        static CognitiveVR_Settings()
        {
            EditorApplication.update += Update;
        }

        static void Update()
        {
            bool show = true;

#if CVR_STEAMVR || CVR_OCULUSVR || CVR_GOOGLEVR || CVR_NONE
        show = false;
#endif
            string version = EditorPrefs.GetString("cvr_version");
            if (string.IsNullOrEmpty(version) || version != CognitiveVR.Core.SDK_Version)
            {
                show = true;
                //new version
#if CVR_STEAMVR
                option = "CVR_STEAMVR";
#elif CVR_OCULUSVR
                option = "CVR_OCULUS";
#elif CVR_GOOGLEVR
                option = "CVR_GOOGLEVR";
#elif CVR_NONE
                option = "CVR_NONE";
#endif
            }

            if (show)
            {
                CognitiveVR_Settings window = GetWindow<CognitiveVR_Settings>(true);
                Vector2 size = new Vector2(300, 500);
                window.minSize = size;
                window.maxSize = size;
            }

            EditorApplication.update -= Update;
        }

        string GetSamplesResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "CognitiveVR/Editor".Length) + "";
        }

        string GetResourcePath()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            path = System.IO.Path.GetDirectoryName(path);
            return path.Substring(0, path.Length - "Editor".Length) + "";
        }

        static string option = "";
        string newID = "";
        public void OnGUI()
        {

            //=========================
            //TITLE
            //=========================

            //title
            var resourcePath = GetResourcePath();
            var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(resourcePath + "Textures/logo.png");
            var rect = GUILayoutUtility.GetRect(position.width, 40, GUI.skin.window);
            if (logo)
                GUI.DrawTexture(rect, logo, ScaleMode.ScaleToFit);

            if (string.IsNullOrEmpty(newID))
            {
                newID = GetPreferences().CustomerID;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Version: " + Core.SDK_Version);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

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



            //=========================
            //account
            //=========================

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

           

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("CognitiveVR Customer ID");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUIContent addInitButtonText = new GUIContent("Add CognitiveVR Manager", "Does not Destroy on Load\nInitializes analytics system with basic device info");

            bool hasManager = FindObjectOfType<CognitiveVR.CognitiveVR_Manager>() != null;
            if (hasManager)
            {
                addInitButtonText.text = "CognitiveVR Manager Found!";
                addInitButtonText.tooltip = "";
            }
            newID = EditorGUILayout.TextField(newID);

            bool validID = (newID != null && newID != "companyname1234-productname-test" && newID.Length > 0);
            if (validID)
            {
                if (hasManager) { GUI.color = Green; }
                if (GUILayout.Button(addInitButtonText))
                {
                    if (!hasManager)
                    {
                        string sampleResourcePath = GetSamplesResourcePath();
                        Object basicInit = AssetDatabase.LoadAssetAtPath<Object>(sampleResourcePath + "CognitiveVR/Resources/CognitiveVR_Manager.prefab");
                        if (basicInit)
                        {
                            PrefabUtility.InstantiatePrefab(basicInit);
                        }
                        else
                        {
                            Debug.Log("Couldn't find CognitiveVR_Manager.prefab");
                        }
                    }
                }
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Button("Invalid Customer ID");
            }

            //=========================
            //SDK
            //=========================

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!validID);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("Please Select your VR SDK");
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            if (option == "CVR_STEAMVR") { GUI.color = Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Steam VR")) { option = "CVR_STEAMVR"; }
            GUI.color = Color.white;

            /*if (option == "CVR_OCULUSVR") { GUI.color = Green; GUI.backgroundColor = Color.white; }
            if (GUILayout.Button("Oculus VR")) { option = "CVR_OCULUSVR"; }
            GUI.color = Color.white;

            if (option == "CVR_GOOGLEVR") { GUI.color = Green; GUI.backgroundColor = Color.white; }
            if (GUILayout.Button("Google VR")) { option = "CVR_GOOGLEVR"; }
            GUI.color = Color.white;*/

            if (option == "CVR_NONE") { GUI.color = Green; GUI.backgroundColor = Color.white; }
            if (GUILayout.Button("None")) { option = "CVR_NONE"; }
            GUI.color = Color.white;

            EditorGUI.EndDisabledGroup();

            //=========================
            //save
            //=========================

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!validID || string.IsNullOrEmpty(option));

            if (GUILayout.Button("Save"))
            {
                SetPlayerDefine(option);

                CognitiveVR.CognitiveVR_Preferences prefs = CognitiveVR.CognitiveVR_EditorPrefs.GetPreferences();
                prefs.CustomerID = newID;
                EditorUtility.SetDirty(prefs);
                AssetDatabase.SaveAssets();

                EditorPrefs.SetString("cvr_version", CognitiveVR.Core.SDK_Version);
            }

            bool containsSDKSymbol = false;
            if (PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone).Contains("CVR_"))
            {
                containsSDKSymbol = true;
            }

            EditorGUI.BeginDisabledGroup(!containsSDKSymbol);
            //save and close
            if (GUILayout.Button("Open Component Setup"))
            {
                CognitiveVR.CognitiveVR_ComponentSetup.Init();

                Close();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
        }

        public void SetPlayerDefine(string newDefine)
        {
            //get all scripting define symbols
            string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
            string[] symbols = s.Split(';');

            //remove all CVR_ symbols
            for(int i = 0; i<symbols.Length; i++)
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


            //if newDefine !null set new define
            if (!string.IsNullOrEmpty(newDefine))
            {
                alldefines += newDefine + ";";
            }
            PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, alldefines);

        }

        public static CognitiveVR_Preferences GetPreferences()
        {
            CognitiveVR_Preferences asset = AssetDatabase.LoadAssetAtPath<CognitiveVR_Preferences>("Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CognitiveVR_Preferences>();
                AssetDatabase.CreateAsset(asset, "Assets/CognitiveVR/Resources/CognitiveVR_Preferences.asset");
                AssetDatabase.Refresh();
            }
            return asset;
        }
    }
}