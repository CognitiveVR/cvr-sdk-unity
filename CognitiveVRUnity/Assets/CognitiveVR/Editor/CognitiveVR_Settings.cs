using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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

#if CVR_STEAMVR || CVR_OCULUS || CVR_GOOGLEVR || CVR_DEFAULT
            show = false;
#endif

#if CVR_STEAMVR
            option.Add("CVR_STEAMVR");
#endif
#if CVR_OCULUS
            option.Add("CVR_OCULUS");
#endif
#if CVR_GOOGLEVR
            option.Add("CVR_GOOGLEVR");
#endif
#if CVR_DEFAULT
            option.Add("CVR_DEFAULT");
#endif

            string version = EditorPrefs.GetString("cvr_version");
            if (string.IsNullOrEmpty(version) || version != CognitiveVR.Core.SDK_Version)
            {
                show = true;
                //new version
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

        static List<string> option = new List<string>();
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

            if (Event.current.type == EventType.repaint && string.IsNullOrEmpty(newID))
            {
                GUIStyle style = new GUIStyle(GUI.skin.textField);
                style.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
                EditorGUILayout.TextField("companyname1234-productname-test", style);
            }
            else
            {
                newID = EditorGUILayout.TextField(newID);
            }

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
                            Debug.LogWarning("Couldn't find CognitiveVR_Manager.prefab");
                            GameObject go = new GameObject("CognitiveVR_Manager");
                            go.AddComponent<CognitiveVR_Manager>();
                            Selection.activeGameObject = go;
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

            if (option.Contains("CVR_STEAMVR")) { GUI.color = Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Steam VR 1.1.1+"))
            {
                if (option.Contains("CVR_STEAMVR"))
                    option.Remove("CVR_STEAMVR");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_STEAMVR");
                }
            }
            GUI.color = Color.white;

            if (option.Contains("CVR_OCULUS")) { GUI.color = Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Oculus Utilities 1.9.0+"))
            {
                if (option.Contains("CVR_OCULUS"))
                    option.Remove("CVR_OCULUS");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_OCULUS");
                }
            }
            GUI.color = Color.white;

            /*
            if (option == "CVR_GOOGLEVR") { GUI.color = Green; GUI.backgroundColor = Color.white; }
            if (GUILayout.Button("Google VR")) { option = "CVR_GOOGLEVR"; }
            GUI.color = Color.white;*/

            if (option.Contains("CVR_DEFAULT")) { GUI.color = Green; GUI.contentColor = Color.white; }
            if (GUILayout.Button("Unity Default VR Settings"))
            {
                if (option.Contains("CVR_DEFAULT"))
                    option.Remove("CVR_DEFAULT");
                else
                {
                    if (!Event.current.shift)
                        option.Clear();
                    option.Add("CVR_DEFAULT");
                }
            }
            GUI.color = Color.white;

            EditorGUI.EndDisabledGroup();

            //=========================
            //save
            //=========================

            GUILayout.Space(10);
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(!validID || option.Count == 0);

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
            if (PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup).Contains("CVR_"))
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

        public void SetPlayerDefine(List<string> newDefines)
        {
            //get all scripting define symbols
            string s = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
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

            foreach (string define in newDefines)
            {
                alldefines += define + ";";
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, alldefines);

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